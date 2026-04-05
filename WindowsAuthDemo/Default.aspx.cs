using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace WindowsAuthDemo
{
    public partial class Default : System.Web.UI.Page
    {
        protected Label lblUserRole;
        protected Label lblStatus;
        protected Label lblFirstAccess;
        protected Label lblError;
        protected System.Web.UI.HtmlControls.HtmlGenericControl normalUserContent;
        protected System.Web.UI.HtmlControls.HtmlGenericControl adminPanel;
        protected System.Web.UI.HtmlControls.HtmlGenericControl infoAuthType;


        // CSRF protection: bind ViewState MAC to the authenticated Windows user
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            ViewStateUserKey = User.Identity.Name;
        }

        // FIX (MED): Security headers were missing on Default.aspx entirely.
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

            // FIX (LOW): Previously called CheckUserIsAdmin() then CheckUserRoleAndRedirect()
            // which ran the same "SELECT admin FROM hardware_users WHERE win_id=@w" query TWICE
            // on every page load. Merged into a single DB round-trip below.
            string windowsUser = User.Identity.Name;
            bool isAdmin;
            bool userExists;
            GetUserRole(windowsUser, out isAdmin, out userExists);
            Session["IsAdmin"] = isAdmin;

            // Employees have no use for the dashboard — send them straight to their acknowledgement receipts
            if (!isAdmin && userExists)
            {
                Response.Redirect("ExistingAgreements.aspx");
                return;
            }

            if (!IsPostBack)
            {
                lblUser.Text = windowsUser;
                lblUserName.Text = windowsUser;
                lblUserSidebar.Text = windowsUser;

                // Find controls
                lblUserRole = (Label)FindControl("lblUserRole");
                lblStatus = (Label)FindControl("lblStatus");
                lblFirstAccess = (Label)FindControl("lblFirstAccess");
                lblError = (Label)FindControl("lblError");
                normalUserContent = (System.Web.UI.HtmlControls.HtmlGenericControl)FindControl("normalUserContent");
                adminPanel = (System.Web.UI.HtmlControls.HtmlGenericControl)FindControl("adminPanel");
                infoAuthType = (System.Web.UI.HtmlControls.HtmlGenericControl)FindControl("infoAuthType");

                // Find statistics controls
                lblTotalUsers = (Label)FindControl("lblTotalUsers");
                lblTotalAgreements = (Label)FindControl("lblTotalAgreements");
                lblTotalDevices = (Label)FindControl("lblTotalDevices");
                lblActiveAgreements = (Label)FindControl("lblActiveAgreements");

                // Load dashboard statistics
                LoadDashboardStatistics(isAdmin);

                // Render the correct UI panel based on the single DB lookup
                if (userExists)
                {
                    if (isAdmin)
                    {
                        DisplayAdminInterface();
                        if (lblUserRole != null) { lblUserRole.Text = "Administrator"; lblUserRole.CssClass = "role-admin"; }
                    }
                    else
                    {
                        DisplayNormalUserInterface();
                        if (lblUserRole != null) { lblUserRole.Text = "Normal User"; lblUserRole.CssClass = "role-normal"; }
                    }
                }
                else
                {
                    // First-time user: auto-register them as a normal user
                    AutoRegisterUser(windowsUser);
                    DisplayNormalUserInterface();
                    if (lblUserRole != null) { lblUserRole.Text = "Normal User (First Access)"; lblUserRole.CssClass = "role-new"; }
                    if (lblFirstAccess != null) lblFirstAccess.Visible = true;
                }
            }
        }

        // FIX (LOW+MED): Replaces two separate methods (CheckUserIsAdmin + CheckUserRoleAndRedirect)
        // with a single query that returns both the existence flag and the admin flag together.
        private void GetUserRole(string windowsUser, out bool isAdmin, out bool userExists)
        {
            isAdmin = false; userExists = false;
            string connectionString = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(
                        "SELECT admin FROM hardware_users WHERE win_id = @win_id AND active = 1", connection))
                    {
                        command.Parameters.AddWithValue("@win_id", windowsUser);
                        object result = command.ExecuteScalar();
                        if (result != null)
                        {
                            userExists = true;
                            isAdmin = Convert.ToInt32(result) == 1;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("GetUserRole error: " + ex.Message);
                    // Fail closed: isAdmin=false, userExists=false
                }
            }
        }

        private void AutoRegisterUser(string windowsUser)
        {
            string connectionString = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
            try
            {
                string emailDomain = System.Configuration.ConfigurationManager
                    .AppSettings["EmailDomain"] ?? "ioioleo.com";
                string email = windowsUser.Contains("\\")
                    ? windowsUser.Split('\\')[1] + "@" + emailDomain
                    : windowsUser + "@" + emailDomain;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "INSERT INTO hardware_users (win_id, active, admin, email) VALUES (@w, 1, 0, @e)", conn))
                    {
                        cmd.Parameters.AddWithValue("@w", windowsUser);
                        cmd.Parameters.AddWithValue("@e", email);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("AutoRegisterUser error: " + ex.Message);
                if (lblError != null) { lblError.Text = "Database connection error. Showing normal user interface."; lblError.Visible = true; }
            }
        }

        private void DisplayAdminInterface()
        {
            // Hide normal user elements
            if (normalUserContent != null) normalUserContent.Visible = false;

            // Show admin elements
            if (adminPanel != null)
            {
                adminPanel.Visible = true;
            }

            // Update UI indicators
            if (lblStatus != null)
            {
                lblStatus.Text = "Administrator Access";
                lblStatus.CssClass = "status-admin";
            }

            if (infoAuthType != null) infoAuthType.InnerText = "Windows Integrated (Admin)";
        }

        private void DisplayNormalUserInterface()
        {
            // Show normal user elements
            if (normalUserContent != null) normalUserContent.Visible = true;

            // Hide admin elements
            if (adminPanel != null) adminPanel.Visible = false;

            // Update UI indicators
            if (lblStatus != null)
            {
                lblStatus.Text = "Standard User Access";
                lblStatus.CssClass = "status-normal";
            }

            if (infoAuthType != null) infoAuthType.InnerText = "Windows Integrated";
        }

        // Class to hold dashboard statistics
        private class DashboardStats
        {
            public int TotalUsers { get; set; }
            public int TotalAgreements { get; set; }
            public int TotalDevices { get; set; }
            public int ActiveAgreements { get; set; }
        }

        // Method to load dashboard statistics
        private void LoadDashboardStatistics(bool isAdmin)
        {
            try
            {
                DashboardStats stats;

                // FIX (MED): Cache key was "DashboardStats" (global) so normal users and admins
                // shared the same cached object — normal users would see company-wide totals
                // instead of their own acknowledgement receipt counts. Now keyed per role.
                string cacheKey = isAdmin ? "DashboardStats_Admin" : "DashboardStats_User";

                if (Cache[cacheKey] != null)
                {
                    stats = (DashboardStats)Cache[cacheKey];
                }
                else
                {
                    stats = GetDashboardStatistics();
                    Cache.Insert(cacheKey, stats, null,
                        DateTime.Now.AddMinutes(5), TimeSpan.Zero);
                }

                // Bind statistics to labels
                if (lblTotalUsers != null) lblTotalUsers.Text = FormatNumber(stats.TotalUsers);
                if (lblTotalAgreements != null) lblTotalAgreements.Text = FormatNumber(stats.TotalAgreements);
                if (lblTotalDevices != null) lblTotalDevices.Text = FormatNumber(stats.TotalDevices);
                if (lblActiveAgreements != null) lblActiveAgreements.Text = FormatNumber(stats.ActiveAgreements);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("LoadDashboardStatistics error: " + ex.Message);
                if (lblTotalUsers != null) lblTotalUsers.Text = "0";
                if (lblTotalAgreements != null) lblTotalAgreements.Text = "0";
                if (lblTotalDevices != null) lblTotalDevices.Text = "0";
                if (lblActiveAgreements != null) lblActiveAgreements.Text = "0";
            }
        }

        // Method to fetch statistics from database
        private DashboardStats GetDashboardStatistics()
        {
            var stats = new DashboardStats();
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // 1. Total Users: number of unduplicated value of column 'win_id' in hardware_users
                    string usersQuery = "SELECT COUNT(DISTINCT win_id) FROM hardware_users";
                    using (SqlCommand command = new SqlCommand(usersQuery, connection))
                    {
                        object result = command.ExecuteScalar();
                        stats.TotalUsers = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                    }

                    // 2. Total Acknowledgement Receipts: number of unduplicated value of column 'agreement_number' in hardware_agreements
                    string agreementsQuery = "SELECT COUNT(DISTINCT agreement_number) FROM hardware_agreements";
                    using (SqlCommand command = new SqlCommand(agreementsQuery, connection))
                    {
                        object result = command.ExecuteScalar();
                        stats.TotalAgreements = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                    }

                    // 3. Total Devices: number of unduplicated value of column 'model' in hardware_model
                    string devicesQuery = "SELECT COUNT(DISTINCT model) FROM hardware_model";
                    using (SqlCommand command = new SqlCommand(devicesQuery, connection))
                    {
                        object result = command.ExecuteScalar();
                        stats.TotalDevices = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                    }

                    // 4. Active Acknowledgement Receipts: number of rows with 'agreement_status' = 'Completed' or 'Agreed'
                    string activeQuery = "SELECT COUNT(*) FROM hardware_agreements WHERE agreement_status IN ('Completed', 'Agreed')";
                    using (SqlCommand command = new SqlCommand(activeQuery, connection))
                    {
                        object result = command.ExecuteScalar();
                        stats.ActiveAgreements = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                    }
                }
                catch (Exception ex)
                {
                    // Log the error

                    // Return zero values if there's an error
                    stats.TotalUsers = 0;
                    stats.TotalAgreements = 0;
                    stats.TotalDevices = 0;
                    stats.ActiveAgreements = 0;
                }
            }

            return stats;
        }

        // Helper method to format numbers with commas
        private string FormatNumber(int number)
        {
            return number.ToString("N0");
        }
    }
}