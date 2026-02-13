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

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                string windowsUser = User.Identity.Name;
                lblUser.Text = windowsUser; // Changed for cleaner display
                lblUserName.Text = windowsUser; // Added for user info card
                lblUserSidebar.Text = windowsUser; // Added for sidebar

                // Check if user is admin and set session
                bool isAdmin = CheckUserIsAdmin(windowsUser);
                Session["IsAdmin"] = isAdmin;

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
                LoadDashboardStatistics();

                // Check user role and redirect accordingly
                CheckUserRoleAndRedirect(windowsUser);
            }
        }

        private bool CheckUserIsAdmin(string windowsUser)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT admin FROM hardware_users WHERE win_id = @win_id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@win_id", windowsUser);
                        object result = command.ExecuteScalar();

                        if (result != null && Convert.ToInt32(result) == 1)
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    // Log error
                }
            }
            return false;
        }

        private void CheckUserRoleAndRedirect(string windowsUser)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // First, check if user exists in database
                    string checkUserQuery = "SELECT admin FROM hardware_users WHERE win_id = @win_id";

                    using (SqlCommand command = new SqlCommand(checkUserQuery, connection))
                    {
                        command.Parameters.AddWithValue("@win_id", windowsUser);

                        object result = command.ExecuteScalar();

                        if (result != null)
                        {
                            // User exists, check if admin
                            int adminValue = Convert.ToInt32(result);

                            if (adminValue == 1)
                            {
                                // User is admin
                                DisplayAdminInterface();
                                if (lblUserRole != null) lblUserRole.Text = "Administrator";
                                if (lblUserRole != null) lblUserRole.CssClass = "role-admin";
                            }
                            else
                            {
                                // User is normal user
                                DisplayNormalUserInterface();
                                if (lblUserRole != null) lblUserRole.Text = "Normal User";
                                if (lblUserRole != null) lblUserRole.CssClass = "role-normal";
                            }
                        }
                        else
                        {
                            // User doesn't exist, insert as normal user
                            // Try to extract email from Windows ID (domain\username -> username@company.com)
                            string email = windowsUser.Contains("\\")
                                ? windowsUser.Split('\\')[1] + "@ioioleo.com"
                                : windowsUser + "@ioioleo.com";

                            string insertUserQuery = "INSERT INTO hardware_users (win_id, active, admin, email) VALUES (@win_id, 1, 0, @email)";

                            using (SqlCommand insertCommand = new SqlCommand(insertUserQuery, connection))
                            {
                                insertCommand.Parameters.AddWithValue("@win_id", windowsUser);
                                insertCommand.Parameters.AddWithValue("@email", email);
                                insertCommand.ExecuteNonQuery();
                            }

                            // Display normal user interface for first-time access
                            DisplayNormalUserInterface();
                            if (lblUserRole != null) lblUserRole.Text = "Normal User (First Access)";
                            if (lblUserRole != null) lblUserRole.CssClass = "role-new";
                            if (lblFirstAccess != null) lblFirstAccess.Visible = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error and display normal interface as fallback
                    System.Diagnostics.Trace.WriteLine("Database error: " + ex.Message);
                    DisplayNormalUserInterface();
                    if (lblUserRole != null) lblUserRole.Text = "Normal User (Error)";
                    if (lblUserRole != null) lblUserRole.CssClass = "role-error";
                    if (lblError != null)
                    {
                        lblError.Text = "Database connection error. Showing normal user interface.";
                        lblError.Visible = true;
                    }
                }
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
        private void LoadDashboardStatistics()
        {
            try
            {
                DashboardStats stats;

                // Check if stats are cached (cache for 5 minutes for better performance)
                if (Cache["DashboardStats"] != null)
                {
                    stats = (DashboardStats)Cache["DashboardStats"];
                }
                else
                {
                    stats = GetDashboardStatistics();
                    // Cache the stats for 5 minutes
                    Cache.Insert("DashboardStats", stats, null,
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
                // Log error but don't break the page
                System.Diagnostics.Trace.WriteLine("Error loading statistics: " + ex.Message);

                // Set default values
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

                    // 2. Total Agreements: number of unduplicated value of column 'agreement_number' in hardware_agreements
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

                    // 4. Active Agreements: number of rows with 'agreement_status' = 'Completed' or 'Agreed'
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
                    System.Diagnostics.Trace.WriteLine("Error getting dashboard statistics: " + ex.Message);

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