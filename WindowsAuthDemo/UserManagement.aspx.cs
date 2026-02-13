using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;

namespace WindowsAuthDemo
{
    public partial class UserManagement : System.Web.UI.Page
    {
        private string connectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Get connection string from web.config
            connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            if (!IsPostBack)
            {
                string windowsUser = User.Identity.Name;
                lblUser.Text = windowsUser;
                lblUserSidebar.Text = windowsUser;

                // Check if user is admin
                bool isAdmin = CheckUserIsAdmin(windowsUser);

                if (isAdmin)
                {
                    // User is admin, show user management interface
                    pnlUserManagement.Visible = true;
                    pnlAccessDenied.Visible = false;
                    lblStatus.Text = "Administrator Access";
                    lblUserRoleSidebar.Text = "Administrator";

                    // Load users data
                    LoadUsers();

                    // Load audit logs
                    LoadAuditLogs();
                }
                else
                {
                    // User is not admin, show access denied
                    pnlUserManagement.Visible = false;
                    pnlAccessDenied.Visible = true;
                    lblStatus.Text = "Standard User Access";
                    lblUserRoleSidebar.Text = "Normal User";
                }
            }
        }

        private bool CheckUserIsAdmin(string windowsUser)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT admin FROM hardware_users WHERE win_id = @win_id ORDER BY email ASC";

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
                catch (Exception ex)
                {
                    LogError("CheckUserIsAdmin", windowsUser, ex.Message);
                    return false;
                }
            }
            return false;
        }

        private void LoadUsers()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT win_id, email, active, admin 
                                   FROM hardware_users 
                                   WHERE (@search IS NULL OR win_id LIKE '%' + @search + '%' OR email LIKE '%' + @search + '%')
                                   ORDER BY email ASC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@search",
                            string.IsNullOrEmpty(txtSearch.Text) ? (object)DBNull.Value : txtSearch.Text);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            gvUsers.DataSource = dt;
                            gvUsers.DataBind();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError("LoadUsers", User.Identity.Name, ex.Message);
                    ShowMessage("Error loading users: " + ex.Message, "error");
                }
            }
        }

        private void LoadAuditLogs()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT TOP 50 
                                   timestamp,
                                   admin_user,
                                   action_type,
                                   target_user,
                                   description
                                   FROM user_management_logs
                                   ORDER BY timestamp DESC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            gvAuditLogs.DataSource = dt;
                            gvAuditLogs.DataBind();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError("LoadAuditLogs", User.Identity.Name, ex.Message);
                    // Don't show error to user for audit logs loading
                }
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;

            string winId = txtWinId.Text.Trim();
            string email = txtEmail.Text.Trim();
            int active = Convert.ToInt32(ddlActive.SelectedValue);
            int admin = Convert.ToInt32(ddlAdmin.SelectedValue);
            string currentWinId = hdnUserId.Value;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    if (string.IsNullOrEmpty(currentWinId))
                    {
                        // Insert new user
                        string query = @"INSERT INTO hardware_users (win_id, email, active, admin) 
                                       VALUES (@win_id, @email, @active, @admin)";

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@win_id", winId);
                            command.Parameters.AddWithValue("@email", email);
                            command.Parameters.AddWithValue("@active", active);
                            command.Parameters.AddWithValue("@admin", admin);

                            int rowsAffected = command.ExecuteNonQuery();
                            if (rowsAffected > 0)
                            {
                                // Log the action
                                LogAction("CREATE", winId, null,
                                    $"{{'email':'{email}','active':'{active}','admin':'{admin}'}}",
                                    $"User '{winId}' was created with email '{email}', status '{(active == 1 ? "Active" : "Inactive")}', role '{(admin == 1 ? "Administrator" : "Normal User")}'");

                                ShowMessage("User added successfully!", "success");
                                ClearForm();
                                LoadUsers();
                                LoadAuditLogs();
                            }
                        }
                    }
                    else
                    {
                        // Get old values first for logging
                        DataRow oldValues = GetUserData(currentWinId);

                        // Update existing user
                        string query = @"UPDATE hardware_users 
                                       SET email = @email, active = @active, admin = @admin 
                                       WHERE win_id = @win_id";

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@win_id", currentWinId);
                            command.Parameters.AddWithValue("@email", email);
                            command.Parameters.AddWithValue("@active", active);
                            command.Parameters.AddWithValue("@admin", admin);

                            int rowsAffected = command.ExecuteNonQuery();
                            if (rowsAffected > 0)
                            {
                                // Log the action
                                string oldJson = oldValues != null ?
                                    $"{{'email':'{oldValues["email"]}','active':'{oldValues["active"]}','admin':'{oldValues["admin"]}'}}" : "{}";
                                string newJson = $"{{'email':'{email}','active':'{active}','admin':'{admin}'}}";

                                LogAction("UPDATE", currentWinId, oldJson, newJson,
                                    $"User '{currentWinId}' was updated. Changes: " +
                                    $"Email: '{oldValues?["email"]}' → '{email}', " +
                                    $"Status: '{(oldValues != null && Convert.ToInt32(oldValues["active"]) == 1 ? "Active" : "Inactive")}' → '{(active == 1 ? "Active" : "Inactive")}', " +
                                    $"Role: '{(oldValues != null && Convert.ToInt32(oldValues["admin"]) == 1 ? "Administrator" : "Normal User")}' → '{(admin == 1 ? "Administrator" : "Normal User")}'");

                                ShowMessage("User updated successfully!", "success");
                                ClearForm();
                                LoadUsers();
                                LoadAuditLogs();
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 2627) // Primary key violation
                    {
                        ShowMessage("Error: Windows ID already exists!", "error");
                    }
                    else
                    {
                        LogError("btnSave_Click", User.Identity.Name, ex.Message);
                        ShowMessage("Error saving user: " + ex.Message, "error");
                    }
                }
                catch (Exception ex)
                {
                    LogError("btnSave_Click", User.Identity.Name, ex.Message);
                    ShowMessage("Error saving user: " + ex.Message, "error");
                }
            }
        }

        private DataRow GetUserData(string winId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT win_id, email, active, admin FROM hardware_users WHERE win_id = @win_id ORDER BY email ASC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@win_id", winId);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError("GetUserData", User.Identity.Name, ex.Message);
                    return null;
                }
            }
        }

        protected void gvUsers_RowEditing(object sender, GridViewEditEventArgs e)
        {
            // Get the Windows ID from the selected row
            string winId = gvUsers.DataKeys[e.NewEditIndex].Value.ToString();

            // Load user data into form
            LoadUserData(winId);
            e.Cancel = true; // Cancel default edit mode
        }

        private void LoadUserData(string winId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT win_id, email, active, admin FROM hardware_users WHERE win_id = @win_id ORDER BY email ASC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@win_id", winId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                hdnUserId.Value = reader["win_id"].ToString();
                                txtWinId.Text = reader["win_id"].ToString();
                                txtEmail.Text = reader["email"].ToString();
                                ddlActive.SelectedValue = reader["active"].ToString();
                                ddlAdmin.SelectedValue = reader["admin"].ToString();

                                litFormTitle.Text = "Edit User";
                                btnCancel.Visible = true;
                                btnSave.Text = "Update User";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError("LoadUserData", User.Identity.Name, ex.Message);
                    ShowMessage("Error loading user data: " + ex.Message, "error");
                }
            }
        }

        protected void gvUsers_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            string winId = gvUsers.DataKeys[e.RowIndex].Value.ToString();

            // Prevent deleting current user
            if (winId == User.Identity.Name)
            {
                ShowMessage("You cannot delete your own account!", "error");
                return;
            }

            // Get user data before deletion for logging
            DataRow userData = GetUserData(winId);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "DELETE FROM hardware_users WHERE win_id = @win_id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@win_id", winId);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            // Log the action
                            if (userData != null)
                            {
                                string oldJson = $"{{'email':'{userData["email"]}','active':'{userData["active"]}','admin':'{userData["admin"]}'}}";
                                LogAction("DELETE", winId, oldJson, null,
                                    $"User '{winId}' (Email: {userData["email"]}, Status: {(Convert.ToInt32(userData["active"]) == 1 ? "Active" : "Inactive")}, Role: {(Convert.ToInt32(userData["admin"]) == 1 ? "Administrator" : "Normal User")}) was deleted");
                            }
                            else
                            {
                                LogAction("DELETE", winId, null, null, $"User '{winId}' was deleted");
                            }

                            ShowMessage("User deleted successfully!", "success");
                            LoadUsers();
                            LoadAuditLogs();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError("gvUsers_RowDeleting", User.Identity.Name, ex.Message);
                    ShowMessage("Error deleting user: " + ex.Message, "error");
                }
            }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            hdnUserId.Value = "";
            txtWinId.Text = "";
            txtEmail.Text = "";
            ddlActive.SelectedValue = "1";
            ddlAdmin.SelectedValue = "0";
            litFormTitle.Text = "Add New User";
            btnCancel.Visible = false;
            btnSave.Text = "Save User";
        }

        protected void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadUsers();
        }

        protected void gvUsers_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvUsers.PageIndex = e.NewPageIndex;
            LoadUsers();
        }

        protected void btnViewLogs_Click(object sender, EventArgs e)
        {
            LoadAuditLogs();
        }

        private void LogAction(string actionType, string targetUser, string oldValues, string newValues, string description)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"INSERT INTO user_management_logs 
                                   (admin_user, action_type, target_user, old_values, new_values, ip_address, user_agent, description)
                                   VALUES (@admin_user, @action_type, @target_user, @old_values, @new_values, @ip_address, @user_agent, @description)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@admin_user", User.Identity.Name);
                        command.Parameters.AddWithValue("@action_type", actionType);
                        command.Parameters.AddWithValue("@target_user", targetUser);
                        command.Parameters.AddWithValue("@old_values", string.IsNullOrEmpty(oldValues) ? (object)DBNull.Value : oldValues);
                        command.Parameters.AddWithValue("@new_values", string.IsNullOrEmpty(newValues) ? (object)DBNull.Value : newValues);
                        command.Parameters.AddWithValue("@ip_address", GetIPAddress());
                        command.Parameters.AddWithValue("@user_agent", GetUserAgent());
                        command.Parameters.AddWithValue("@description", description);

                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    // If logging fails, we still want the main operation to succeed
                    // So we just write to the event log or trace
                    System.Diagnostics.Trace.WriteLine($"LogAction failed: {ex.Message}");
                }
            }
        }

        private void LogError(string method, string user, string errorMessage)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"INSERT INTO user_management_logs 
                                   (admin_user, action_type, description, ip_address, user_agent)
                                   VALUES (@admin_user, 'ERROR', @description, @ip_address, @user_agent)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@admin_user", user);
                        command.Parameters.AddWithValue("@description", $"Error in {method}: {errorMessage}");
                        command.Parameters.AddWithValue("@ip_address", GetIPAddress());
                        command.Parameters.AddWithValue("@user_agent", GetUserAgent());

                        command.ExecuteNonQuery();
                    }
                }
                catch
                {
                    // If even error logging fails, write to trace
                    System.Diagnostics.Trace.WriteLine($"Error in {method} for user {user}: {errorMessage}");
                }
            }
        }

        private string GetIPAddress()
        {
            string ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = Request.ServerVariables["REMOTE_ADDR"];
            }

            return ipAddress ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return Request.UserAgent ?? "Unknown";
        }

        private void ShowMessage(string message, string type)
        {
            pnlMessage.Visible = true;
            litMessage.Text = message;

            // Set CSS class based on message type
            switch (type.ToLower())
            {
                case "success":
                    pnlMessage.CssClass = "alert alert-success";
                    break;
                case "error":
                    pnlMessage.CssClass = "alert alert-error";
                    break;
                case "warning":
                    pnlMessage.CssClass = "alert alert-warning";
                    break;
                default:
                    pnlMessage.CssClass = "alert";
                    break;
            }
        }
    }
}