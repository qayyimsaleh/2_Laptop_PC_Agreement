using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace WindowsAuthDemo
{
    public partial class ExistingAgreements : System.Web.UI.Page
    {
        // Properties using ViewState for persistence
        protected int CurrentPage
        {
            get
            {
                object obj = ViewState["CurrentPage"];
                return (obj == null) ? 1 : (int)obj;
            }
            set { ViewState["CurrentPage"] = value; }
        }

        protected int PageSize
        {
            get
            {
                object obj = ViewState["PageSize"];
                return (obj == null) ? 10 : (int)obj;
            }
            set { ViewState["PageSize"] = value; }
        }

        protected int TotalRecords
        {
            get
            {
                object obj = ViewState["TotalRecords"];
                return (obj == null) ? 0 : (int)obj;
            }
            set { ViewState["TotalRecords"] = value; }
        }

        // NEW: Property to track if current user is admin
        protected bool IsAdmin
        {
            get
            {
                object obj = ViewState["IsAdmin"];
                return (obj == null) ? false : (bool)obj;
            }
            set { ViewState["IsAdmin"] = value; }
        }

        // NEW: Property to store current user's email
        protected string CurrentUserEmail
        {
            get
            {
                object obj = ViewState["CurrentUserEmail"];
                return (obj == null) ? "" : (string)obj;
            }
            set { ViewState["CurrentUserEmail"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {

                // Get current user info from database
                string currentWinId = User.Identity.Name;

                // Check user role and get email
                CheckUserRoleAndEmail(currentWinId);

                // If user is not found in the system at all, redirect
                if (string.IsNullOrEmpty(CurrentUserEmail))
                {
                    Response.Redirect("Default.aspx");
                    return;
                }

                // Populate sidebar user info
                lblUserName.Text = User.Identity.Name;

                // Set header user-profile info (Windows ID and Access Role)
                lblUser.Text = User.Identity.Name;
                lblStatus.Text = IsAdmin ? "Administrator" : "Normal User";
                lblUserRole.Text = IsAdmin ? "Administrator" : "Normal User";

                // Initialize page
                CurrentPage = 1;
                LoadStatistics();
                LoadAgreements();

                // NEW: Adjust UI based on user role
                SetupUIForUserRole();
            }
        }

        // NEW: Check user role and get email from database
        private void CheckUserRoleAndEmail(string winId)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT admin, email FROM hardware_users WHERE win_id = @winId AND active = 1";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@winId", winId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                IsAdmin = Convert.ToBoolean(reader["admin"]);
                                CurrentUserEmail = reader["email"].ToString();

                                // Also update Session for compatibility with other pages
                                Session["IsAdmin"] = IsAdmin;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error checking user role: " + ex.Message);
                }
            }
        }

        // NEW: Setup UI elements based on user role
        private void SetupUIForUserRole()
        {
            if (!IsAdmin)
            {
                // Hide certain admin-only elements if needed
                // For example, hide the "Create New" button for normal users
                // You can customize this based on your requirements

                // Update page title or header to indicate "My Agreements"
                // If you have a page title label, update it here
                // Example: lblPageTitle.Text = "My Agreements";
            }
        }

        private void LoadStatistics()
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Build WHERE clause based on user role
                    string userFilter = "";
                    if (!IsAdmin)
                    {
                        userFilter = " WHERE employee_email = @userEmail AND agreement_status != 'Archived'";
                    }
                    else
                    {
                        userFilter = " WHERE agreement_status != 'Archived'";
                    }

                    // Total agreements
                    string totalQuery = "SELECT COUNT(*) FROM hardware_agreements" + userFilter;
                    using (SqlCommand totalCmd = new SqlCommand(totalQuery, connection))
                    {
                        if (!IsAdmin)
                        {
                            totalCmd.Parameters.AddWithValue("@userEmail", CurrentUserEmail);
                        }
                        litTotal.Text = Convert.ToInt32(totalCmd.ExecuteScalar()).ToString();
                    }

                    // Drafts
                    string draftsQuery = "SELECT COUNT(*) FROM hardware_agreements WHERE agreement_status = 'Draft'" +
                                        (IsAdmin ? "" : " AND employee_email = @userEmail");
                    using (SqlCommand draftsCmd = new SqlCommand(draftsQuery, connection))
                    {
                        if (!IsAdmin)
                        {
                            draftsCmd.Parameters.AddWithValue("@userEmail", CurrentUserEmail);
                        }
                        litDrafts.Text = Convert.ToInt32(draftsCmd.ExecuteScalar()).ToString();
                    }

                    // Pending
                    string pendingQuery = "SELECT COUNT(*) FROM hardware_agreements WHERE agreement_status = 'Pending'" +
                                         (IsAdmin ? "" : " AND employee_email = @userEmail");
                    using (SqlCommand pendingCmd = new SqlCommand(pendingQuery, connection))
                    {
                        if (!IsAdmin)
                        {
                            pendingCmd.Parameters.AddWithValue("@userEmail", CurrentUserEmail);
                        }
                        litPending.Text = Convert.ToInt32(pendingCmd.ExecuteScalar()).ToString();
                    }

                    // Agreed
                    string agreedQuery = "SELECT COUNT(*) FROM hardware_agreements WHERE agreement_status = 'Agreed'" +
                                        (IsAdmin ? "" : " AND employee_email = @userEmail");
                    using (SqlCommand agreedCmd = new SqlCommand(agreedQuery, connection))
                    {
                        if (!IsAdmin)
                        {
                            agreedCmd.Parameters.AddWithValue("@userEmail", CurrentUserEmail);
                        }
                        litAgreed.Text = Convert.ToInt32(agreedCmd.ExecuteScalar()).ToString();
                    }

                    // Completed
                    string completedQuery = "SELECT COUNT(*) FROM hardware_agreements WHERE agreement_status = 'Completed'" +
                                        (IsAdmin ? "" : " AND employee_email = @userEmail");
                    using (SqlCommand completedCmd = new SqlCommand(completedQuery, connection))
                    {
                        if (!IsAdmin)
                        {
                            completedCmd.Parameters.AddWithValue("@userEmail", CurrentUserEmail);
                        }
                        litCompleted.Text = Convert.ToInt32(completedCmd.ExecuteScalar()).ToString();
                    }

                    // Archived
                    string archivedQuery = "SELECT COUNT(*) FROM hardware_agreements WHERE agreement_status = 'Archived'" +
                                        (IsAdmin ? "" : " AND employee_email = @userEmail");
                    using (SqlCommand archivedCmd = new SqlCommand(archivedQuery, connection))
                    {
                        if (!IsAdmin)
                        {
                            archivedCmd.Parameters.AddWithValue("@userEmail", CurrentUserEmail);
                        }
                        litArchived.Text = Convert.ToInt32(archivedCmd.ExecuteScalar()).ToString();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading statistics: " + ex.Message);
                }
            }
        }

        private void LoadAgreements()
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Build query with filters
                    string query = BuildQuery();

                    // Get total count for pagination
                    string countQuery = $"SELECT COUNT(*) FROM ({query}) AS CountQuery";
                    using (SqlCommand countCmd = new SqlCommand(countQuery, connection))
                    {
                        AddFilterParameters(countCmd);
                        TotalRecords = Convert.ToInt32(countCmd.ExecuteScalar());
                        litTotalCount.Text = TotalRecords.ToString();
                    }

                    // Add pagination
                    query += $" ORDER BY {GetSortField()} OFFSET {(CurrentPage - 1) * PageSize} ROWS FETCH NEXT {PageSize} ROWS ONLY";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        AddFilterParameters(command);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            gvAgreements.DataSource = dt;
                            gvAgreements.DataBind();

                            // Update showing count
                            litShowingCount.Text = dt.Rows.Count.ToString();
                        }
                    }

                    // Setup pagination
                    SetupPagination();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading agreements: " + ex.Message);
                }
            }
        }

        private string BuildQuery()
        {
            string query = @"
            SELECT a.id, a.agreement_number, m.model, a.serial_number, 
                   a.asset_number, a.agreement_status, a.it_staff_win_id, 
                   a.issue_date, a.created_date, a.employee_name, a.employee_email
            FROM hardware_agreements a
            LEFT JOIN hardware_model m ON a.model_id = m.id
            WHERE 1=1";

            // NEW: Add user filter for non-admin users
            if (!IsAdmin)
            {
                query += " AND a.employee_email = @userEmail";
            }

            // Add status filter
            if (!string.IsNullOrEmpty(ddlStatusFilter.SelectedValue))
            {
                query += " AND a.agreement_status = @status";
            }
            else
            {
                // Default: exclude Archived from "All Status" view
                query += " AND a.agreement_status != 'Archived'";
            }

            // Add search filter
            if (!string.IsNullOrEmpty(txtSearch.Text))
            {
                query += @" AND (a.agreement_number LIKE @search 
                          OR a.serial_number LIKE @search 
                          OR a.asset_number LIKE @search
                          OR a.employee_name LIKE @search
                          OR a.it_staff_win_id LIKE @search
                          OR a.employee_email LIKE @search
                          OR m.model LIKE @search)";
            }

            return query;
        }

        private void AddFilterParameters(SqlCommand command)
        {
            // NEW: Add user email parameter for non-admin users
            if (!IsAdmin)
            {
                command.Parameters.AddWithValue("@userEmail", CurrentUserEmail);
            }

            if (!string.IsNullOrEmpty(ddlStatusFilter.SelectedValue))
            {
                command.Parameters.AddWithValue("@status", ddlStatusFilter.SelectedValue);
            }

            if (!string.IsNullOrEmpty(txtSearch.Text))
            {
                command.Parameters.AddWithValue("@search", "%" + txtSearch.Text + "%");
            }
        }

        private string GetSortField()
        {
            switch (ddlSortBy.SelectedValue)
            {
                case "created_date ASC":
                    return "a.created_date ASC";
                case "agreement_number":
                    return "a.agreement_number";
                case "agreement_status":
                    return "a.agreement_status";
                default:
                    return "a.created_date DESC";
            }
        }

        private void SetupPagination()
        {
            int totalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

            if (totalPages <= 1)
            {
                rptPagination.Visible = false;
                return;
            }

            DataTable dtPages = new DataTable();
            dtPages.Columns.Add("PageNumber");

            for (int i = 1; i <= totalPages; i++)
            {
                dtPages.Rows.Add(i);
            }

            rptPagination.DataSource = dtPages;
            rptPagination.DataBind();
            rptPagination.Visible = true;
        }

        protected void gvAgreements_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditAgreement")
            {
                int agreementId = Convert.ToInt32(e.CommandArgument);

                // Only allow edit for admins and only for Draft status
                if (IsAdmin && IsAgreementDraft(agreementId))
                {
                    Response.Redirect($"Agreement.aspx?id={agreementId}");
                }
                else
                {
                    // Non-admin users or non-draft agreements go to view mode
                    Response.Redirect($"Agreement.aspx?id={agreementId}&mode=view");
                }
            }
            else if (e.CommandName == "ViewAgreement")
            {
                int agreementId = Convert.ToInt32(e.CommandArgument);
                Response.Redirect($"Agreement.aspx?id={agreementId}&mode=view");
            }
        }

        private bool IsAgreementDraft(int agreementId)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT agreement_status FROM hardware_agreements WHERE id = @id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", agreementId);
                        object result = command.ExecuteScalar();
                        return result?.ToString() == "Draft";
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        protected void gvAgreements_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                // Get the status from the data item
                DataRowView rowView = (DataRowView)e.Row.DataItem;
                string status = rowView["agreement_status"].ToString();

                // Find the action buttons
                LinkButton btnEdit = (LinkButton)e.Row.FindControl("btnEdit");
                LinkButton btnView = (LinkButton)e.Row.FindControl("btnView");

                // Only show Edit button for Draft status AND only for admins
                if (btnEdit != null)
                {
                    btnEdit.Visible = IsAdmin && (status == "Draft");
                }

                // Show View button for all users and statuses

                // Row colors based on status
                if (status == "Draft")
                {
                    e.Row.CssClass = "draft-row";
                }
                else if (status == "Pending")
                {
                    e.Row.CssClass = "pending-row";
                }
                else if (status == "Agreed")
                {
                    e.Row.CssClass = "agreed-row";
                }
                else if (status == "Completed")
                {
                    e.Row.CssClass = "completed-row";
                }
                else if (status == "Archived")
                {
                    e.Row.CssClass = "archived-row";
                }
            }
        }

        protected void ddlStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentPage = 1;
            LoadAgreements();
        }

        protected void ddlSortBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadAgreements();
        }

        protected void txtSearch_TextChanged(object sender, EventArgs e)
        {
            CurrentPage = 1;
            LoadAgreements();
        }

        protected void rptPagination_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "Page")
            {
                CurrentPage = Convert.ToInt32(e.CommandArgument);
                LoadAgreements();

                // Scroll to top of table
                string script = "<script type='text/javascript'>" +
                               "document.querySelector('.table-container').scrollIntoView({ behavior: 'smooth' });" +
                               "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ScrollToTop", script);
            }
        }

        public string GetPageCssClass(object dataItem)
        {
            if (dataItem is DataRowView rowView)
            {
                int pageNumber = Convert.ToInt32(rowView["PageNumber"]);
                return pageNumber == CurrentPage ? "page-link active" : "page-link";
            }
            return "page-link";
        }
    }
}