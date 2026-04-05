using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WindowsAuthDemo
{
    public partial class UserManagement : System.Web.UI.Page
    {
        private string connectionString;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            ViewStateUserKey = User.Identity.Name;
        }

        // FIX (MED): Security headers were missing entirely on this page.
        // Every other page (ReportPage, ExistingAgreements) has these — adding here for consistency.
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
            connectionString = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            string winUser = User.Identity.Name;
            bool adminCheck = CheckUserIsAdmin(winUser);

            if (!adminCheck)
            {
                if (IsPostBack) Response.Redirect("ExistingAgreements.aspx");
                else
                {
                    pnlUserManagement.Visible = false;
                    pnlAccessDenied.Visible = true;
                    lblUser.Text = lblUserSidebar.Text = winUser;
                    lblStatus.Text = "Standard User Access";
                    lblUserRoleSidebar.Text = "Normal User";
                }
                return;
            }

            if (!IsPostBack)
            {
                lblUser.Text = lblUserSidebar.Text = winUser;
                lblStatus.Text = "Administrator Access";
                lblUserRoleSidebar.Text = "Administrator";
                pnlUserManagement.Visible = true;
                pnlAccessDenied.Visible = false;

                LoadStats();
                LoadUsers();
                LoadAuditLogs();
            }
        }

        private bool CheckUserIsAdmin(string w)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        "SELECT admin FROM hardware_users WHERE win_id=@w AND active=1", conn))
                    {
                        cmd.Parameters.AddWithValue("@w", w);
                        var r = cmd.ExecuteScalar();
                        return r != null && Convert.ToInt32(r) == 1;
                    }
                }
                catch { return false; }
            }
        }

        // ── Stats ──────────────────────────────────────────────────────────
        private void LoadStats()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"
                        SELECT
                            COUNT(*)                                          AS Total,
                            SUM(CASE WHEN active=1 THEN 1 ELSE 0 END)        AS Active,
                            SUM(CASE WHEN admin=1  THEN 1 ELSE 0 END)        AS Admins,
                            SUM(CASE WHEN active=0 THEN 1 ELSE 0 END)        AS Inactive
                        FROM hardware_users", conn))
                    {
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                litStatTotal.Text = r["Total"].ToString();
                                litStatActive.Text = r["Active"].ToString();
                                litStatAdmins.Text = r["Admins"].ToString();
                                litStatInactive.Text = r["Inactive"].ToString();
                            }
                        }
                    }
                }
                catch
                {
                    litStatTotal.Text = litStatActive.Text =
                    litStatAdmins.Text = litStatInactive.Text = "—";
                }
            }
        }

        // ── Load Users ─────────────────────────────────────────────────────
        private void LoadUsers()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string search = txtSearch.Text.Trim();
                    bool hasS = !string.IsNullOrEmpty(search) && search.Length >= 2;
                    // FIX (LOW): Escape LIKE special chars so searching for "50%" or "user_1"
                    // doesn't behave as wildcards. Consistent with ExistingAgreements.aspx.cs.
                    if (hasS)
                        search = search.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
                    string role = ddlRoleFilter.SelectedValue;
                    string status = ddlStatusFilter.SelectedValue;

                    string q = @"
                        SELECT u.win_id, u.email, u.active, u.admin,
                               ISNULL(u.receive_notification, 1) AS receive_notification,
                               COUNT(DISTINCT a.id) AS agreement_count
                        FROM hardware_users u
                        LEFT JOIN hardware_agreements a ON a.it_staff_win_id = u.win_id
                        WHERE (@s IS NULL
                               OR u.win_id LIKE '%'+@s+'%'
                               OR u.email  LIKE '%'+@s+'%')
                          AND (@role='' OR (CASE WHEN u.admin=1 THEN 'admin' ELSE 'user' END)=@role)
                          AND (@st=''   OR (CASE WHEN u.active=1 THEN 'active' ELSE 'inactive' END)=@st)
                        GROUP BY u.win_id, u.email, u.active, u.admin, u.receive_notification
                        ORDER BY u.admin DESC, u.active DESC, u.email ASC";

                    using (var cmd = new SqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@s", hasS ? (object)search : DBNull.Value);
                        cmd.Parameters.AddWithValue("@role", role ?? "");
                        cmd.Parameters.AddWithValue("@st", status ?? "");
                        var dt = new DataTable();
                        new SqlDataAdapter(cmd).Fill(dt);
                        gvUsers.DataSource = dt;
                        gvUsers.DataBind();
                        litUserCount.Text = dt.Rows.Count.ToString();
                    }
                }
                catch (Exception ex)
                {
                    LogError("LoadUsers", User.Identity.Name, ex.Message);
                    ShowMessage("Error loading users.", "error");
                }
            }
        }

        // ── Load Audit Logs ────────────────────────────────────────────────
        private void LoadAuditLogs()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string filter = ddlLogFilter?.SelectedValue ?? "";
                    string q = @"SELECT TOP 100
                                    timestamp, admin_user, action_type, target_user, description
                                 FROM user_management_logs
                                 WHERE (@f='' OR action_type=@f)
                                 ORDER BY timestamp DESC";
                    using (var cmd = new SqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@f", filter);
                        var dt = new DataTable();
                        new SqlDataAdapter(cmd).Fill(dt);
                        gvAuditLogs.DataSource = dt;
                        gvAuditLogs.DataBind();
                        litLogCount.Text = dt.Rows.Count.ToString();
                    }
                }
                catch { /* non-critical */ }
            }
        }

        // ── Save ───────────────────────────────────────────────────────────
        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            string winId = txtWinId.Text.Trim();
            string email = txtEmail.Text.Trim();
            int active = Convert.ToInt32(ddlActive.SelectedValue);
            int admin = Convert.ToInt32(ddlAdmin.SelectedValue);
            string cur = hdnUserId.Value;

            if (!string.IsNullOrEmpty(cur) && cur == User.Identity.Name)
            {
                if (active == 0) { ShowMessage("You cannot deactivate your own account.", "error"); return; }
                if (admin == 0) { ShowMessage("You cannot remove your own admin role.", "error"); return; }
            }

            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    if (string.IsNullOrEmpty(cur))
                    {
                        using (var cmd = new SqlCommand(
                            "INSERT INTO hardware_users(win_id,email,active,admin,receive_notification) VALUES(@w,@e,@a,@ad,@n)", conn))
                        {
                            cmd.Parameters.AddWithValue("@w", winId);
                            cmd.Parameters.AddWithValue("@e", email);
                            cmd.Parameters.AddWithValue("@a", active);
                            cmd.Parameters.AddWithValue("@ad", admin);
                            cmd.Parameters.AddWithValue("@n", int.Parse(ddlNotify.SelectedValue));
                            cmd.ExecuteNonQuery();
                        }
                        LogAction("CREATE", winId, null, Json(email, active, admin),
                            $"User '{winId}' created — {(active == 1 ? "Active" : "Inactive")}, {(admin == 1 ? "Admin" : "User")}");
                        ShowMessage($"User <strong>{HE(winId)}</strong> created successfully.", "success");
                    }
                    else
                    {
                        var old = GetUserData(cur);
                        using (var cmd = new SqlCommand(
                            "UPDATE hardware_users SET email=@e,active=@a,admin=@ad,receive_notification=@n WHERE win_id=@w", conn))
                        {
                            cmd.Parameters.AddWithValue("@w", cur);
                            cmd.Parameters.AddWithValue("@e", email);
                            cmd.Parameters.AddWithValue("@a", active);
                            cmd.Parameters.AddWithValue("@ad", admin);
                            cmd.Parameters.AddWithValue("@n", int.Parse(ddlNotify.SelectedValue));
                            cmd.ExecuteNonQuery();
                        }
                        LogAction("UPDATE", cur,
                            old != null ? Json(old["email"].ToString(), Conv(old["active"]), Conv(old["admin"])) : "{}",
                            Json(email, active, admin),
                            $"User '{cur}' updated");
                        ShowMessage($"User <strong>{HE(cur)}</strong> updated.", "success");
                    }
                    ClearForm(); LoadStats(); LoadUsers(); LoadAuditLogs();
                }
                catch (SqlException ex) when (ex.Number == 2627)
                {
                    ShowMessage($"Windows ID <strong>{HE(winId)}</strong> already exists.", "error");
                }
                catch (Exception ex)
                {
                    LogError("btnSave_Click", User.Identity.Name, ex.Message);
                    ShowMessage("Error saving user.", "error");
                }
            }
        }

        // ── Quick Toggle ───────────────────────────────────────────────────
        protected void gvUsers_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ToggleActive") return;
            string winId = e.CommandArgument.ToString();
            if (winId == User.Identity.Name) { ShowMessage("You cannot deactivate your own account.", "error"); return; }
            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    var old = GetUserData(winId);
                    int newState = (old != null && Conv(old["active"]) == 1) ? 0 : 1;
                    using (var cmd = new SqlCommand("UPDATE hardware_users SET active=@a WHERE win_id=@w", conn))
                    {
                        cmd.Parameters.AddWithValue("@a", newState);
                        cmd.Parameters.AddWithValue("@w", winId);
                        cmd.ExecuteNonQuery();
                    }
                    string verb = newState == 1 ? "activated" : "deactivated";
                    LogAction("UPDATE", winId,
                        old != null ? Json(old["email"].ToString(), Conv(old["active"]), Conv(old["admin"])) : "{}",
                        old != null ? Json(old["email"].ToString(), newState, Conv(old["admin"])) : "{}",
                        $"User '{winId}' {verb} via quick toggle");
                    ShowMessage($"User <strong>{HE(winId)}</strong> {verb}.", "success");
                    LoadStats(); LoadUsers(); LoadAuditLogs();
                }
                catch (Exception ex)
                {
                    LogError("ToggleActive", User.Identity.Name, ex.Message);
                    ShowMessage("Error toggling user status.", "error");
                }
            }
        }

        // ── Row Edit ───────────────────────────────────────────────────────
        protected void gvUsers_RowEditing(object sender, GridViewEditEventArgs e)
        {
            string winId = gvUsers.DataKeys[e.NewEditIndex].Value.ToString();
            LoadUserData(winId);
            e.Cancel = true;
            ScriptManager.RegisterStartupScript(this, GetType(), "scrollForm",
                "setTimeout(()=>document.getElementById('userFormCard')?.scrollIntoView({behavior:'smooth'}),100);", true);
        }

        private void LoadUserData(string winId)
        {
            var row = GetUserData(winId);
            if (row == null) { ShowMessage("User not found.", "error"); return; }
            hdnUserId.Value = row["win_id"].ToString();
            txtWinId.Text = row["win_id"].ToString();
            txtWinId.ReadOnly = true;
            txtEmail.Text = row["email"].ToString();
            ddlActive.SelectedValue = row["active"].ToString();
            ddlAdmin.SelectedValue = row["admin"].ToString();
            ddlNotify.SelectedValue = row["receive_notification"].ToString();
            litFormTitle.Text = "Edit User";
            litFormSubtitle.Text = "Editing: <strong>" + HE(winId) + "</strong>";
            btnCancel.Visible = true;
            btnSave.Text = "Update User";
        }

        // ── Delete ─────────────────────────────────────────────────────────
        protected void gvUsers_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            string winId = gvUsers.DataKeys[e.RowIndex].Value.ToString();
            if (winId == User.Identity.Name) { ShowMessage("You cannot delete your own account.", "error"); return; }
            var ud = GetUserData(winId);
            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("DELETE FROM hardware_users WHERE win_id=@w", conn))
                    {
                        cmd.Parameters.AddWithValue("@w", winId);
                        cmd.ExecuteNonQuery();
                    }
                    LogAction("DELETE", winId,
                        ud != null ? Json(ud["email"].ToString(), Conv(ud["active"]), Conv(ud["admin"])) : null,
                        null, $"User '{winId}' permanently deleted");
                    ShowMessage($"User <strong>{HE(winId)}</strong> deleted.", "success");
                    LoadStats(); LoadUsers(); LoadAuditLogs();
                }
                catch (Exception ex)
                {
                    LogError("gvUsers_RowDeleting", User.Identity.Name, ex.Message);
                    ShowMessage("Error deleting user.", "error");
                }
            }
        }

        // ── Export CSV ─────────────────────────────────────────────────────
        protected void btnExportCsv_Click(object sender, EventArgs e)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"
                        SELECT u.win_id AS [Windows ID], u.email AS [Email],
                               CASE WHEN u.active=1 THEN 'Active' ELSE 'Inactive' END AS [Status],
                               CASE WHEN u.admin=1  THEN 'Administrator' ELSE 'Normal User' END AS [Role],
                               CASE WHEN ISNULL(u.receive_notification,1)=1 THEN 'On' ELSE 'Off' END AS [Email Notify],
                               COUNT(DISTINCT a.id) AS [Acknowledgement Receipts Handled]
                        FROM hardware_users u
                        LEFT JOIN hardware_agreements a ON a.it_staff_win_id=u.win_id
                        GROUP BY u.win_id,u.email,u.active,u.admin,u.receive_notification
                        ORDER BY u.admin DESC, u.email ASC", conn))
                    {
                        var dt = new DataTable();
                        new SqlDataAdapter(cmd).Fill(dt);
                        var sb = new StringBuilder();
                        foreach (DataColumn c in dt.Columns) sb.Append($"\"{c.ColumnName}\",");
                        sb.Length--; sb.AppendLine();
                        foreach (DataRow row in dt.Rows)
                        {
                            foreach (DataColumn c in dt.Columns)
                                sb.Append($"\"{row[c].ToString().Replace("\"", "\"\"")}\",");
                            sb.Length--; sb.AppendLine();
                        }
                        Response.Clear();
                        Response.ContentType = "text/csv";
                        Response.AddHeader("Content-Disposition",
                            $"attachment; filename=Users_{DateTime.Now:yyyyMMdd_HHmm}.csv");
                        Response.Write(sb.ToString());
                        Response.End();
                    }
                }
                catch (Exception ex)
                {
                    LogError("ExportCsv", User.Identity.Name, ex.Message);
                    ShowMessage("Export failed.", "error");
                }
            }
        }

        // ── Filter / Paging events ─────────────────────────────────────────
        protected void txtSearch_TextChanged(object s, EventArgs e) => LoadUsers();
        protected void ddlRoleFilter_SelectedIndexChanged(object s, EventArgs e) => LoadUsers();
        protected void ddlStatusFilter_SelectedIndexChanged(object s, EventArgs e) => LoadUsers();
        protected void ddlLogFilter_SelectedIndexChanged(object s, EventArgs e) => LoadAuditLogs();
        protected void btnRefreshLogs_Click(object s, EventArgs e) => LoadAuditLogs();
        protected void btnCancel_Click(object s, EventArgs e) => ClearForm();
        protected void btnClear_Click(object s, EventArgs e) => ClearForm();

        protected void gvUsers_PageIndexChanging(object s, GridViewPageEventArgs e)
        { gvUsers.PageIndex = e.NewPageIndex; LoadUsers(); }

        protected void gvAuditLogs_PageIndexChanging(object s, GridViewPageEventArgs e)
        { gvAuditLogs.PageIndex = e.NewPageIndex; LoadAuditLogs(); }

        // ── Helpers ────────────────────────────────────────────────────────
        private void ClearForm()
        {
            hdnUserId.Value = txtWinId.Text = txtEmail.Text = "";
            txtWinId.ReadOnly = false;
            ddlActive.SelectedValue = "1";
            ddlAdmin.SelectedValue = "0";
            ddlNotify.SelectedValue = "1";
            litFormTitle.Text = "Add New User";
            litFormSubtitle.Text = "Fill in the details below to register a new portal user";
            btnCancel.Visible = false;
            btnSave.Text = "Save User";
        }

        private DataRow GetUserData(string winId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        "SELECT win_id,email,active,admin,ISNULL(receive_notification,1) AS receive_notification FROM hardware_users WHERE win_id=@w", conn))
                    {
                        cmd.Parameters.AddWithValue("@w", winId);
                        var dt = new DataTable();
                        new SqlDataAdapter(cmd).Fill(dt);
                        return dt.Rows.Count > 0 ? dt.Rows[0] : null;
                    }
                }
                catch { return null; }
            }
        }

        private static string Json(string email, int active, int admin)
            => $"{{'email':'{email}','active':'{active}','admin':'{admin}'}}";
        private static int Conv(object v) => v != null && v != DBNull.Value ? Convert.ToInt32(v) : 0;
        private static string HE(string s) => HttpUtility.HtmlEncode(s);

        private void LogAction(string type, string target, string oldV, string newV, string desc)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"INSERT INTO user_management_logs
                        (admin_user,action_type,target_user,old_values,new_values,ip_address,user_agent,description)
                        VALUES(@au,@at,@tu,@ov,@nv,@ip,@ua,@d)", conn))
                    {
                        cmd.Parameters.AddWithValue("@au", User.Identity.Name);
                        cmd.Parameters.AddWithValue("@at", type);
                        cmd.Parameters.AddWithValue("@tu", target);
                        cmd.Parameters.AddWithValue("@ov", string.IsNullOrEmpty(oldV) ? (object)DBNull.Value : oldV);
                        cmd.Parameters.AddWithValue("@nv", string.IsNullOrEmpty(newV) ? (object)DBNull.Value : newV);
                        cmd.Parameters.AddWithValue("@ip", GetIP());
                        cmd.Parameters.AddWithValue("@ua", GetUA());
                        cmd.Parameters.AddWithValue("@d", desc);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { }
            }
        }

        private void LogError(string method, string user, string msg)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"INSERT INTO user_management_logs
                        (admin_user,action_type,description,ip_address,user_agent)
                        VALUES(@au,'ERROR',@d,@ip,@ua)", conn))
                    {
                        cmd.Parameters.AddWithValue("@au", user);
                        cmd.Parameters.AddWithValue("@d", $"Error in {method}: {msg}");
                        cmd.Parameters.AddWithValue("@ip", GetIP());
                        cmd.Parameters.AddWithValue("@ua", GetUA());
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { }
            }
        }

        private string GetIP() => Request.ServerVariables["REMOTE_ADDR"] ?? "Unknown";
        private string GetUA() => Request.UserAgent ?? "Unknown";

        private void ShowMessage(string msg, string type)
        {
            pnlMessage.Visible = true;
            litMessage.Text = msg;
            pnlMessage.CssClass = type == "success" ? "alert alert-success"
                                : type == "error" ? "alert alert-error"
                                : type == "warning" ? "alert alert-warning"
                                : "alert";
        }
        // ─────────────────────────────────────────────────────────────────────
        // MARKUP HELPERS (called from GridView ItemTemplate)
        // ─────────────────────────────────────────────────────────────────────
        protected string GetInitials(string winId)
        {
            if (string.IsNullOrEmpty(winId)) return "?";
            // DOMAIN\username → get username part
            string name = winId.Contains("\\") ? winId.Split('\\')[1] : winId;
            return name.Length > 0 ? name[0].ToString().ToUpper() : "?";
        }

        protected string GetAvatarColor(string winId)
        {
            // Deterministic colour from username hash
            string[] colors = {
                "linear-gradient(135deg,#4361ee,#7209b7)",
                "linear-gradient(135deg,#10b981,#059669)",
                "linear-gradient(135deg,#f59e0b,#d97706)",
                "linear-gradient(135deg,#ef4444,#dc2626)",
                "linear-gradient(135deg,#8b5cf6,#6d28d9)",
                "linear-gradient(135deg,#06b6d4,#0891b2)",
                "linear-gradient(135deg,#ec4899,#db2777)"
            };
            int hash = 0;
            foreach (char c in (winId ?? "")) hash += c;
            return colors[Math.Abs(hash) % colors.Length];
        }

    }
}