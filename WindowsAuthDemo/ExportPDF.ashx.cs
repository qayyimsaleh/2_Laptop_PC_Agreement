using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.SessionState;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace WindowsAuthDemo
{
    public class ExportPDF : IHttpHandler, IRequiresSessionState
    {
        // ── Brand Colors — Guideline ink/paper palette ────────────────
        private static readonly BaseColor Primary = new BaseColor(26, 79, 214);     // --primary #1a4fd6
        private static readonly BaseColor PrimaryDark = new BaseColor(91, 33, 182);    // --secondary #5b21b6
        private static readonly BaseColor Ink = new BaseColor(15, 17, 23);     // --ink #0f1117
        private static readonly BaseColor Paper = new BaseColor(245, 243, 238);  // --paper #f5f3ee
        private static readonly BaseColor Cream = new BaseColor(237, 233, 225);  // --cream #ede9e1
        private static readonly BaseColor Line = new BaseColor(203, 197, 184);  // --line #cbc5b8
        private static readonly BaseColor Dark = new BaseColor(15, 17, 23);     // ink
        private static readonly BaseColor Gray600 = new BaseColor(74, 85, 104);
        private static readonly BaseColor Gray400 = new BaseColor(113, 120, 133);
        private static readonly BaseColor Gray200 = new BaseColor(203, 197, 184);  // --line
        private static readonly BaseColor Gray100 = new BaseColor(237, 233, 225);  // --cream
        private static readonly BaseColor Gray50 = new BaseColor(245, 243, 238);  // --paper
        private static readonly BaseColor Green = new BaseColor(13, 138, 94);    // --success
        private static readonly BaseColor GreenDark = new BaseColor(6, 78, 53);
        private static readonly BaseColor GreenBg = new BaseColor(208, 245, 232);
        private static readonly BaseColor Amber = new BaseColor(192, 92, 0);     // --warning
        private static readonly BaseColor AmberDark = new BaseColor(120, 53, 0);
        private static readonly BaseColor AmberBg = new BaseColor(255, 237, 213);
        private static readonly BaseColor Blue = new BaseColor(26, 79, 214);

        // ── Fonts — guideline style ─────────────────────────────────────
        private static readonly Font FTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, BaseColor.WHITE);
        private static readonly Font FSubtitle = FontFactory.GetFont(FontFactory.HELVETICA, 9, new BaseColor(200, 210, 230));
        private static readonly Font FPhaseTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
        private static readonly Font FPhaseNum = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7, new BaseColor(200, 210, 230));
        private static readonly Font FLabel = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7.5f, Gray600);
        private static readonly Font FValue = FontFactory.GetFont(FontFactory.HELVETICA, 9.5f, Dark);
        private static readonly Font FSmall = FontFactory.GetFont(FontFactory.HELVETICA, 7.5f, Gray400);
        private static readonly Font FTermsTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9.5f, Dark);
        private static readonly Font FTermsIntro = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, Gray600);
        private static readonly Font FTerms = FontFactory.GetFont(FontFactory.HELVETICA, 8f, Gray600);
        private static readonly Font FAccOn = FontFactory.GetFont(FontFactory.ZAPFDINGBATS, 10, Green);
        private static readonly Font FAccOff = FontFactory.GetFont(FontFactory.ZAPFDINGBATS, 10, Gray400);
        private static readonly Font FAccLabel = FontFactory.GetFont(FontFactory.HELVETICA, 8f, Dark);
        private static readonly Font FAccLblOff = FontFactory.GetFont(FontFactory.HELVETICA, 8f, Gray400);
        private static readonly Font FMetaLabel = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 6.5f, Gray400);
        private static readonly Font FMetaValue = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9f, Dark);
        private static readonly Font FSigRole = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7f, Gray600);
        private static readonly Font FSigName = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9.5f, Dark);
        private static readonly Font FSigDate = FontFactory.GetFont(FontFactory.HELVETICA, 7.5f, Gray600);
        private static readonly Font FBadge = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8.5f, BaseColor.WHITE);
        private static readonly Font FVerifyOk = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8.5f, GreenDark);
        private static readonly Font FVerifyNo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8.5f, AmberDark);

        // ================================================================
        public void ProcessRequest(HttpContext context)
        {
            // ── Security: require authentication ────────────────────────
            if (!context.User.Identity.IsAuthenticated)
            { context.Response.StatusCode = 401; context.Response.Write("Unauthorized."); return; }

            // ── Security headers ────────────────────────────────────────
            context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            string idStr = context.Request.QueryString["id"];
            int agreementId;
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out agreementId))
            { context.Response.StatusCode = 400; context.Response.Write("Invalid acknowledgement ID."); return; }

            // ── Security: only admins or the acknowledgement receipt's employee may export ─
            if (!CanUserExport(context, agreementId))
            { context.Response.StatusCode = 403; context.Response.Write("You do not have permission to export this acknowledgement receipt."); return; }

            DataRow data = GetAgreementData(agreementId);
            if (data == null)
            { context.Response.StatusCode = 404; context.Response.Write("Acknowledgement Receipt not found."); return; }

            byte[] pdfBytes = GeneratePDF(data);
            string fileName = "Receipt_" + data["agreement_number"].ToString().Replace("-", "_") + ".pdf";

            context.Response.ContentType = "application/pdf";
            context.Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
            context.Response.BinaryWrite(pdfBytes);
            context.Response.End();
        }

        /// <summary>
        /// Returns true if the current user is an admin OR is the employee
        /// who owns the acknowledgement receipt. Fail-closed: returns false on any error.
        /// </summary>
        private bool CanUserExport(HttpContext context, int agreementId)
        {
            string cs = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
            string winId = context.User.Identity.Name;

            using (SqlConnection conn = new SqlConnection(cs))
            {
                try
                {
                    conn.Open();

                    // Check if user is admin
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT admin FROM hardware_users WHERE win_id = @w AND active = 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@w", winId);
                        object result = cmd.ExecuteScalar();
                        if (result != null && Convert.ToInt32(result) == 1)
                            return true; // admin can export any agreement
                    }

                    // Not admin — check if user's email matches the acknowledgement receipt's employee_email
                    string userEmail = "";
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT email FROM hardware_users WHERE win_id = @w AND active = 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@w", winId);
                        object result = cmd.ExecuteScalar();
                        userEmail = (result != null && result != DBNull.Value) ? result.ToString().Trim() : "";
                    }

                    if (string.IsNullOrEmpty(userEmail)) return false;

                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM hardware_agreements WHERE id = @id AND employee_email = @e", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", agreementId);
                        cmd.Parameters.AddWithValue("@e", userEmail);
                        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("ExportPDF.CanUserExport error: " + ex.Message);
                    return false; // fail closed
                }
            }
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
                PhaseBar(doc, "PHASE 1", "Raise Acknowledgement Receipt \u2014 Hardware Details", Primary, PrimaryDark);
                HardwareTable(doc, d);
                Accessories(doc, d);
                Sp(doc, 12);

                // PHASE 2 - Always show
                PhaseBar(doc, "PHASE 2", "Employee Acknowledgement", Amber, new BaseColor(217, 119, 6));
                EmployeeDetails(doc, d);
                Sp(doc, 12);

                // PHASE 3 - Always show
                PhaseBar(doc, "PHASE 3", "IT Verification", Green, new BaseColor(5, 150, 105));
                Verification(doc, d);
                Sp(doc, 12);

                // SIGNATURES
                Signatures(doc, d);

                // ARCHIVE REMARKS — only shown when acknowledgement receipt is Archived
                if (status == "Archived")
                {
                    string archiveRemarks = S(d, "archive_remarks");
                    if (!string.IsNullOrEmpty(archiveRemarks))
                    {
                        Sp(doc, 12);
                        ArchiveRemarksSection(doc, archiveRemarks);
                    }
                }

                doc.Close();
                return ms.ToArray();
            }
        }

        // ── HEADER — ink dark band, guideline style ─────────────────────
        private void BuildHeader(Document doc, PdfWriter writer, string agrNum, string status)
        {
            PdfContentByte cb = writer.DirectContentUnder;
            float top = doc.PageSize.Height - doc.TopMargin;
            float h = 68;
            float w = doc.PageSize.Width - doc.LeftMargin - doc.RightMargin;

            // Solid ink header band
            cb.SetColorFill(Ink);
            cb.Rectangle(doc.LeftMargin, top - h, w, h);
            cb.Fill();

            // Thin primary accent strip at very top
            cb.SetColorFill(Primary);
            cb.Rectangle(doc.LeftMargin, top - 3, w, 3);
            cb.Fill();

            PdfPTable ht = new PdfPTable(2);
            ht.WidthPercentage = 100;
            ht.SetWidths(new float[] { 65, 35 });

            PdfPCell lc = new PdfPCell();
            lc.Border = Rectangle.NO_BORDER;
            lc.BackgroundColor = Ink;
            lc.PaddingTop = 14; lc.PaddingBottom = 12; lc.PaddingLeft = 8;
            lc.FixedHeight = h;
            lc.VerticalAlignment = Element.ALIGN_MIDDLE;
            lc.AddElement(new Paragraph("Laptop / Dekstop Acknowledgement Receipt", FTitle));
            lc.AddElement(new Paragraph("Laptop / Dekstop Equipment Issuance & Usage Acknowledgement Receipt", FSubtitle));
            ht.AddCell(lc);

            PdfPCell rc = new PdfPCell();
            rc.Border = Rectangle.NO_BORDER;
            rc.BackgroundColor = Ink;
            rc.FixedHeight = h;
            rc.VerticalAlignment = Element.ALIGN_MIDDLE;
            rc.HorizontalAlignment = Element.ALIGN_RIGHT;
            rc.PaddingRight = 10;

            // Status badge — pill style with border
            PdfPTable badge = new PdfPTable(1);
            badge.HorizontalAlignment = Element.ALIGN_RIGHT;
            PdfPCell bc = new PdfPCell(new Phrase(status.ToUpper(), FBadge));
            bc.BackgroundColor = GetStatusBadgeBg(status);
            bc.HorizontalAlignment = Element.ALIGN_CENTER;
            bc.Padding = 6; bc.PaddingLeft = 16; bc.PaddingRight = 16;
            bc.Border = Rectangle.BOX;
            bc.BorderColor = GetStatusColor(status);
            bc.BorderWidth = 1.5f;
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

            MC(t, "ACKNOWLEDGEMENT RECEIPT NUMBER", S(d, "agreement_number"));
            MC(t, "CREATED DATE", D(d, "created_date", "dd MMM yyyy"));
            MC(t, "LAST UPDATED", D(d, "last_updated", "dd MMM yyyy"));
            MC(t, "ISSUE DATE", D(d, "issue_date", "dd MMM yyyy"));
            doc.Add(t);
        }

        private void MC(PdfPTable t, string label, string val)
        {
            PdfPCell c = new PdfPCell();
            c.BackgroundColor = Paper; c.Border = Rectangle.BOX;
            c.BorderColor = Line; c.BorderWidth = 1f; c.Padding = 10;
            c.AddElement(new Paragraph(label, FMetaLabel));
            c.AddElement(new Paragraph(val, FMetaValue));
            t.AddCell(c);
        }

        // ── PHASE BAR — ink solid, guideline style ──────────────────────
        private void PhaseBar(Document doc, string phase, string title, BaseColor c1, BaseColor c2)
        {
            // Spacer before each phase
            Sp(doc, 4);

            PdfPTable t = new PdfPTable(new float[] { 10, 90 });
            t.WidthPercentage = 100;

            // Phase number cell — accent colour background
            PdfPCell nc = new PdfPCell(new Phrase(phase, FPhaseNum));
            nc.BackgroundColor = c2;
            nc.HorizontalAlignment = Element.ALIGN_CENTER;
            nc.VerticalAlignment = Element.ALIGN_MIDDLE;
            nc.Padding = 9;
            nc.Border = Rectangle.NO_BORDER;
            t.AddCell(nc);

            // Title cell — ink background
            PdfPCell tc = new PdfPCell(new Phrase(title, FPhaseTitle));
            tc.BackgroundColor = Ink;
            tc.VerticalAlignment = Element.ALIGN_MIDDLE;
            tc.PaddingLeft = 12; tc.Padding = 9;
            tc.Border = Rectangle.NO_BORDER;
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
            hdr.BackgroundColor = Ink; hdr.Padding = 8;
            // override FLabel colour for the ink header
            Paragraph aHdr = new Paragraph("ACCESSORIES INCLUDED",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7.5f, new BaseColor(200, 210, 230)));
            hdr = new PdfPCell();
            hdr.BackgroundColor = Ink; hdr.Padding = 8;
            hdr.Border = Rectangle.BOX; hdr.BorderColor = Ink; hdr.BorderWidth = 1f;
            hdr.AddElement(aHdr);
            t.AddCell(hdr);

            PdfPCell row = new PdfPCell();
            row.Border = Rectangle.BOX; row.BorderColor = Line; row.BorderWidth = 0.75f; row.Padding = 10;
            row.BackgroundColor = Paper;

            PdfPTable bg = new PdfPTable(4);
            bg.WidthPercentage = 100;

            AB(bg, "Carry Bag", B(d, "has_carry_bag"));
            AB(bg, "Power Adapter", B(d, "has_power_adapter"));
            string ml = "Mouse";
            if (B(d, "has_mouse")) { string mt = S(d, "mouse_type"); if (!string.IsNullOrEmpty(mt)) ml += " (" + mt + ")"; }
            AB(bg, ml, B(d, "has_mouse"));
            AB(bg, "VGA Converter / HDMI", B(d, "has_vga_converter"));
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
            c.BorderColor = on ? new BaseColor(167, 220, 195) : Line;
            c.BackgroundColor = on ? GreenBg : Paper;
            c.BorderWidth = 0.75f; c.Padding = 8;
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
            c.BackgroundColor = Paper; c.Border = Rectangle.BOX;
            c.BorderColor = Line; c.BorderWidth = 0.75f; c.Padding = 16;

            c.AddElement(new Paragraph("Laptop & Desktop Usage Acknowledgement Receipt", FTermsTitle) { SpacingAfter = 8 });
            c.AddElement(new Paragraph("In acceptance of this device (Laptop/Desktop) for usage, I acknowledge the receipt stated below:", FTermsIntro) { SpacingAfter = 10 });

            string[] terms = {
                "I understand that I am responsible for the laptop/desktop whilst in my possession.",
                "I am responsible for keeping the laptop/desktop in good condition while using it and until the time of return.",
                "I understand that I should not install any program or software that is not permitted to use by the company, for privacy and security reasons.",
                "I should be the only authorized person to have access to and use this laptop/desktop, any unauthorized access to this laptop/desktop is a violation of this acknowledgement receipt.",
                "This laptop/desktop shall be use for work-related purpose only and shall not be for personal use.",
                "In the event of loss, theft, or damage, this must be reported to the police within 24\u201348 hours, and a copy of a Police report or incident report must be submitted to the company for verification purposes.",
                "I understand that I may be held financially responsible for full or partial cost of repair or replacement if the device and its accessories was damaged, lost or rendered unusable due to my carelessness, negligence, or misuse.",
                "I understand that any violation of acknowledgement receipt terms is a breach that may be subjected to disciplinary action by the company."
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
            R2(t, "Employee ID", S(d, "employee_staff_id"));
            R2(t, "Windows ID", S(d, "employee_id"));
            R2(t, "Position / Job Title", S(d, "employee_position"));
            R2(t, "Department", S(d, "employee_department"));
            R2(t, "Acknowledged Date", D(d, "employee_agreed_date", "dd MMM yyyy HH:mm"));
            R2(t, "Receipt Acknowledged", "\u2713 I acknowledge that I have received the laptop/desktop and all listed accessories in good condition");
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
            c.BorderColor = ok ? new BaseColor(100, 180, 140) : new BaseColor(180, 130, 60);
            c.BorderWidth = 1f; c.Padding = 12; c.HorizontalAlignment = Element.ALIGN_CENTER;
            string txt = ok ? "\u2713  " + label + ": Completed" : "\u2717  " + label + ": Not completed";
            c.Phrase = new Phrase(txt, ok ? FVerifyOk : FVerifyNo);
            t.AddCell(c);
        }

        // ── DIGITAL ACKNOWLEDGEMENT TABLE ──────────────────────────────
        private void Signatures(Document doc, DataRow d)
        {
            Sp(doc, 8);

            // Section header bar
            PdfPTable hdr = new PdfPTable(1);
            hdr.WidthPercentage = 100;
            PdfPCell hc = new PdfPCell();
            hc.BackgroundColor = Ink; hc.Border = Rectangle.NO_BORDER; hc.Padding = 9;
            hc.AddElement(new Paragraph("DIGITAL ACKNOWLEDGEMENT RECORD",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7.5f, new BaseColor(200, 210, 230))));
            hdr.AddCell(hc);
            doc.Add(hdr);

            // 3-column record: Employee | IT Staff | Verified By
            PdfPTable t = new PdfPTable(3);
            t.WidthPercentage = 100;
            t.SetWidths(new float[] { 33, 34, 33 });

            SR(t, "Employee",
               S(d, "employee_name"),
               D(d, "employee_agreed_date", "dd MMM yyyy  HH:mm"));
            SR(t, "IT Staff",
               S(d, "it_staff_win_id"),
               D(d, "issue_date", "dd MMM yyyy"));
            SR(t, "Verified By",
               S(d, "it_verified_by"),
               D(d, "it_verified_date", "dd MMM yyyy  HH:mm"));
            doc.Add(t);

            // Digital closing statement
            Sp(doc, 14);
            DigitalClosing(doc, d);
        }

        private void SR(PdfPTable t, string role, string name, string date)
        {
            PdfPCell c = new PdfPCell();
            c.BackgroundColor = Paper;
            c.Border = Rectangle.BOX; c.BorderColor = Line; c.BorderWidth = 0.75f;
            c.Padding = 12;

            // Role label
            Paragraph rp = new Paragraph(role.ToUpper(),
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 6.5f, Gray400));
            rp.Alignment = Element.ALIGN_CENTER;
            c.AddElement(rp);

            // Name
            if (!string.IsNullOrEmpty(name))
            {
                Paragraph np = new Paragraph(name, FSigName);
                np.Alignment = Element.ALIGN_CENTER;
                np.SpacingBefore = 6;
                c.AddElement(np);
            }

            // Timestamp
            string ds = string.IsNullOrEmpty(date) || date == "\u2014" ? "\u2014" : date;
            Paragraph dp = new Paragraph("\u23f0  " + ds,
                FontFactory.GetFont(FontFactory.HELVETICA, 7.5f, Gray400));
            dp.Alignment = Element.ALIGN_CENTER;
            dp.SpacingBefore = 4;
            c.AddElement(dp);

            t.AddCell(c);
        }

        private void DigitalClosing(Document doc, DataRow d)
        {
            string genDate = DateTime.Now.ToString("dd MMM yyyy, HH:mm");
            string agrNum = S(d, "agreement_number");

            PdfPTable t = new PdfPTable(1);
            t.WidthPercentage = 100;

            PdfPCell c = new PdfPCell();
            c.BackgroundColor = new BaseColor(220, 232, 255);  // light admin-blue
            c.Border = Rectangle.BOX;
            c.BorderColor = Primary;
            c.BorderWidth = 1.5f;
            c.Padding = 14;
            c.PaddingLeft = 18;

            // Main statement
            Paragraph main = new Paragraph(
                "\u2713  This acknowledgement receipt was executed digitally through the Laptop/Dekstop Hardware Portal.",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9.5f, new BaseColor(15, 40, 100)));
            main.SpacingAfter = 6;
            c.AddElement(main);

            // Sub-lines
            Font fsub = FontFactory.GetFont(FontFactory.HELVETICA, 8f, new BaseColor(40, 60, 120));
            c.AddElement(new Paragraph("All parties acknowledged the receipt via the portal. No physical signature is required.", fsub));

            Sp(doc, 2);
            Font fmeta = FontFactory.GetFont(FontFactory.HELVETICA, 7.5f, new BaseColor(80, 100, 150));
            Paragraph meta = new Paragraph(
                "Acknowledgement Receipt No: " + agrNum + "    \u00b7    Generated: " + genDate,
                fmeta);
            meta.SpacingBefore = 8;
            c.AddElement(meta);

            t.AddCell(c);
            doc.Add(t);
        }

        // ── TABLE HELPERS ──────────────────────────────────────────────
        private PdfPTable T2() { PdfPTable t = new PdfPTable(2); t.WidthPercentage = 100; t.SetWidths(new float[] { 28, 72 }); return t; }
        private void R2(PdfPTable t, string l, string v) { LC(t, l); t.AddCell(VC(v)); }
        private void R4(PdfPTable t, string l1, string v1, string l2, string v2) { LC(t, l1); t.AddCell(VC(v1)); LC(t, l2); t.AddCell(VC(v2)); }

        private void LC(PdfPTable t, string label)
        {
            PdfPCell c = new PdfPCell(new Phrase(label.ToUpper(), FLabel));
            c.BackgroundColor = Cream; c.Padding = 9;
            c.Border = Rectangle.BOX; c.BorderColor = Line; c.BorderWidth = 0.75f;
            t.AddCell(c);
        }

        private PdfPCell VC(string val)
        {
            PdfPCell c = new PdfPCell(new Phrase(string.IsNullOrEmpty(val) ? "\u2014" : val, FValue));
            c.BackgroundColor = BaseColor.WHITE;
            c.Padding = 9; c.Border = Rectangle.BOX; c.BorderColor = Line; c.BorderWidth = 0.75f;
            return c;
        }

        private void Sp(Document doc, float h) { doc.Add(new Paragraph(" ") { SpacingAfter = h }); }

        private void ArchiveRemarksSection(Document doc, string remarks)
        {
            // Red phase bar matching PhaseBar style
            BaseColor archiveRed = new BaseColor(185, 28, 28);
            BaseColor archiveRedDk = new BaseColor(127, 20, 20);
            BaseColor archiveBg = new BaseColor(254, 240, 238);

            PdfPTable bar = new PdfPTable(1);
            bar.WidthPercentage = 100;
            PdfPCell bc = new PdfPCell();
            bc.Border = Rectangle.NO_BORDER;
            bc.BackgroundColor = archiveRed;
            bc.Padding = 10;
            Paragraph bp = new Paragraph();
            bp.Add(new Chunk("ARCHIVED  ", new Font(Font.FontFamily.HELVETICA, 8f, Font.BOLD, BaseColor.WHITE)));
            bp.Add(new Chunk("Archive Reason", new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD, BaseColor.WHITE)));
            bc.AddElement(bp);
            bar.AddCell(bc);
            doc.Add(bar);

            // Remarks content box
            PdfPTable t = new PdfPTable(1);
            t.WidthPercentage = 100;
            t.SpacingBefore = 0;
            PdfPCell rc = new PdfPCell();
            rc.Border = Rectangle.BOX;
            rc.BorderColor = archiveRed;
            rc.BorderWidth = 1.5f;
            rc.BackgroundColor = archiveBg;
            rc.Padding = 14;
            rc.AddElement(new Paragraph(remarks,
                new Font(Font.FontFamily.HELVETICA, 10f, Font.NORMAL, new BaseColor(127, 29, 29))));
            t.AddCell(rc);
            doc.Add(t);
        }

        // ── DATA HELPERS ───────────────────────────────────────────────
        private string S(DataRow r, string c) { try { if (r.Table.Columns.Contains(c) && r[c] != null && r[c] != DBNull.Value) return r[c].ToString(); } catch { } return ""; }
        private string D(DataRow r, string c, string f) { try { if (r.Table.Columns.Contains(c) && r[c] != null && r[c] != DBNull.Value) return Convert.ToDateTime(r[c]).ToString(f); } catch { } return "\u2014"; }
        private bool B(DataRow r, string c) { try { if (r.Table.Columns.Contains(c) && r[c] != null && r[c] != DBNull.Value) return Convert.ToBoolean(r[c]); } catch { } return false; }

        // Returns the border/accent colour for status badges
        private BaseColor GetStatusColor(string s)
        {
            switch (s)
            {
                case "Draft": return new BaseColor(113, 120, 133);  // gray
                case "Pending": return new BaseColor(26, 79, 214);    // blue
                case "Agreed": return new BaseColor(192, 92, 0);     // amber
                case "Completed": return new BaseColor(13, 138, 94);    // green
                case "Archived": return new BaseColor(113, 120, 133);
                default: return new BaseColor(113, 120, 133);
            }
        }

        // Returns a soft background for the status pill in the PDF header
        private BaseColor GetStatusBadgeBg(string s)
        {
            switch (s)
            {
                case "Draft": return new BaseColor(45, 50, 60);
                case "Pending": return new BaseColor(20, 40, 80);
                case "Agreed": return new BaseColor(70, 40, 10);
                case "Completed": return new BaseColor(10, 50, 35);
                case "Archived": return new BaseColor(40, 42, 48);
                default: return new BaseColor(45, 50, 60);
            }
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
            t.AddCell(FC("Laptop & Desktop Acknowledgement Receipt System  |  Confidential", Element.ALIGN_LEFT));
            t.AddCell(FC(_num, Element.ALIGN_CENTER));
            t.AddCell(FC("Page " + w.PageNumber + "  |  Generated: " + DateTime.Now.ToString("dd MMM yyyy HH:mm"), Element.ALIGN_RIGHT));
            t.WriteSelectedRows(0, -1, d.LeftMargin, d.BottomMargin - 5, w.DirectContent);
        }

        private PdfPCell FC(string txt, int a)
        {
            PdfPCell c = new PdfPCell(new Phrase(txt, _f));
            c.Border = Rectangle.TOP_BORDER; c.BorderColorTop = new BaseColor(203, 197, 184); // --line
            c.BorderWidthTop = 0.75f; c.PaddingTop = 6; c.HorizontalAlignment = a;
            return c;
        }
    }
}