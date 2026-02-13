using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace WindowsAuthDemo
{
    public class ExportPDF : IHttpHandler
    {
        // ── Brand Colors ───────────────────────────────────────────────
        private static readonly BaseColor Primary = new BaseColor(67, 97, 238);
        private static readonly BaseColor PrimaryDark = new BaseColor(114, 9, 183);
        private static readonly BaseColor Dark = new BaseColor(30, 41, 59);
        private static readonly BaseColor Gray600 = new BaseColor(100, 116, 139);
        private static readonly BaseColor Gray400 = new BaseColor(148, 163, 184);
        private static readonly BaseColor Gray200 = new BaseColor(226, 232, 240);
        private static readonly BaseColor Gray100 = new BaseColor(241, 245, 249);
        private static readonly BaseColor Gray50 = new BaseColor(248, 250, 252);
        private static readonly BaseColor Green = new BaseColor(16, 185, 129);
        private static readonly BaseColor GreenDark = new BaseColor(6, 95, 70);
        private static readonly BaseColor GreenBg = new BaseColor(236, 253, 245);
        private static readonly BaseColor Amber = new BaseColor(245, 158, 11);
        private static readonly BaseColor AmberDark = new BaseColor(146, 64, 14);
        private static readonly BaseColor AmberBg = new BaseColor(255, 251, 235);
        private static readonly BaseColor Blue = new BaseColor(59, 130, 246);

        // ── Fonts ──────────────────────────────────────────────────────
        private static readonly Font FTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 22, BaseColor.WHITE);
        private static readonly Font FSubtitle = FontFactory.GetFont(FontFactory.HELVETICA, 10, new BaseColor(200, 210, 255));
        private static readonly Font FPhaseTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.WHITE);
        private static readonly Font FPhaseNum = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, BaseColor.WHITE);
        private static readonly Font FLabel = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8.5f, Gray600);
        private static readonly Font FValue = FontFactory.GetFont(FontFactory.HELVETICA, 9.5f, Dark);
        private static readonly Font FSmall = FontFactory.GetFont(FontFactory.HELVETICA, 8, Gray400);
        private static readonly Font FTermsTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, Dark);
        private static readonly Font FTermsIntro = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8.5f, Dark);
        private static readonly Font FTerms = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, new BaseColor(55, 65, 81));
        private static readonly Font FAccOn = FontFactory.GetFont(FontFactory.ZAPFDINGBATS, 11, Green);
        private static readonly Font FAccOff = FontFactory.GetFont(FontFactory.ZAPFDINGBATS, 11, Gray400);
        private static readonly Font FAccLabel = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, Dark);
        private static readonly Font FAccLblOff = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, Gray400);
        private static readonly Font FMetaLabel = FontFactory.GetFont(FontFactory.HELVETICA, 7.5f, Gray400);
        private static readonly Font FMetaValue = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, Dark);
        private static readonly Font FSigRole = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7.5f, Gray600);
        private static readonly Font FSigName = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, Dark);
        private static readonly Font FSigDate = FontFactory.GetFont(FontFactory.HELVETICA, 8, Gray600);
        private static readonly Font FBadge = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
        private static readonly Font FVerifyOk = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, GreenDark);
        private static readonly Font FVerifyNo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, AmberDark);

        // ================================================================
        public void ProcessRequest(HttpContext context)
        {
            string idStr = context.Request.QueryString["id"];
            int agreementId;
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out agreementId))
            { context.Response.StatusCode = 400; context.Response.Write("Invalid agreement ID."); return; }

            DataRow data = GetAgreementData(agreementId);
            if (data == null)
            { context.Response.StatusCode = 404; context.Response.Write("Agreement not found."); return; }

            byte[] pdfBytes = GeneratePDF(data);
            string fileName = "Agreement_" + data["agreement_number"].ToString().Replace("-", "_") + ".pdf";

            context.Response.ContentType = "application/pdf";
            context.Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
            context.Response.BinaryWrite(pdfBytes);
            context.Response.End();
        }

        private DataRow GetAgreementData(int id)
        {
            string cs = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT a.*, m.model, m.type AS model_type FROM hardware_agreements a LEFT JOIN hardware_model m ON a.model_id = m.id WHERE a.id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    DataTable dt = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0] : null;
                }
            }
        }

        // ================================================================
        // GENERATE PDF
        // ================================================================
        private byte[] GeneratePDF(DataRow d)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 36, 36, 36, 56);
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);
                string agrNum = S(d, "agreement_number");
                writer.PageEvent = new PdfFooterEvent(agrNum);
                doc.Open();

                string status = S(d, "agreement_status");

                // HEADER
                BuildHeader(doc, writer, agrNum, status);
                BuildMetaBar(doc, d);
                Sp(doc, 10);

                // PHASE 1
                PhaseBar(doc, "PHASE 1", "Raise Agreement \u2014 Hardware Details", Primary, PrimaryDark);
                HardwareTable(doc, d);
                Accessories(doc, d);
                Sp(doc, 12);

                // PHASE 2 - Always show
                PhaseBar(doc, "PHASE 2", "Employee Agreement", Amber, new BaseColor(217, 119, 6));
                Terms(doc);
                EmployeeDetails(doc, d);
                Sp(doc, 12);

                // PHASE 3 - Always show
                PhaseBar(doc, "PHASE 3", "IT Verification", Green, new BaseColor(5, 150, 105));
                Verification(doc, d);
                Sp(doc, 12);

                // SIGNATURES
                Signatures(doc, d);

                doc.Close();
                return ms.ToArray();
            }
        }

        // ── HEADER ─────────────────────────────────────────────────────
        private void BuildHeader(Document doc, PdfWriter writer, string agrNum, string status)
        {
            PdfContentByte cb = writer.DirectContentUnder;
            float top = doc.PageSize.Height - doc.TopMargin;
            float h = 72;
            float w = doc.PageSize.Width - doc.LeftMargin - doc.RightMargin;

            // Main gradient band
            cb.SetColorFill(Primary);
            cb.Rectangle(doc.LeftMargin, top - h, w, h);
            cb.Fill();

            // Accent diagonal stripe
            cb.SetColorFill(PrimaryDark);
            float sw = 180;
            cb.MoveTo(doc.PageSize.Width - doc.RightMargin - sw, top);
            cb.LineTo(doc.PageSize.Width - doc.RightMargin, top);
            cb.LineTo(doc.PageSize.Width - doc.RightMargin, top - h);
            cb.LineTo(doc.PageSize.Width - doc.RightMargin - sw - 40, top - h);
            cb.Fill();

            PdfPTable ht = new PdfPTable(2);
            ht.WidthPercentage = 100;
            ht.SetWidths(new float[] { 65, 35 });

            PdfPCell lc = new PdfPCell();
            lc.Border = Rectangle.NO_BORDER;
            lc.PaddingTop = 14; lc.PaddingBottom = 12; lc.PaddingLeft = 8;
            lc.FixedHeight = h;
            lc.VerticalAlignment = Element.ALIGN_MIDDLE;
            lc.AddElement(new Paragraph("HARDWARE AGREEMENT", FTitle));
            lc.AddElement(new Paragraph("Laptop / PC Equipment Issuance & Usage Agreement", FSubtitle));
            ht.AddCell(lc);

            PdfPCell rc = new PdfPCell();
            rc.Border = Rectangle.NO_BORDER;
            rc.FixedHeight = h;
            rc.VerticalAlignment = Element.ALIGN_MIDDLE;
            rc.HorizontalAlignment = Element.ALIGN_RIGHT;
            rc.PaddingRight = 8;

            PdfPTable badge = new PdfPTable(1);
            badge.HorizontalAlignment = Element.ALIGN_RIGHT;
            PdfPCell bc = new PdfPCell(new Phrase(status.ToUpper(), FBadge));
            bc.BackgroundColor = GetStatusColor(status);
            bc.HorizontalAlignment = Element.ALIGN_CENTER;
            bc.Padding = 8; bc.PaddingLeft = 22; bc.PaddingRight = 22;
            bc.Border = Rectangle.NO_BORDER;
            badge.AddCell(bc);
            rc.AddElement(badge);
            ht.AddCell(rc);

            doc.Add(ht);
        }

        // ── META BAR ───────────────────────────────────────────────────
        private void BuildMetaBar(Document doc, DataRow d)
        {
            PdfPTable t = new PdfPTable(4);
            t.WidthPercentage = 100;
            t.SetWidths(new float[] { 28, 24, 24, 24 });

            MC(t, "AGREEMENT NUMBER", S(d, "agreement_number"));
            MC(t, "CREATED DATE", D(d, "created_date", "dd MMM yyyy"));
            MC(t, "LAST UPDATED", D(d, "last_updated", "dd MMM yyyy"));
            MC(t, "ISSUE DATE", D(d, "issue_date", "dd MMM yyyy"));
            doc.Add(t);
        }

        private void MC(PdfPTable t, string label, string val)
        {
            PdfPCell c = new PdfPCell();
            c.BackgroundColor = Gray50; c.Border = Rectangle.BOX;
            c.BorderColor = Gray200; c.BorderWidth = 0.5f; c.Padding = 10;
            c.AddElement(new Paragraph(label, FMetaLabel));
            c.AddElement(new Paragraph(val, FMetaValue));
            t.AddCell(c);
        }

        // ── PHASE BAR ──────────────────────────────────────────────────
        private void PhaseBar(Document doc, string phase, string title, BaseColor c1, BaseColor c2)
        {
            PdfPTable t = new PdfPTable(new float[] { 12, 88 });
            t.WidthPercentage = 100; t.SpacingBefore = 4;

            PdfPCell nc = new PdfPCell(new Phrase(phase, FPhaseNum));
            nc.BackgroundColor = c2; nc.HorizontalAlignment = Element.ALIGN_CENTER;
            nc.VerticalAlignment = Element.ALIGN_MIDDLE; nc.Padding = 10;
            nc.Border = Rectangle.NO_BORDER;
            t.AddCell(nc);

            PdfPCell tc = new PdfPCell(new Phrase(title, FPhaseTitle));
            tc.BackgroundColor = c1; tc.VerticalAlignment = Element.ALIGN_MIDDLE;
            tc.PaddingLeft = 14; tc.Padding = 10; tc.Border = Rectangle.NO_BORDER;
            t.AddCell(tc);
            doc.Add(t);
        }

        // ── HARDWARE TABLE ─────────────────────────────────────────────
        private void HardwareTable(Document doc, DataRow d)
        {
            PdfPTable t = new PdfPTable(4);
            t.WidthPercentage = 100;
            t.SetWidths(new float[] { 20, 30, 20, 30 });

            R4(t, "Model", S(d, "model"), "Model Type", S(d, "model_type"));
            R4(t, "Serial Number", S(d, "serial_number"), "Asset Number", S(d, "asset_number"));
            R4(t, "IT Staff (Windows ID)", S(d, "it_staff_win_id"), "Issue Date", D(d, "issue_date", "dd MMM yyyy"));
            R4(t, "Employee Email", S(d, "employee_email"), "HOD Email", S(d, "hod_email"));

            string remarks = S(d, "remarks");
            if (!string.IsNullOrEmpty(remarks))
            {
                LC(t, "Remarks");
                PdfPCell vc = VC(remarks); vc.Colspan = 3; t.AddCell(vc);
            }
            doc.Add(t);
        }

        // ── ACCESSORIES ────────────────────────────────────────────────
        private void Accessories(Document doc, DataRow d)
        {
            PdfPTable t = new PdfPTable(1);
            t.WidthPercentage = 100;

            PdfPCell hdr = new PdfPCell(new Phrase("ACCESSORIES INCLUDED", FLabel));
            hdr.BackgroundColor = Gray100; hdr.Padding = 8;
            hdr.Border = Rectangle.BOX; hdr.BorderColor = Gray200; hdr.BorderWidth = 0.5f;
            t.AddCell(hdr);

            PdfPCell row = new PdfPCell();
            row.Border = Rectangle.BOX; row.BorderColor = Gray200; row.BorderWidth = 0.5f; row.Padding = 10;

            PdfPTable bg = new PdfPTable(4);
            bg.WidthPercentage = 100;

            AB(bg, "Carry Bag", B(d, "has_carry_bag"));
            AB(bg, "Power Adapter", B(d, "has_power_adapter"));
            string ml = "Mouse";
            if (B(d, "has_mouse")) { string mt = S(d, "mouse_type"); if (!string.IsNullOrEmpty(mt)) ml += " (" + mt + ")"; }
            AB(bg, ml, B(d, "has_mouse"));
            AB(bg, "VGA Converter", B(d, "has_vga_converter"));
            row.AddElement(bg);

            string other = S(d, "other_accessories");
            if (!string.IsNullOrEmpty(other))
            { Paragraph op = new Paragraph("Other: " + other, FSmall); op.SpacingBefore = 6; row.AddElement(op); }

            t.AddCell(row);
            doc.Add(t);
        }

        private void AB(PdfPTable t, string name, bool on)
        {
            string icon = on ? "\u0034" : "\u0038"; // ZapfDingbats check/cross
            PdfPCell c = new PdfPCell();
            c.Border = Rectangle.BOX;
            c.BorderColor = on ? new BaseColor(167, 243, 208) : Gray200;
            c.BackgroundColor = on ? GreenBg : Gray50;
            c.BorderWidth = 1f; c.Padding = 8;
            c.HorizontalAlignment = Element.ALIGN_CENTER;
            Phrase p = new Phrase();
            p.Add(new Chunk(icon, on ? FAccOn : FAccOff));
            p.Add(new Chunk("  " + name, on ? FAccLabel : FAccLblOff));
            c.Phrase = p;
            t.AddCell(c);
        }

        // ── TERMS ──────────────────────────────────────────────────────
        private void Terms(Document doc)
        {
            PdfPTable t = new PdfPTable(1); t.WidthPercentage = 100;
            PdfPCell c = new PdfPCell();
            c.BackgroundColor = Gray50; c.Border = Rectangle.BOX;
            c.BorderColor = Gray200; c.BorderWidth = 0.5f; c.Padding = 16;

            c.AddElement(new Paragraph("Laptop / PC Usage Agreement", FTermsTitle) { SpacingAfter = 8 });
            c.AddElement(new Paragraph("In acceptance of this device (Laptop/PC) for usage, I agree to the terms and conditions stated below:", FTermsIntro) { SpacingAfter = 10 });

            string[] terms = {
                "I understand that I am responsible for the laptop/PC whilst in my possession.",
                "I am responsible for keeping the laptop/PC in good condition while using it and until the time of return.",
                "I understand that I should not install any program or software that is not permitted to use by the company, for privacy and security reasons.",
                "I should be the only authorized person to have access to and use this laptop/PC. Any unauthorized access to this laptop/PC is a violation of this company\u2019s policy, employment regulation and employment contract.",
                "I should remove all data that is not company or work-related before turning over the laptop/PC to the designated department.",
                "In the event of loss, theft, or damage, this must be reported to the police within 24\u201348 hours, and a copy of a Police report or incident report must be submitted to the company for verification purposes.",
                "I understand that any violation of these policies is a violation and I am subject to any disciplinary action by the company."
            };
            for (int i = 0; i < terms.Length; i++)
            { Paragraph p = new Paragraph((i + 1) + ".  " + terms[i], FTerms); p.IndentationLeft = 8; p.SpacingAfter = 4; c.AddElement(p); }

            t.AddCell(c);
            doc.Add(t);
        }

        // ── EMPLOYEE DETAILS ───────────────────────────────────────────
        private void EmployeeDetails(Document doc, DataRow d)
        {
            PdfPTable t = T2();
            R2(t, "Employee Name", S(d, "employee_name"));
            R2(t, "Employee ID", S(d, "employee_id"));
            R2(t, "Position / Job Title", S(d, "employee_position"));
            R2(t, "Department", S(d, "employee_department"));
            R2(t, "Agreed Date", D(d, "employee_agreed_date", "dd MMM yyyy HH:mm"));
            R2(t, "Terms Accepted", "\u2713 I have read, understood, and agree to all terms and conditions");
            doc.Add(t);
        }

        // ── VERIFICATION ───────────────────────────────────────────────
        private void Verification(Document doc, DataRow d)
        {
            PdfPTable vb = new PdfPTable(2);
            vb.WidthPercentage = 100;
            VB(vb, "Hardware Checklist", B(d, "it_verify_hardware_checklist"));
            VB(vb, "System Configuration", B(d, "it_verify_system_config"));
            doc.Add(vb);

            PdfPTable t = T2();
            string others = S(d, "it_verify_others");
            if (!string.IsNullOrEmpty(others)) R2(t, "Additional Notes", others);
            R2(t, "Verified By", S(d, "it_verified_by"));
            R2(t, "Verification Date", D(d, "it_verified_date", "dd MMM yyyy HH:mm"));
            doc.Add(t);
        }

        private void VB(PdfPTable t, string label, bool ok)
        {
            PdfPCell c = new PdfPCell();
            c.BackgroundColor = ok ? GreenBg : AmberBg;
            c.Border = Rectangle.BOX;
            c.BorderColor = ok ? new BaseColor(167, 243, 208) : new BaseColor(253, 230, 138);
            c.BorderWidth = 1f; c.Padding = 12; c.HorizontalAlignment = Element.ALIGN_CENTER;
            string txt = ok ? "\u2713  " + label + ": Completed" : "\u2717  " + label + ": Not completed";
            c.Phrase = new Phrase(txt, ok ? FVerifyOk : FVerifyNo);
            t.AddCell(c);
        }

        // ── SIGNATURES ─────────────────────────────────────────────────
        private void Signatures(Document doc, DataRow d)
        {
            PdfPTable t = new PdfPTable(3);
            t.WidthPercentage = 100;
            t.SetWidths(new float[] { 33, 34, 33 });
            t.SpacingBefore = 16;

            SC(t, "Employee", S(d, "employee_name"), D(d, "employee_agreed_date", "dd/MM/yyyy"));
            SC(t, "IT Staff", S(d, "it_staff_win_id"), D(d, "issue_date", "dd/MM/yyyy"));
            SC(t, "Verified By", S(d, "it_verified_by"), D(d, "it_verified_date", "dd/MM/yyyy"));
            doc.Add(t);
        }

        private void SC(PdfPTable t, string role, string name, string date)
        {
            PdfPCell c = new PdfPCell();
            c.Border = Rectangle.BOX; c.BorderColor = Gray200; c.BorderWidth = 0.5f;
            c.Padding = 14; c.PaddingTop = 10;

            Paragraph rp = new Paragraph(role.ToUpper(), FSigRole); rp.Alignment = Element.ALIGN_CENTER;
            c.AddElement(rp);
            c.AddElement(new Paragraph(" ") { SpacingAfter = 28 });

            PdfPTable line = new PdfPTable(1); line.WidthPercentage = 80;
            line.HorizontalAlignment = Element.ALIGN_CENTER;
            PdfPCell lcc = new PdfPCell(); lcc.Border = Rectangle.TOP_BORDER;
            lcc.BorderColorTop = Dark; lcc.BorderWidthTop = 1f; lcc.FixedHeight = 1;
            line.AddCell(lcc);
            c.AddElement(line);

            if (!string.IsNullOrEmpty(name))
            { Paragraph np = new Paragraph(name, FSigName); np.Alignment = Element.ALIGN_CENTER; np.SpacingBefore = 6; c.AddElement(np); }

            string ds = string.IsNullOrEmpty(date) || date == "\u2014" ? "\u2014" : date;
            Paragraph dp = new Paragraph("Date: " + ds, FSigDate); dp.Alignment = Element.ALIGN_CENTER; dp.SpacingBefore = 4;
            c.AddElement(dp);
            t.AddCell(c);
        }

        // ── TABLE HELPERS ──────────────────────────────────────────────
        private PdfPTable T2() { PdfPTable t = new PdfPTable(2); t.WidthPercentage = 100; t.SetWidths(new float[] { 28, 72 }); return t; }
        private void R2(PdfPTable t, string l, string v) { LC(t, l); t.AddCell(VC(v)); }
        private void R4(PdfPTable t, string l1, string v1, string l2, string v2) { LC(t, l1); t.AddCell(VC(v1)); LC(t, l2); t.AddCell(VC(v2)); }

        private void LC(PdfPTable t, string label)
        {
            PdfPCell c = new PdfPCell(new Phrase(label, FLabel));
            c.BackgroundColor = Gray100; c.Padding = 9;
            c.Border = Rectangle.BOX; c.BorderColor = Gray200; c.BorderWidth = 0.5f;
            t.AddCell(c);
        }

        private PdfPCell VC(string val)
        {
            PdfPCell c = new PdfPCell(new Phrase(string.IsNullOrEmpty(val) ? "\u2014" : val, FValue));
            c.Padding = 9; c.Border = Rectangle.BOX; c.BorderColor = Gray200; c.BorderWidth = 0.5f;
            return c;
        }

        private void Sp(Document doc, float h) { doc.Add(new Paragraph(" ") { SpacingAfter = h }); }

        // ── DATA HELPERS ───────────────────────────────────────────────
        private string S(DataRow r, string c) { try { if (r.Table.Columns.Contains(c) && r[c] != null && r[c] != DBNull.Value) return r[c].ToString(); } catch { } return ""; }
        private string D(DataRow r, string c, string f) { try { if (r.Table.Columns.Contains(c) && r[c] != null && r[c] != DBNull.Value) return Convert.ToDateTime(r[c]).ToString(f); } catch { } return "\u2014"; }
        private bool B(DataRow r, string c) { try { if (r.Table.Columns.Contains(c) && r[c] != null && r[c] != DBNull.Value) return Convert.ToBoolean(r[c]); } catch { } return false; }

        private BaseColor GetStatusColor(string s)
        {
            switch (s) { case "Draft": return Gray600; case "Pending": return Blue; case "Agreed": return Amber; case "Completed": return Green; default: return Gray600; }
        }

        public bool IsReusable { get { return false; } }
    }

    // ================================================================
    // PAGE FOOTER
    // ================================================================
    public class PdfFooterEvent : PdfPageEventHelper
    {
        private readonly string _num;
        private readonly Font _f = FontFactory.GetFont(FontFactory.HELVETICA, 7, new BaseColor(148, 163, 184));
        public PdfFooterEvent(string num) { _num = num; }

        public override void OnEndPage(PdfWriter w, Document d)
        {
            PdfPTable t = new PdfPTable(3);
            t.TotalWidth = d.PageSize.Width - d.LeftMargin - d.RightMargin;
            t.SetWidths(new float[] { 40, 30, 30 });
            t.AddCell(FC("Hardware Agreement System  |  Confidential", Element.ALIGN_LEFT));
            t.AddCell(FC(_num, Element.ALIGN_CENTER));
            t.AddCell(FC("Page " + w.PageNumber + "  |  Generated: " + DateTime.Now.ToString("dd MMM yyyy HH:mm"), Element.ALIGN_RIGHT));
            t.WriteSelectedRows(0, -1, d.LeftMargin, d.BottomMargin - 5, w.DirectContent);
        }

        private PdfPCell FC(string txt, int a)
        {
            PdfPCell c = new PdfPCell(new Phrase(txt, _f));
            c.Border = Rectangle.TOP_BORDER; c.BorderColorTop = new BaseColor(226, 232, 240);
            c.BorderWidthTop = 0.5f; c.PaddingTop = 6; c.HorizontalAlignment = a;
            return c;
        }
    }
}