using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace WindowsAuthDemo
{
    public partial class ExistingAgreements : System.Web.UI.Page
    {
        protected int CurrentPage
        {
            get { return ViewState["CurrentPage"] == null ? 1 : (int)ViewState["CurrentPage"]; }
            set { ViewState["CurrentPage"] = value; }
        }
        protected int PageSize
        {
            get { return ViewState["PageSize"] == null ? 10 : (int)ViewState["PageSize"]; }
            set { ViewState["PageSize"] = value; }
        }
        protected int TotalRecords
        {
            get { return ViewState["TotalRecords"] == null ? 0 : (int)ViewState["TotalRecords"]; }
            set { ViewState["TotalRecords"] = value; }
        }
        protected bool IsAdmin
        {
            get { return ViewState["IsAdmin"] == null ? false : (bool)ViewState["IsAdmin"]; }
            set { ViewState["IsAdmin"] = value; }
        }
        protected string CurrentUserEmail
        {
            get { return ViewState["CurrentUserEmail"] == null ? "" : (string)ViewState["CurrentUserEmail"]; }
            set { ViewState["CurrentUserEmail"] = value; }
        }
        protected string SortColumn
        {
            get { return ViewState["SortColumn"] == null ? "a.created_date" : (string)ViewState["SortColumn"]; }
            set { ViewState["SortColumn"] = value; }
        }
        protected string SortDir
        {
            get { return ViewState["SortDir"] == null ? "DESC" : (string)ViewState["SortDir"]; }
            set { ViewState["SortDir"] = value; }
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            ViewStateUserKey = User.Identity.IsAuthenticated ? User.Identity.Name : Session.SessionID;
        }

        private void SetSecurityHeaders()
        {
            var h = Response.Headers;
            h["X-Frame-Options"] = "SAMEORIGIN";
            h["X-Content-Type-Options"] = "nosniff";
            h["X-XSS-Protection"] = "1; mode=block";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            SetSecurityHeaders();

            // FIX (MED): Always re-check admin status from DB — not just on first load.
            // Without this a demoted/deactivated admin retains IsAdmin=true in ViewState
            // for the rest of their session and can still see all acknowledgement receipts on PostBack.
            CheckUserRoleAndEmail(User.Identity.Name);

            if (!IsPostBack)
            {
                if (string.IsNullOrEmpty(CurrentUserEmail)) { Response.Redirect("Default.aspx"); return; }

                lblUserName.Text = User.Identity.Name;
                lblUser.Text = User.Identity.Name;
                lblStatus.Text = IsAdmin ? "Administrator" : "Normal User";
                lblUserRole.Text = IsAdmin ? "Administrator" : "Normal User";

                pnlITStaffFilter.Visible = IsAdmin;

                CurrentPage = 1;
                PopulateFilterDropdowns();
                LoadStatistics();
                LoadAgreements();
            }
            else
            {
                // On PostBack: redirect if user has no email (deactivated / unknown user)
                if (string.IsNullOrEmpty(CurrentUserEmail)) { Response.Redirect("Default.aspx"); return; }

                int ps;
                if (int.TryParse(ddlPageSize.SelectedValue, out ps)) PageSize = ps;
            }
        }

        private void CheckUserRoleAndEmail(string winId)
        {
            string cs = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
            using (var conn = new SqlConnection(cs))
                try
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        "SELECT admin, email FROM hardware_users WHERE win_id=@w AND active=1", conn))
                    {
                        cmd.Parameters.AddWithValue("@w", winId);
                        using (var r = cmd.ExecuteReader())
                            if (r.Read())
                            {
                                IsAdmin = r["admin"] != DBNull.Value && Convert.ToBoolean(r["admin"]);
                                CurrentUserEmail = r["email"] != DBNull.Value ? r["email"].ToString() : "";
                                Session["IsAdmin"] = IsAdmin;
                            }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("CheckUserRoleAndEmail error: " + ex.Message);
                }
        }

        private void PopulateFilterDropdowns()
        {
            string cs = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
            using (var conn = new SqlConnection(cs))
                try
                {
                    conn.Open();

                    // Hardware types
                    string typeQ = IsAdmin
                        ? "SELECT DISTINCT type FROM hardware_model WHERE type IS NOT NULL ORDER BY type"
                        : @"SELECT DISTINCT hm.type FROM hardware_agreements ha
                        INNER JOIN hardware_model hm ON ha.model_id=hm.id
                        WHERE ha.employee_email=@e AND hm.type IS NOT NULL ORDER BY hm.type";
                    using (var cmd = new SqlCommand(typeQ, conn))
                    {
                        if (!IsAdmin) cmd.Parameters.AddWithValue("@e", CurrentUserEmail);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                ddlHardwareType.Items.Add(new ListItem(r[0].ToString(), r[0].ToString()));
                    }

                    // IT Staff (admin only)
                    if (IsAdmin)
                    {
                        using (var cmd = new SqlCommand(
                            "SELECT DISTINCT it_staff_win_id FROM hardware_agreements " +
                            "WHERE it_staff_win_id IS NOT NULL ORDER BY it_staff_win_id", conn))
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                ddlITStaff.Items.Add(new ListItem(r[0].ToString(), r[0].ToString()));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("PopulateFilterDropdowns error: " + ex.Message);
                }
        }

        private void LoadStatistics()
        {
            string cs = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
            using (var conn = new SqlConnection(cs))
                try
                {
                    conn.Open();
                    string ep = IsAdmin ? "" : " AND employee_email=@e";
                    string tw = IsAdmin ? "agreement_status!='Archived'" : "employee_email=@e AND agreement_status!='Archived'";

                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM hardware_agreements WHERE " + tw, conn))
                    {
                        if (!IsAdmin) cmd.Parameters.AddWithValue("@e", CurrentUserEmail);
                        litTotal.Text = cmd.ExecuteScalar().ToString();
                    }

                    var statuses = new[] { "Draft", "Pending", "Agreed", "Completed", "Archived" };
                    var lits = new Literal[] { litDrafts, litPending, litAgreed, litCompleted, litArchived };
                    for (int i = 0; i < statuses.Length; i++)
                    {
                        using (var cmd = new SqlCommand(
                            "SELECT COUNT(*) FROM hardware_agreements WHERE agreement_status=@s" + ep, conn))
                        {
                            cmd.Parameters.AddWithValue("@s", statuses[i]);
                            if (!IsAdmin) cmd.Parameters.AddWithValue("@e", CurrentUserEmail);
                            lits[i].Text = cmd.ExecuteScalar().ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("LoadStatistics error: " + ex.Message);
                }
        }

        // FIX (HIGH): Whitelist of allowed ORDER BY columns.
        // SortColumn is stored in ViewState and set via gvAgreements_Sorting — if it were
        // ever interpolated without this guard it would be a direct SQL injection vector.
        private static readonly System.Collections.Generic.HashSet<string> AllowedSortColumns =
            new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "a.agreement_number", "m.model", "a.agreement_status",
                "a.employee_name", "a.it_staff_win_id", "a.issue_date", "a.created_date"
            };

        private string SafeSortColumn
        {
            get { return AllowedSortColumns.Contains(SortColumn) ? SortColumn : "a.created_date"; }
        }

        private string SafeSortDir
        {
            get { return SortDir == "ASC" ? "ASC" : "DESC"; }
        }

        private void LoadAgreements()
        {
            int ps;
            if (int.TryParse(ddlPageSize.SelectedValue, out ps)) PageSize = ps;

            string cs = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
            using (var conn = new SqlConnection(cs))
                try
                {
                    conn.Open();
                    string baseQ = BuildQuery();

                    // Count
                    using (var cnt = new SqlCommand("SELECT COUNT(*) FROM (" + baseQ + ") AS C", conn))
                    {
                        AddFilterParameters(cnt);
                        TotalRecords = Convert.ToInt32(cnt.ExecuteScalar());
                        litTotalCount.Text = TotalRecords.ToString();
                    }

                    // Data — FIX (HIGH): use SafeSortColumn/SafeSortDir (whitelisted) not raw ViewState values
                    string fullQ = baseQ +
                        $" ORDER BY {SafeSortColumn} {SafeSortDir}" +
                        $" OFFSET {(CurrentPage - 1) * PageSize} ROWS FETCH NEXT {PageSize} ROWS ONLY";

                    using (var cmd = new SqlCommand(fullQ, conn))
                    {
                        AddFilterParameters(cmd);
                        var dt = new DataTable();
                        new SqlDataAdapter(cmd).Fill(dt);
                        gvAgreements.DataSource = dt;
                        gvAgreements.DataBind();
                        litShowingCount.Text = dt.Rows.Count.ToString();
                    }

                    SetupPagination();
                    UpdateSortInfo();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("LoadAgreements error: " + ex.Message);
                    // Surface the error so it doesn't silently leave stale data
                    litTotalCount.Text = "0";
                    litShowingCount.Text = "0";
                    gvAgreements.DataSource = null;
                    gvAgreements.DataBind();
                }
        }

        private string BuildQuery()
        {
            string q = @"
            SELECT a.id, a.agreement_number, m.model, m.type AS hardware_type,
                   a.serial_number, a.asset_number, a.agreement_status,
                   a.it_staff_win_id, a.issue_date, a.created_date,
                   a.employee_name, a.employee_email, a.token_expiry_date
            FROM   hardware_agreements a
            LEFT JOIN hardware_model m ON a.model_id = m.id
            WHERE  1=1";

            if (!IsAdmin) q += " AND a.employee_email = @userEmail";

            if (!string.IsNullOrEmpty(ddlStatusFilter.SelectedValue))
                q += " AND a.agreement_status = @status";
            else
                q += " AND a.agreement_status != 'Archived'";

            if (!string.IsNullOrEmpty(ddlHardwareType.SelectedValue))
                q += " AND m.type = @hwType";

            if (IsAdmin && !string.IsNullOrEmpty(ddlITStaff.SelectedValue))
                q += " AND a.it_staff_win_id = @itStaff";

            if (!string.IsNullOrEmpty(txtDateFrom.Text)) q += " AND a.issue_date >= @dateFrom";
            if (!string.IsNullOrEmpty(txtDateTo.Text)) q += " AND a.issue_date <= @dateTo";

            if (!string.IsNullOrEmpty(txtSearch.Text))
                q += @" AND (a.agreement_number LIKE @search
                          OR a.serial_number    LIKE @search
                          OR a.asset_number     LIKE @search
                          OR a.employee_name    LIKE @search
                          OR a.it_staff_win_id  LIKE @search
                          OR a.employee_email   LIKE @search
                          OR m.model            LIKE @search)";
            return q;
        }

        private void AddFilterParameters(SqlCommand cmd)
        {
            if (!IsAdmin)
                cmd.Parameters.AddWithValue("@userEmail", CurrentUserEmail);

            if (!string.IsNullOrEmpty(ddlStatusFilter.SelectedValue))
                cmd.Parameters.AddWithValue("@status", ddlStatusFilter.SelectedValue);

            if (!string.IsNullOrEmpty(ddlHardwareType.SelectedValue))
                cmd.Parameters.AddWithValue("@hwType", ddlHardwareType.SelectedValue);

            if (IsAdmin && !string.IsNullOrEmpty(ddlITStaff.SelectedValue))
                cmd.Parameters.AddWithValue("@itStaff", ddlITStaff.SelectedValue);

            DateTime d;
            if (!string.IsNullOrEmpty(txtDateFrom.Text) && DateTime.TryParse(txtDateFrom.Text, out d))
                cmd.Parameters.AddWithValue("@dateFrom", d);
            if (!string.IsNullOrEmpty(txtDateTo.Text) && DateTime.TryParse(txtDateTo.Text, out d))
                cmd.Parameters.AddWithValue("@dateTo", d.AddDays(1).AddTicks(-1)); // inclusive

            if (!string.IsNullOrEmpty(txtSearch.Text))
            {
                string safe = txtSearch.Text
                    .Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
                cmd.Parameters.AddWithValue("@search", "%" + safe + "%");
            }
        }

        // Returns the CSS class for the sort header link
        public string GetSortClass(string col)
        {
            return SortColumn == col ? "sort-active" : "";
        }

        // Returns a dual-arrow widget:
        //   unsorted  → both arrows grey (▲▼)
        //   ASC       → up arrow primary + bold, down arrow faint  (▲▼)
        //   DESC      → up arrow faint, down arrow primary + bold  (▲▼)
        public string GetSortIcon(string col)
        {
            if (SortColumn != col)
                return "<span class='sort-icon-wrap'>"
                     + "<i class='fas fa-sort-up'></i>"
                     + "<i class='fas fa-sort-down'></i>"
                     + "</span>";

            if (SortDir == "ASC")
                return "<span class='sort-icon-wrap'>"
                     + "<i class='fas fa-sort-up active-arrow' title='Sorted A→Z / Oldest first'></i>"
                     + "<i class='fas fa-sort-down inactive-arrow'></i>"
                     + "</span>";

            return "<span class='sort-icon-wrap'>"
                 + "<i class='fas fa-sort-up inactive-arrow'></i>"
                 + "<i class='fas fa-sort-down active-arrow' title='Sorted Z→A / Newest first'></i>"
                 + "</span>";
        }

        private void UpdateSortInfo()
        {
            string label;
            switch (SortColumn)
            {
                case "a.agreement_number": label = "Agreement #"; break;
                case "m.model": label = "Model"; break;
                case "a.agreement_status": label = "Status"; break;
                case "a.employee_name": label = "Employee"; break;
                case "a.it_staff_win_id": label = "IT Staff"; break;
                case "a.issue_date": label = "Issue Date"; break;
                default: label = "Date Created"; break;
            }
            string arrow = SortDir == "ASC" ? "▲ Ascending" : "▼ Descending";
            string arrowIcon = SortDir == "ASC" ? "fa-arrow-up" : "fa-arrow-down";
            litSortInfo.Text =
                $"<span style='font-size:.8rem;color:var(--text-secondary);display:inline-flex;align-items:center;gap:5px;'>" +
                $"<i class='fas {arrowIcon}' style='color:var(--primary);font-size:.72rem;'></i>" +
                $"<strong style='color:var(--primary);'>{label}</strong>" +
                $"<span style='color:var(--text-secondary);'>{arrow}</span>" +
                $"</span>";
        }

        // ── Column sort event ─────────────────────────────────────────────────
        protected void gvAgreements_Sorting(object sender, GridViewSortEventArgs e)
        {
            if (SortColumn == e.SortExpression)
                SortDir = SortDir == "ASC" ? "DESC" : "ASC";
            else { SortColumn = e.SortExpression; SortDir = "ASC"; }
            CurrentPage = 1;
            LoadAgreements();
        }

        // ── Filter events ─────────────────────────────────────────────────────
        protected void ddlStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        { CurrentPage = 1; LoadAgreements(); }

        protected void ddlHardwareType_SelectedIndexChanged(object sender, EventArgs e)
        { CurrentPage = 1; LoadAgreements(); }

        protected void ddlITStaff_SelectedIndexChanged(object sender, EventArgs e)
        { CurrentPage = 1; LoadAgreements(); }

        protected void txtDateFrom_TextChanged(object sender, EventArgs e)
        { CurrentPage = 1; LoadAgreements(); }

        protected void txtDateTo_TextChanged(object sender, EventArgs e)
        { CurrentPage = 1; LoadAgreements(); }

        protected void txtSearch_TextChanged(object sender, EventArgs e)
        { CurrentPage = 1; LoadAgreements(); }

        protected void ddlPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ps;
            if (int.TryParse(ddlPageSize.SelectedValue, out ps)) PageSize = ps;
            CurrentPage = 1; LoadAgreements();
        }

        protected void btnClearFilters_Click(object sender, EventArgs e)
        {
            ddlStatusFilter.SelectedIndex = 0;
            ddlHardwareType.SelectedIndex = 0;
            if (IsAdmin && ddlITStaff.Items.Count > 0) ddlITStaff.SelectedIndex = 0;
            txtDateFrom.Text = "";
            txtDateTo.Text = "";
            txtSearch.Text = "";
            ddlPageSize.SelectedIndex = 0;
            PageSize = 10;
            SortColumn = "a.created_date";
            SortDir = "DESC";
            CurrentPage = 1;
            LoadAgreements();
        }

        // kept for any remaining markup references
        protected void ddlSortBy_SelectedIndexChanged(object sender, EventArgs e)
        { LoadAgreements(); }

        // ── GridView events ───────────────────────────────────────────────────
        protected void gvAgreements_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            // CommandName="Sort" is handled exclusively by gvAgreements_Sorting.
            // ASP.NET fires BOTH Sorting and RowCommand for sort links — if we
            // toggled here too the direction would flip twice (net = no change).
            if (e.CommandName == "Sort") return;

            int id;
            if (!int.TryParse(e.CommandArgument.ToString(), out id)) return;

            if (e.CommandName == "EditAgreement")
                Response.Redirect(IsAdmin && IsAgreementDraft(id)
                    ? $"Agreement.aspx?id={id}"
                    : $"Agreement.aspx?id={id}&mode=view");
            else if (e.CommandName == "ViewAgreement")
                Response.Redirect(IsAdmin
                    ? $"Agreement.aspx?id={id}&mode=view"
                    : $"Agreement.aspx?id={id}&mode=empview");
        }

        private bool IsAgreementDraft(int id)
        {
            string cs = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
            using (var conn = new SqlConnection(cs))
                try
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        "SELECT agreement_status FROM hardware_agreements WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        object result = cmd.ExecuteScalar();
                        return result != null && result.ToString() == "Draft";
                    }
                }
                catch (Exception ex) { System.Diagnostics.Trace.WriteLine("IsAgreementDraft error: " + ex.Message); return false; }
        }

        protected void gvAgreements_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var row = (DataRowView)e.Row.DataItem;
            string st = row["agreement_status"].ToString();

            var btnEdit = (LinkButton)e.Row.FindControl("btnEdit");
            if (btnEdit != null) btnEdit.Visible = IsAdmin && st == "Draft";

            // For employees: View button opens agreement in a new tab (empview mode)
            // using window.open so the Acknowledgement Receipts list stays open
            if (!IsAdmin)
            {
                var btnView = (LinkButton)e.Row.FindControl("btnView");
                if (btnView != null)
                {
                    int id = Convert.ToInt32(row["id"]);
                    btnView.OnClientClick = $"window.open('Agreement.aspx?id={id}&mode=empview','_blank'); return false;";
                }
            }

            switch (st)
            {
                case "Draft": e.Row.CssClass = "draft-row"; break;
                case "Pending": e.Row.CssClass = "pending-row"; break;
                case "Agreed": e.Row.CssClass = "agreed-row"; break;
                case "Completed": e.Row.CssClass = "completed-row"; break;
                case "Archived": e.Row.CssClass = "archived-row"; break;
            }

            if (st == "Pending")
            {
                object exp = row.Row.Table.Columns.Contains("token_expiry_date")
                    ? row["token_expiry_date"] : DBNull.Value;
                if (exp != DBNull.Value && exp != null && Convert.ToDateTime(exp) < DateTime.Now)
                    foreach (System.Web.UI.WebControls.TableCell cell in e.Row.Cells)
                        if (cell.Text.Contains("Pending"))
                        {
                            cell.Text += " <span class='badge-expired' title='Employee link expired'>Link Expired</span>";
                            break;
                        }
            }
        }

        // ── Pagination ────────────────────────────────────────────────────────
        private void SetupPagination()
        {
            int total = (int)Math.Ceiling((double)TotalRecords / PageSize);
            if (total <= 1) { rptPagination.Visible = false; return; }

            var dt = new DataTable();
            dt.Columns.Add("PageNumber");
            for (int i = 1; i <= total; i++) dt.Rows.Add(i);
            rptPagination.DataSource = dt;
            rptPagination.DataBind();
            rptPagination.Visible = true;
        }

        protected void rptPagination_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "Page")
            {
                CurrentPage = Convert.ToInt32(e.CommandArgument);
                LoadAgreements();
                ClientScript.RegisterStartupScript(GetType(), "scroll",
                    "<script>document.querySelector('.table-container').scrollIntoView({behavior:'smooth'});</script>");
            }
        }

        public string GetPageCssClass(object dataItem)
        {
            var rv = dataItem as DataRowView;
            if (rv != null)
                return Convert.ToInt32(rv["PageNumber"]) == CurrentPage ? "page-link active" : "page-link";
            return "page-link";
        }
    }
}