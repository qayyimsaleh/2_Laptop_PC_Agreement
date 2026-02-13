using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using System.Web.UI;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace WindowsAuthDemo
{
    public partial class Agreement : System.Web.UI.Page
    {
        private int? currentAgreementId = null;
        private bool isEditMode = false;
        private bool isViewMode = false;
        private bool isEmployeeMode = false;
        private string currentStatus = "";
        private bool accessoriesSectionVisible = false;
        private string accessToken = "";
        protected System.Web.UI.WebControls.Button btnExportPDF;
        protected System.Web.UI.WebControls.Button btnSaveUpdate;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Always get token from query string (persists on PostBack)
            accessToken = Request.QueryString["token"];

            // Check if employee mode based on token presence
            isEmployeeMode = !string.IsNullOrEmpty(accessToken);

            // On PostBack, restore state from ViewState AND hidden field
            // On PostBack, restore state from ViewState AND hidden field
            if (IsPostBack)
            {
                // First try hidden field (most reliable)
                if (!string.IsNullOrEmpty(hdnAgreementId.Value))
                {
                    int parsedId;
                    if (int.TryParse(hdnAgreementId.Value, out parsedId))
                    {
                        currentAgreementId = parsedId;
                    }
                }

                // Fallback to ViewState
                if (!currentAgreementId.HasValue && ViewState["CurrentAgreementId"] != null)
                {
                    currentAgreementId = (int?)ViewState["CurrentAgreementId"];
                }

                // Restore other state
                if (ViewState["CurrentStatus"] != null)
                {
                    currentStatus = (string)ViewState["CurrentStatus"];
                }
                if (ViewState["IsEmployeeMode"] != null)
                {
                    isEmployeeMode = (bool)ViewState["IsEmployeeMode"];
                }
                if (ViewState["IsViewMode"] != null)
                {
                    isViewMode = (bool)ViewState["IsViewMode"];
                }
                if (ViewState["IsEditMode"] != null)
                {
                    isEditMode = (bool)ViewState["IsEditMode"];
                }

                // ADD THIS LINE to restore token
                if (ViewState["AccessToken"] != null)
                {
                    accessToken = (string)ViewState["AccessToken"];
                }

                System.Diagnostics.Debug.WriteLine($"PostBack - CurrentAgreementId: {currentAgreementId}, hdnAgreementId: {hdnAgreementId.Value}, Token: {accessToken}");

                // ADD DEBUG INFO
                System.Diagnostics.Debug.WriteLine($"PostBack Debug - IsEmployeeMode: {isEmployeeMode}");
                System.Diagnostics.Debug.WriteLine($"PostBack Debug - txtEmpName exists: {txtEmpName != null}");

                return;
            }

            // Initial load (not PostBack)
            // Add this line to populate the sidebar user name
            lblUserName.Text = User.Identity.Name;
            lblTopUserName.Text = User.Identity.Name;
            lblTopUserRole.Text = "Administrator";

            // Debug: Log current user info
            System.Diagnostics.Debug.WriteLine($"=== Page_Load (Initial) ===");
            System.Diagnostics.Debug.WriteLine($"Current Windows ID: {User.Identity.Name}");
            System.Diagnostics.Debug.WriteLine($"Token from URL: {accessToken}");

            // Check if employee mode (via token)
            // Check if employee mode (via token)
            if (isEmployeeMode)
            {
                // Employee accessing via token
                System.Diagnostics.Debug.WriteLine($"=== EMPLOYEE MODE ===");
                System.Diagnostics.Debug.WriteLine($"Employee mode detected. Token: {accessToken}");
                System.Diagnostics.Debug.WriteLine($"Current Windows ID: {User.Identity.Name}");

                ValidateEmployeeAccess(accessToken);

                // IMPORTANT: Return here if validation failed
                if (!currentAgreementId.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"Validation failed. currentAgreementId is null.");

                    // Try one more time to get from query string if available
                    if (Request.QueryString["id"] != null)
                    {
                        int agreementId;
                        if (int.TryParse(Request.QueryString["id"], out agreementId))
                        {
                            currentAgreementId = agreementId;
                            System.Diagnostics.Debug.WriteLine($"Got Agreement ID from query string: {currentAgreementId}");
                        }
                    }

                    if (!currentAgreementId.HasValue)
                    {
                        ShowError("Unable to validate your access. Please use the link from your email.");
                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Validation successful. Agreement ID: {currentAgreementId.Value}");

                // CRITICAL: Store in multiple places for PostBack reliability
                // 1. ViewState
                ViewState["CurrentAgreementId"] = currentAgreementId;
                ViewState["CurrentStatus"] = currentStatus;
                ViewState["IsEmployeeMode"] = isEmployeeMode;
                ViewState["AccessToken"] = accessToken; // Store token too

                // 2. Hidden field (most reliable for PostBack)
                hdnAgreementId.Value = currentAgreementId.Value.ToString();

                // 3. Also register as client script variable for JavaScript
                Page.ClientScript.RegisterHiddenField("hdnCurrentAgreementId", currentAgreementId.Value.ToString());

                System.Diagnostics.Debug.WriteLine($"Stored Agreement ID: {currentAgreementId.Value}");
                System.Diagnostics.Debug.WriteLine($"hdnAgreementId.Value: {hdnAgreementId.Value}");
            }
            else
            {
                // Admin mode - check if user is admin
                if (Session["IsAdmin"] == null || !(bool)Session["IsAdmin"])
                {
                    Response.Redirect("Default.aspx");
                    return;
                }

                // Check mode from query string
                string mode = Request.QueryString["mode"];
                isViewMode = (mode == "view");

                // Check if editing/viewing existing agreement
                if (Request.QueryString["id"] != null)
                {
                    int agreementId;
                    if (int.TryParse(Request.QueryString["id"], out agreementId))
                    {
                        currentAgreementId = agreementId;

                        // Load the agreement to check its status
                        LoadAgreementStatus(agreementId);

                        // Set modes based on status and query string
                        if (isViewMode)
                        {
                            isEditMode = false;
                        }
                        else
                        {
                            // Only allow edit mode if status is Draft
                            isEditMode = (currentStatus == "Draft");

                            // If trying to edit a non-draft agreement, force view mode
                            if (!isEditMode)
                            {
                                isViewMode = true;
                                Response.Redirect($"Agreement.aspx?id={agreementId}&mode=view");
                                return;
                            }
                        }

                        // Store in ViewState for PostBack
                        ViewState["CurrentAgreementId"] = currentAgreementId;
                        ViewState["CurrentStatus"] = currentStatus;
                        ViewState["IsViewMode"] = isViewMode;
                        ViewState["IsEditMode"] = isEditMode;
                    }
                }
            }

            // Auto-fill IT Staff (current user) - only for admin mode
            if (!isEmployeeMode)
            {
                txtITStaff.Text = User.Identity.Name;
            }

            // Load hardware models from database
            LoadHardwareModels();

            // Load employee emails from database
            LoadEmployeeEmails();

            // If edit/view mode OR employee mode, load existing data
            if (currentAgreementId.HasValue)
            {
                LoadExistingAgreement(currentAgreementId.Value);
            }

            // Check accessories section visibility (only for admin)
            if (!isEmployeeMode)
            {
                CheckAndShowAccessoriesSection();
            }

            // Setup page based on mode
            SetupPageMode();

            // Hide messages initially
            messageSuccess.Visible = false;
            messageError.Visible = false;

            // Always set accessories section visibility (only for admin)
            if (accessoriesSection != null && !isEmployeeMode)
            {
                accessoriesSection.Visible = accessoriesSectionVisible;
            }
        }

        // Handle dropdown selection change
        protected void ddlModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Show/hide "Other" model panel based on selection
            pnlOtherModel.Visible = (ddlModel.SelectedValue == "OTHER");

            // Enable/disable validators for "Other" model
            rfvOtherModel.Enabled = (ddlModel.SelectedValue == "OTHER");
            rfvDeviceType.Enabled = (ddlModel.SelectedValue == "OTHER");

            // Check and update accessories section
            CheckAndShowAccessoriesSection();

            // Update accessories section visibility
            if (accessoriesSection != null)
            {
                accessoriesSection.Visible = accessoriesSectionVisible;
            }
        }

        // Handle device type change for "Other" option
        protected void ddlDeviceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Only update if "Other" is selected
            if (ddlModel.SelectedValue == "OTHER")
            {
                CheckAndShowAccessoriesSection();
                if (accessoriesSection != null)
                {
                    accessoriesSection.Visible = accessoriesSectionVisible;
                }
            }
        }

        private void CheckAndShowAccessoriesSection()
        {
            accessoriesSectionVisible = false;

            if (!string.IsNullOrEmpty(ddlModel.SelectedValue))
            {
                if (ddlModel.SelectedValue == "OTHER")
                {
                    // For "Other" option, check if device type is Laptop
                    if (ddlDeviceType.SelectedValue == "Laptop")
                    {
                        accessoriesSectionVisible = true;
                    }
                }
                else
                {
                    // For existing models, check database
                    string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();
                            string query = "SELECT type FROM hardware_model WHERE id = @id";
                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@id", SafeConvertToInt(ddlModel.SelectedValue, -1));
                                object result = command.ExecuteScalar();
                                if (result != null && result != DBNull.Value)
                                {
                                    string deviceType = result.ToString().ToLower();
                                    accessoriesSectionVisible = (deviceType == "laptop");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error checking device type: " + ex.Message);
                        }
                    }
                }
            }
        }

        private void LoadAgreementStatus(int agreementId)
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

                        // Use safe conversion
                        currentStatus = SafeConvertToString(result);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading agreement status: " + ex.Message);
                    currentStatus = "";
                }
            }
        }

        private void SetupPageMode()
        {
            if (isEmployeeMode)
            {
                // Employee Mode - Readonly for hardware details, editable for signature
                SetFormReadOnly(true); // Make hardware details readonly

                // CRITICAL: Explicitly enable employee fields
                txtEmpName.Enabled = true;
                txtEmpName.ReadOnly = false;
                txtEmpPosition.Enabled = true;
                txtEmpPosition.ReadOnly = false;
                txtEmpDepartment.Enabled = true;
                txtEmpDepartment.ReadOnly = false;
                txtEmpId.Enabled = true;
                txtEmpId.ReadOnly = true; // This should be readonly (auto-filled)
                txtEmpSignatureDate.Enabled = true;
                txtEmpSignatureDate.ReadOnly = true;

                // Remove any readonly CSS classes and aspNetDisabled
                txtEmpName.CssClass = "form-control";
                txtEmpPosition.CssClass = "form-control";
                txtEmpDepartment.CssClass = "form-control";
                txtEmpId.CssClass = "form-control auto-fill";
                txtEmpSignatureDate.CssClass = "form-control auto-fill";

                // Remove any server-side disabled state
                txtEmpName.Attributes.Remove("disabled");
                txtEmpPosition.Attributes.Remove("disabled");
                txtEmpDepartment.Attributes.Remove("disabled");

                litPageTitle.Text = "Employee Agreement - Laptop/PC Agreement System";
                litHeaderTitle.Text = "Laptop/PC Agreement - Employee Signature";
                litHeaderDescription.Text = "Please review and sign the laptop/pc agreement";
                litBreadcrumbTitle.Text = "Employee Agreement";

                // Hide admin action buttons, show employee button
                btnSaveDraft.Visible = false;
                btnSubmit.Visible = false;
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                btnSubmitEmployee.Visible = true;
                btnVerify.Visible = false;
                btnSaveUpdate.Visible = false;
                btnExportPDF.Visible = false;

                // Show agreement info
                agreementInfo.Visible = true;

                // Update status label
                lblCurrentStatus.Text = currentStatus;

                // CRITICAL: Show employee signature section
                pnlEmployeeSignature.Visible = true;

                // Hide IT verification section for employees
                pnlITVerification.Visible = false;
                phaseVerify.Visible = false;

                // Show Phase 2 for employee
                phaseAgree.Visible = true;

                // Hide accessories section for employees
                if (accessoriesSection != null)
                {
                    accessoriesSection.Visible = false;
                }

                // Load employee data if exists (for already partially filled forms)
                if (currentAgreementId.HasValue)
                {
                    LoadEmployeeData(currentAgreementId.Value);
                }

                // Enable employee signature section
                EnableEmployeeSignatureSection();

                // Auto-fill employee Windows ID - ONLY ON INITIAL LOAD
                if (!IsPostBack)
                {
                    string userName = User.Identity.Name;
                    if (userName.Contains("\\"))
                    {
                        userName = userName.Split('\\')[1];
                    }
                    txtEmpId.Text = userName;
                    txtEmpSignatureDate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                }
            }
            else if (isViewMode)
            {
                // View Mode - Readonly
                SetFormReadOnly(true);
                litPageTitle.Text = "View Agreement - Windows Auth Demo";
                litHeaderTitle.Text = "View Laptop/PC Agreement";
                litHeaderDescription.Text = "View agreement details (Read-only)";

                // Hide action buttons
                btnSaveDraft.Visible = false;
                btnSubmit.Visible = false;
                btnEdit.Visible = (currentStatus == "Draft"); // Only show Edit if it's a draft
                btnDelete.Visible = true; // Show delete button in view mode
                btnSubmitEmployee.Visible = false;
                btnVerify.Visible = false;
                btnSaveUpdate.Visible = false;

                // Show Export PDF button for non-draft statuses
                btnExportPDF.Visible = (currentStatus != "Draft");

                // For Pending status: allow IT admin to edit Phase 1 fields
                if (currentStatus == "Pending" && Session["IsAdmin"] != null && (bool)Session["IsAdmin"])
                {
                    SetFormReadOnly(false); // Unlock Phase 1 fields
                    btnSaveUpdate.Visible = true;
                    btnDelete.Visible = true;

                    litHeaderTitle.Text = "Edit Pending Agreement";
                    litHeaderDescription.Text = "You can edit hardware details and resend the notification email";
                }

                // Show IT Verification section for Agreed status (IT admin can verify)
                if (currentStatus == "Agreed")
                {
                    phaseVerify.Visible = true;
                    pnlITVerification.Visible = true;
                    btnVerify.Visible = true;
                    btnDelete.Visible = false;

                    litHeaderTitle.Text = "Verify Laptop/PC Agreement";
                    litHeaderDescription.Text = "Review employee agreement and complete IT verification";

                    // Auto-fill verified by
                    txtVerifiedBy.Text = User.Identity.Name;
                    txtVerifiedDate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                    // Load employee info into verification display
                    LoadVerificationData(currentAgreementId.Value);
                }
                else
                {
                    phaseVerify.Visible = false;
                    pnlITVerification.Visible = false;
                }

                // Show verification data in read-only for Completed status
                if (currentStatus == "Completed")
                {
                    phaseVerify.Visible = true;
                    pnlITVerification.Visible = true;
                    chkVerifyHardware.Enabled = false;
                    chkVerifySystemConfig.Enabled = false;
                    txtVerifyOthers.ReadOnly = true;
                    txtVerifyOthers.CssClass += " readonly-control";

                    // Load verification data
                    LoadVerificationData(currentAgreementId.Value);
                }

                // Show agreement info
                agreementInfo.Visible = true;

                // Update status label
                lblCurrentStatus.Text = currentStatus;

                litBreadcrumbTitle.Text = (currentStatus == "Agreed") ? "Verify Agreement" : "View Agreement";

                // Show employee agreement section as read-only for IT admin
                // when status is Pending, Agreed, or Completed
                if (currentStatus == "Pending" || currentStatus == "Agreed" || currentStatus == "Completed")
                {
                    phaseAgree.Visible = true;
                    pnlEmployeeSignature.Visible = true;

                    // Make all employee fields fully disabled for admin view
                    txtEmpName.ReadOnly = true;
                    txtEmpName.Enabled = false;
                    txtEmpName.CssClass = "form-control readonly-control";
                    txtEmpPosition.ReadOnly = true;
                    txtEmpPosition.Enabled = false;
                    txtEmpPosition.CssClass = "form-control readonly-control";
                    txtEmpDepartment.ReadOnly = true;
                    txtEmpDepartment.Enabled = false;
                    txtEmpDepartment.CssClass = "form-control readonly-control";
                    txtEmpId.ReadOnly = true;
                    txtEmpId.Enabled = false;
                    txtEmpId.CssClass = "form-control readonly-control";
                    txtEmpSignatureDate.ReadOnly = true;
                    txtEmpSignatureDate.Enabled = false;
                    txtEmpSignatureDate.CssClass = "form-control readonly-control";
                    chkAgreeTerms.Enabled = false;
                    btnSubmitEmployee.Visible = false;

                    // Load employee data if it exists (for Agreed/Completed)
                    if (currentAgreementId.HasValue)
                    {
                        LoadEmployeeData(currentAgreementId.Value);
                    }

                    // For Pending status, employee hasn't signed yet - show empty/placeholder
                    if (currentStatus == "Pending")
                    {
                        if (string.IsNullOrEmpty(txtEmpName.Text))
                            txtEmpName.Text = "(Pending employee signature)";
                        if (string.IsNullOrEmpty(txtEmpPosition.Text))
                            txtEmpPosition.Text = "(Pending employee signature)";
                        if (string.IsNullOrEmpty(txtEmpDepartment.Text))
                            txtEmpDepartment.Text = "(Pending employee signature)";
                        if (string.IsNullOrEmpty(txtEmpId.Text))
                            txtEmpId.Text = "(Pending employee signature)";
                        if (string.IsNullOrEmpty(txtEmpSignatureDate.Text))
                            txtEmpSignatureDate.Text = "(Pending employee signature)";
                    }

                    // For Agreed/Completed, show checkbox as checked (read-only)
                    if (currentStatus == "Agreed" || currentStatus == "Completed")
                    {
                        chkAgreeTerms.Checked = true;
                    }
                }
                else
                {
                    // Hide employee signature section for Draft view
                    phaseAgree.Visible = false;
                    pnlEmployeeSignature.Visible = false;
                }
            }
            else if (isEditMode)
            {
                // Edit Mode - but double check it's actually a draft
                if (currentStatus != "Draft")
                {
                    // This shouldn't happen due to redirect in Page_Load, but just in case
                    SetFormReadOnly(true);
                    litPageTitle.Text = "View Agreement - Windows Auth Demo";
                    litHeaderTitle.Text = "View Agreement (Read-only)";
                    litHeaderDescription.Text = "This agreement cannot be edited as it's not in draft status.";

                    // Hide edit buttons
                    btnSaveDraft.Visible = false;
                    btnSubmit.Visible = false;
                    btnEdit.Visible = false;
                    btnDelete.Visible = true;
                    btnSubmitEmployee.Visible = false;
                    btnVerify.Visible = false;
                    btnExportPDF.Visible = false;
                }
                else
                {
                    // Valid edit mode for draft
                    SetFormReadOnly(false);
                    litPageTitle.Text = "Edit Agreement - Windows Auth Demo";
                    litHeaderTitle.Text = "Edit Laptop/PC Agreement";
                    litHeaderDescription.Text = "Edit draft agreement details";

                    btnSaveDraft.Text = "Update Draft";
                    btnSubmit.Text = "Update and Submit";
                    btnSaveDraft.Visible = true;
                    btnSubmit.Visible = true;
                    btnEdit.Visible = false;
                    btnDelete.Visible = true;
                    btnSubmitEmployee.Visible = false;
                    btnVerify.Visible = false;
                    btnExportPDF.Visible = false;
                }

                // Show agreement info
                agreementInfo.Visible = true;

                // Update status label
                lblCurrentStatus.Text = currentStatus;

                litBreadcrumbTitle.Text = "Edit Agreement";

                // Hide employee signature section for admin edit
                pnlEmployeeSignature.Visible = false;
                phaseAgree.Visible = false;

                // Hide IT verification section for edit
                pnlITVerification.Visible = false;
                phaseVerify.Visible = false;
            }
            else
            {
                // Create Mode
                SetFormReadOnly(false);
                litPageTitle.Text = "Create New Agreement - Windows Auth Demo";
                litHeaderTitle.Text = "Create New Laptop/PC Agreement";
                litHeaderDescription.Text = "Fill in the details for the new laptop/pc agreement";

                btnSaveDraft.Text = "Save as Draft";
                btnSubmit.Text = "Submit Agreement";
                btnSaveDraft.Visible = true;
                btnSubmit.Visible = true;
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                btnSubmitEmployee.Visible = false;
                btnVerify.Visible = false;
                btnExportPDF.Visible = false;

                // Hide agreement info for new agreements
                agreementInfo.Visible = false;
                litBreadcrumbTitle.Text = "Create Agreement";

                // Hide employee signature section for admin create
                pnlEmployeeSignature.Visible = false;
                phaseAgree.Visible = false;

                // Hide IT verification section for create
                pnlITVerification.Visible = false;
                phaseVerify.Visible = false;
            }

            // Show status section only in edit mode for non-draft agreements
            statusSection.Visible = (isEditMode && currentStatus != "Draft");
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            // Additional security check on every postback
            if (currentAgreementId.HasValue && isEditMode)
            {
                string actualStatus = GetCurrentAgreementStatus();
                if (actualStatus != "Draft")
                {
                    // Force view mode if someone tampered with the form
                    isEditMode = false;
                    isViewMode = true;
                    SetFormReadOnly(true);
                    btnSaveDraft.Visible = false;
                    btnSubmit.Visible = false;
                    btnEdit.Visible = (actualStatus == "Draft");

                    ShowError("This agreement cannot be edited as it's not in draft status.");
                }
            }
        }

        private void SaveAgreement(string action)
        {
            // Validate model selection
            if (string.IsNullOrEmpty(ddlModel.SelectedValue))
            {
                ShowError("Please select a hardware model.");
                return;
            }

            // If "Other" is selected, validate the model name and type
            if (ddlModel.SelectedValue == "OTHER")
            {
                if (string.IsNullOrEmpty(txtOtherModel.Text.Trim()))
                {
                    ShowError("Please enter a model name for the 'Other' option.");
                    return;
                }

                if (string.IsNullOrEmpty(ddlDeviceType.SelectedValue))
                {
                    ShowError("Please select a device type for the new model.");
                    return;
                }
            }

            // Validate email fields for submission
            if (action == "Submitted")
            {
                if (string.IsNullOrEmpty(hdnEmployeeEmail.Value) ||
                    string.IsNullOrEmpty(hdnHODEmail.Value))
                {
                    ShowError("Both Employee Email and HOD Email are required for submission.");
                    return;
                }
            }

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Get or create model ID
                    int modelId = GetOrCreateModelId(connection);

                    // Check if we have an ID in query string and it's a draft
                    if (Request.QueryString["id"] != null)
                    {
                        int agreementId;
                        if (int.TryParse(Request.QueryString["id"], out agreementId))
                        {
                            // Check if it's actually a draft
                            string status = GetAgreementStatus(agreementId);
                            if (status == "Draft")
                            {
                                // UPDATE existing draft
                                UpdateAgreement(connection, action, agreementId, modelId);
                                return;
                            }
                        }
                    }

                    // If no ID or not a draft, CREATE new
                    CreateNewAgreement(connection, action, modelId);
                }
                catch (Exception ex)
                {
                    // DETAILED ERROR LOGGING
                    string errorDetails = $"Error in SaveAgreement:\n" +
                                         $"Message: {ex.Message}\n" +
                                         $"Stack Trace: {ex.StackTrace}\n" +
                                         $"Source: {ex.Source}\n" +
                                         $"Action: {action}";

                    System.Diagnostics.Debug.WriteLine(errorDetails);
                    ShowError($"Database error: {ex.Message}\n\nStack: {ex.StackTrace}");
                }
            }
        }

        // Get or create model ID
        // Get or create model ID
        private int GetOrCreateModelId(SqlConnection connection)
        {
            if (ddlModel.SelectedValue == "OTHER")
            {
                string newModelName = txtOtherModel.Text.Trim();
                string deviceType = ddlDeviceType.SelectedValue;

                if (string.IsNullOrEmpty(newModelName))
                {
                    throw new Exception("Please enter a model name for the 'Other' option.");
                }

                if (string.IsNullOrEmpty(deviceType))
                {
                    throw new Exception("Please select a device type for the new model.");
                }

                // Check if model already exists
                string checkQuery = "SELECT id FROM hardware_model WHERE model = @model";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@model", newModelName);
                    object existingId = checkCmd.ExecuteScalar();

                    // FIX: Check for DBNull
                    if (existingId != null && existingId != DBNull.Value)
                    {
                        return Convert.ToInt32(existingId);
                    }
                }

                // Insert new model with type
                string insertQuery = "INSERT INTO hardware_model (model, type, created_date) VALUES(@model, @type, GETDATE());SELECT SCOPE_IDENTITY();";

                using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
                {
                    insertCmd.Parameters.AddWithValue("@model", newModelName);
                    insertCmd.Parameters.AddWithValue("@type", deviceType);
                    object result = insertCmd.ExecuteScalar();

                    // FIX: Check for DBNull
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        throw new Exception("Failed to create new model. Please try again.");
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(ddlModel.SelectedValue))
                {
                    throw new Exception("Please select a valid model.");
                }
                return Convert.ToInt32(ddlModel.SelectedValue);
            }
        }

        private string GetAgreementStatus(int agreementId)
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

                        // Use safe conversion
                        return SafeConvertToString(result, "Draft"); // Default to Draft if not found
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error getting agreement status: " + ex.Message);
                    return "Draft"; // Default to Draft on error
                }
            }
        }

        private string GetCurrentAgreementStatus()
        {
            if (!currentAgreementId.HasValue) return "";

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT agreement_status FROM hardware_agreements WHERE id = @id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", currentAgreementId.Value);
                        object result = command.ExecuteScalar();

                        // Use safe conversion
                        return SafeConvertToString(result);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error getting current agreement status: " + ex.Message);
                    return "";
                }
            }
        }

        private void SetFormReadOnly(bool readOnly)
        {
            // Add/remove readonly class to form container
            if (readOnly)
            {
                formContainer.Attributes["class"] = formContainer.Attributes["class"] + " view-mode";
            }
            else
            {
                // Remove view-mode class
                formContainer.Attributes["class"] = formContainer.Attributes["class"].Replace("view-mode", "").Trim();
            }

            // Find controls if they're not accessible directly
            TextBox txtRemarksControl = (TextBox)FindControl("txtRemarks");
            TextBox txtSerialNumberControl = (TextBox)FindControl("txtSerialNumber");
            TextBox txtAssetNumberControl = (TextBox)FindControl("txtAssetNumber");
            TextBox txtOtherAccessoriesControl = (TextBox)FindControl("txtOtherAccessories");
            TextBox txtOtherModelControl = (TextBox)FindControl("txtOtherModel");

            if (txtRemarksControl != null) txtRemarksControl.ReadOnly = readOnly;
            if (txtSerialNumberControl != null) txtSerialNumberControl.ReadOnly = readOnly;
            if (txtAssetNumberControl != null) txtAssetNumberControl.ReadOnly = readOnly;
            if (txtOtherAccessoriesControl != null) txtOtherAccessoriesControl.ReadOnly = readOnly;
            if (txtOtherModelControl != null) txtOtherModelControl.ReadOnly = readOnly;

            // Enable/disable dropdowns and other controls - BUT NOT EMPLOYEE FIELDS!
            ddlModel.Enabled = !readOnly;
            ddlDeviceType.Enabled = !readOnly;
            txtEmployeeEmailSearch.ReadOnly = readOnly;
            txtHODEmailSearch.ReadOnly = readOnly;
            chkCarryBag.Enabled = !readOnly;
            chkPowerAdapter.Enabled = !readOnly;
            chkMouse.Enabled = !readOnly;
            rbWired.Enabled = !readOnly;
            rbWireless.Enabled = !readOnly;
            chkVGAConverter.Enabled = !readOnly;
            rbActive.Enabled = !readOnly;
            rbInactive.Enabled = !readOnly;

            // Add readonly CSS class
            if (readOnly)
            {
                if (txtSerialNumberControl != null) txtSerialNumberControl.CssClass += " readonly-control";
                if (txtAssetNumberControl != null) txtAssetNumberControl.CssClass += " readonly-control";
                if (txtOtherAccessoriesControl != null) txtOtherAccessoriesControl.CssClass += " readonly-control";
                if (txtRemarksControl != null) txtRemarksControl.CssClass += " readonly-control";
                if (txtOtherModelControl != null) txtOtherModelControl.CssClass += " readonly-control";
                ddlModel.CssClass += " readonly-control";
                ddlDeviceType.CssClass += " readonly-control";
                txtEmployeeEmailSearch.CssClass += " readonly-control";
                txtHODEmailSearch.CssClass += " readonly-control";
            }
            else
            {
                // Remove readonly classes
                if (txtSerialNumberControl != null) txtSerialNumberControl.CssClass = txtSerialNumberControl.CssClass.Replace("readonly-control", "").Trim();
                if (txtAssetNumberControl != null) txtAssetNumberControl.CssClass = txtAssetNumberControl.CssClass.Replace("readonly-control", "").Trim();
                if (txtOtherAccessoriesControl != null) txtOtherAccessoriesControl.CssClass = txtOtherAccessoriesControl.CssClass.Replace("readonly-control", "").Trim();
                if (txtRemarksControl != null) txtRemarksControl.CssClass = txtRemarksControl.CssClass.Replace("readonly-control", "").Trim();
                if (txtOtherModelControl != null) txtOtherModelControl.CssClass = txtOtherModelControl.CssClass.Replace("readonly-control", "").Trim();
                ddlModel.CssClass = ddlModel.CssClass.Replace("readonly-control", "").Trim();
                ddlDeviceType.CssClass = ddlDeviceType.CssClass.Replace("readonly-control", "").Trim();
                txtEmployeeEmailSearch.CssClass = txtEmployeeEmailSearch.CssClass.Replace("readonly-control", "").Trim();
                txtHODEmailSearch.CssClass = txtHODEmailSearch.CssClass.Replace("readonly-control", "").Trim();
            }
        }

        private void LoadExistingAgreement(int agreementId)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                SELECT a.*, m.model, m.type,
                       CONVERT(varchar, a.created_date, 103) as created_date_display,
                       CONVERT(varchar, a.last_updated, 103) as last_updated_display
                FROM hardware_agreements a
                LEFT JOIN hardware_model m ON a.model_id = m.id
                WHERE a.id = @id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", agreementId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Store current status using safe conversion
                                currentStatus = SafeConvertToString(reader["agreement_status"]);

                                // Display agreement info using safe conversions
                                agreementNumberDisplay.InnerText = SafeConvertToString(reader["agreement_number"]);
                                createdDateDisplay.InnerText = SafeConvertToString(reader["created_date_display"]);
                                updatedDateDisplay.InnerText = SafeConvertToString(reader["last_updated_display"]);

                                // Add status badge
                                string statusBadgeClass = "status-" + currentStatus.ToLower();
                                litStatusBadge.Text = $"<span class='status-badge {statusBadgeClass}'>{currentStatus}</span>";

                                // Set model - find by value using safe conversion
                                string modelId = SafeConvertToString(reader["model_id"]);
                                if (!string.IsNullOrEmpty(modelId))
                                {
                                    ListItem item = ddlModel.Items.FindByValue(modelId);
                                    if (item != null)
                                    {
                                        ddlModel.SelectedValue = modelId;
                                    }
                                }

                                txtSerialNumber.Text = SafeConvertToString(reader["serial_number"]);
                                txtAssetNumber.Text = SafeConvertToString(reader["asset_number"]);

                                // Load email fields - set hidden field values and text box display
                                string employeeEmail = SafeConvertToString(reader["employee_email"]);
                                string hodEmail = SafeConvertToString(reader["hod_email"]);

                                if (!string.IsNullOrEmpty(employeeEmail))
                                {
                                    hdnEmployeeEmail.Value = employeeEmail;
                                    txtEmployeeEmailSearch.Text = employeeEmail;
                                }

                                if (!string.IsNullOrEmpty(hodEmail))
                                {
                                    hdnHODEmail.Value = hodEmail;
                                    txtHODEmailSearch.Text = hodEmail;
                                }

                                // Load accessories using safe conversions
                                bool hasCarryBag = SafeConvertToBool(reader["has_carry_bag"]);
                                bool hasPowerAdapter = SafeConvertToBool(reader["has_power_adapter"]);
                                bool hasMouse = SafeConvertToBool(reader["has_mouse"]);
                                bool hasVGAConverter = SafeConvertToBool(reader["has_vga_converter"]);

                                chkCarryBag.Checked = hasCarryBag;
                                chkPowerAdapter.Checked = hasPowerAdapter;
                                chkMouse.Checked = hasMouse;
                                chkVGAConverter.Checked = hasVGAConverter;

                                string mouseType = SafeConvertToString(reader["mouse_type"]);
                                rbWired.Checked = (mouseType == "Wired");
                                rbWireless.Checked = (mouseType == "Wireless");

                                // Handle other_accessories with safe conversion
                                txtOtherAccessories.Text = SafeConvertToString(reader["other_accessories"]);

                                // Load IT details
                                txtITStaff.Text = SafeConvertToString(reader["it_staff_win_id"]);

                                // Handle issue_date with safe conversion
                                DateTime? issueDate = SafeConvertToDateTime(reader["issue_date"]);
                                if (issueDate.HasValue)
                                {
                                    txtDateIssue.Text = issueDate.Value.ToString("dd/MM/yyyy");
                                }
                                else
                                {
                                    txtDateIssue.Text = DateTime.Now.ToString("dd/MM/yyyy");
                                }

                                // Load remarks - handle DBNull
                                TextBox txtRemarksControl = (TextBox)FindControl("txtRemarks");
                                if (txtRemarksControl != null)
                                {
                                    txtRemarksControl.Text = SafeConvertToString(reader["remarks"]);
                                }

                                string status = SafeConvertToString(reader["agreement_status"]);
                                if (status == "Active" || status == "Pending" || status == "Inactive")
                                {
                                    bool isActive = (status == "Active" || status == "Pending");
                                    rbActive.Checked = isActive;
                                    rbInactive.Checked = !isActive;
                                }

                                // After loading data, check accessories section
                                CheckAndShowAccessoriesSection();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Error loading agreement: " + ex.Message);
                }
            }
        }

        private void LoadHardwareModels()
        {
            // Clear existing items first
            ddlModel.Items.Clear();

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT id, model, type FROM hardware_model ORDER BY model";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            ddlModel.Items.Add(new ListItem("-- Select Model --", ""));

                            while (reader.Read())
                            {
                                string modelName = reader["model"].ToString();
                                string modelType = reader["type"].ToString();
                                string displayText = $"{modelName} ({modelType})";

                                ddlModel.Items.Add(new ListItem(
                                    displayText,
                                    reader["id"].ToString()
                                ));
                            }

                            // Add "Other" option at the end
                            ddlModel.Items.Add(new ListItem("-- Other (Add New) --", "OTHER"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Error loading hardware models: " + ex.Message);
                    ddlModel.Items.Add(new ListItem("-- Error loading models --", ""));
                }
            }
        }

        private void LoadEmployeeEmails()
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT email, win_id FROM hardware_users WHERE active = 1 AND email IS NOT NULL ORDER BY email ASC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Build JSON array for client-side searchable dropdowns
                            var emailList = new System.Collections.Generic.List<string>();

                            while (reader.Read())
                            {
                                string email = reader["email"].ToString();
                                string winId = reader["win_id"].ToString();
                                string displayText = $"{email} ({winId})";

                                // Escape for JSON
                                string escapedText = displayText.Replace("\\", "\\\\").Replace("\"", "\\\"");
                                string escapedValue = email.Replace("\\", "\\\\").Replace("\"", "\\\"");

                                emailList.Add($"{{\"text\":\"{escapedText}\",\"value\":\"{escapedValue}\"}}");
                            }

                            string jsonArray = "[" + string.Join(",", emailList) + "]";

                            // Populate both hidden fields with the same list
                            hdnEmployeeEmailList.Value = jsonArray;
                            hdnHODEmailList.Value = jsonArray;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Error loading employee emails: " + ex.Message);
                    hdnEmployeeEmailList.Value = "[]";
                    hdnHODEmailList.Value = "[]";
                }
            }
        }

        // Create new agreement with modelId parameter
        // Create new agreement with modelId parameter
        private void CreateNewAgreement(SqlConnection connection, string action, int modelId)
        {
            // Generate agreement number
            string agreementNumber = GenerateAgreementNumber();

            // Generate access token for employee link
            string accessToken = null;
            DateTime? tokenExpiryDate = null;

            if (action == "Submitted")
            {
                accessToken = Guid.NewGuid().ToString("N").Substring(0, 32);
                tokenExpiryDate = DateTime.Now.AddDays(7);
            }

            // Determine status and dates
            string finalStatus;
            DateTime? submittedDate = null;
            DateTime issueDate = DateTime.Now;

            if (action == "Draft")
            {
                finalStatus = "Draft";
            }
            else // Submitted
            {
                submittedDate = DateTime.Now;
                issueDate = submittedDate.Value;
                finalStatus = "Pending"; // Always Pending first on submit
            }

            if (!string.IsNullOrEmpty(accessToken) && action == "Submitted")
            {
                tokenExpiryDate = DateTime.Now.AddDays(7);
            }

            string query = @"
        INSERT INTO hardware_agreements 
        (agreement_number, model_id, serial_number, asset_number, 
         has_carry_bag, has_power_adapter, has_mouse, mouse_type, 
         has_vga_converter, other_accessories, it_staff_win_id, 
         issue_date, remarks, agreement_status, submitted_date, created_date,
         employee_email, hod_email, agreement_view_token, token_expiry_date)
        VALUES 
        (@agreementNumber, @modelId, @serialNumber, @assetNumber,
         @hasCarryBag, @hasPowerAdapter, @hasMouse, @mouseType,
         @hasVGAConverter, @otherAccessories, @itStaff,
         @issueDate, @remarks, @status, @submittedDate, GETDATE(),
         @employeeEmail, @hodEmail, @agreementViewToken, @tokenExpiryDate)";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                // Use the provided modelId parameter
                command.Parameters.AddWithValue("@agreementNumber", agreementNumber);
                command.Parameters.AddWithValue("@modelId", modelId);
                command.Parameters.AddWithValue("@serialNumber", txtSerialNumber.Text.Trim());
                command.Parameters.AddWithValue("@assetNumber", txtAssetNumber.Text.Trim());

                // Email fields
                command.Parameters.AddWithValue("@employeeEmail", string.IsNullOrEmpty(hdnEmployeeEmail.Value) ? DBNull.Value : (object)hdnEmployeeEmail.Value);
                command.Parameters.AddWithValue("@hodEmail", string.IsNullOrEmpty(hdnHODEmail.Value) ? DBNull.Value : (object)hdnHODEmail.Value);

                // Accessories - set to 0 instead of NULL
                command.Parameters.AddWithValue("@hasCarryBag", chkCarryBag.Checked ? 1 : 0);
                command.Parameters.AddWithValue("@hasPowerAdapter", chkPowerAdapter.Checked ? 1 : 0);
                command.Parameters.AddWithValue("@hasMouse", chkMouse.Checked ? 1 : 0);

                string mouseType = "";
                if (chkMouse.Checked)
                {
                    mouseType = rbWired.Checked ? "Wired" :
                               rbWireless.Checked ? "Wireless" : "";
                }
                // Send empty string instead of DBNull.Value
                command.Parameters.AddWithValue("@mouseType",
                    string.IsNullOrEmpty(mouseType) ? (object)"" : mouseType);

                command.Parameters.AddWithValue("@hasVGAConverter", chkVGAConverter.Checked ? 1 : 0);

                // Find txtOtherAccessories control
                TextBox txtOtherAccessoriesControl = (TextBox)FindControl("txtOtherAccessories");
                command.Parameters.AddWithValue("@otherAccessories",
                    (txtOtherAccessoriesControl != null && !string.IsNullOrEmpty(txtOtherAccessoriesControl.Text)) ?
                    (object)txtOtherAccessoriesControl.Text.Trim() : "");

                // IT Details
                command.Parameters.AddWithValue("@itStaff", txtITStaff.Text);
                command.Parameters.AddWithValue("@issueDate", issueDate);

                // Remarks - find control
                TextBox txtRemarksControl = (TextBox)FindControl("txtRemarks");
                command.Parameters.AddWithValue("@remarks",
                    (txtRemarksControl != null && !string.IsNullOrEmpty(txtRemarksControl.Text)) ?
                    (object)txtRemarksControl.Text.Trim() : "");

                // Status and dates
                command.Parameters.AddWithValue("@status", finalStatus);
                command.Parameters.AddWithValue("@submittedDate",
                    submittedDate.HasValue ? (object)submittedDate.Value : DBNull.Value);

                // Token fields
                command.Parameters.AddWithValue("@agreementViewToken",
            string.IsNullOrEmpty(accessToken) ? DBNull.Value : (object)accessToken);
                command.Parameters.AddWithValue("@tokenExpiryDate",
            tokenExpiryDate.HasValue ? (object)tokenExpiryDate.Value : DBNull.Value);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    // Get the inserted ID
                    string getLastIdQuery = "SELECT SCOPE_IDENTITY()";
                    using (SqlCommand getIdCmd = new SqlCommand(getLastIdQuery, connection))
                    {
                        object result = getIdCmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            currentAgreementId = Convert.ToInt32(result);
                        }
                        else
                        {
                            // Fallback: get by agreement number
                            string getByAgreementQuery = "SELECT id FROM hardware_agreements WHERE agreement_number = @agreementNumber";
                            using (SqlCommand getByAgreementCmd = new SqlCommand(getByAgreementQuery, connection))
                            {
                                getByAgreementCmd.Parameters.AddWithValue("@agreementNumber", agreementNumber);
                                object idResult = getByAgreementCmd.ExecuteScalar();
                                if (idResult != null && idResult != DBNull.Value)
                                {
                                    currentAgreementId = Convert.ToInt32(idResult);
                                }
                            }
                        }
                    }

                    if (currentAgreementId.HasValue && currentAgreementId.Value > 0)
                    {
                        // Send email notification
                        if (action == "Submitted")
                        {
                            bool emailSent = SendAgreementEmail(action, agreementNumber, finalStatus);
                            if (emailSent)
                            {
                                ShowSuccess($"Agreement submitted successfully! Agreement Number: {agreementNumber}. Email sent to {hdnEmployeeEmail.Value} and {hdnHODEmail.Value}");
                            }
                            else
                            {
                                ShowSuccess($"Agreement submitted successfully! Agreement Number: {agreementNumber}. Status: {finalStatus}. Note: Email notification failed.");
                            }
                        }
                        else
                        {
                            ShowSuccess($"Draft saved successfully! Agreement Number: {agreementNumber}");
                        }

                        // Redirect based on action
                        string redirectPage = (action == "Draft") ? "Default.aspx" : "ExistingAgreements.aspx";
                        string script = "<script type='text/javascript'>" +
                                        "setTimeout(function(){ window.location.href = '" + redirectPage + "'; }, 2000);" +
                                        "</script>";
                        ClientScript.RegisterStartupScript(this.GetType(), "redirect", script);
                    }
                    else
                    {
                        ShowSuccess($"Agreement saved successfully! Agreement Number: {agreementNumber}");

                        // Still redirect
                        string redirectPage = (action == "Draft") ? "Default.aspx" : "ExistingAgreements.aspx";
                        string script = "<script type='text/javascript'>" +
                                        "setTimeout(function(){ window.location.href = '" + redirectPage + "'; }, 2000);" +
                                        "</script>";
                        ClientScript.RegisterStartupScript(this.GetType(), "redirect", script);
                    }
                }
                else
                {
                    ShowError("Failed to save agreement. Please try again.");
                }
            }
        }

        // Helper methods for safe conversion
        private int SafeConvertToInt(object value, int defaultValue = 0)
        {
            if (value == null || value == DBNull.Value)
                return defaultValue;

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        private string SafeConvertToString(object value, string defaultValue = "")
        {
            if (value == null || value == DBNull.Value)
                return defaultValue;

            return value.ToString();
        }

        private bool SafeConvertToBool(object value, bool defaultValue = false)
        {
            if (value == null || value == DBNull.Value)
                return defaultValue;

            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        private DateTime? SafeConvertToDateTime(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            try
            {
                return Convert.ToDateTime(value);
            }
            catch
            {
                return null;
            }
        }

        private void UpdateAgreement(SqlConnection connection, string action, int agreementId, int modelId)
        {
            // Get agreement number from database first
            string agreementNumber = "";
            string getNumberQuery = "SELECT agreement_number FROM hardware_agreements WHERE id = @id";
            using (SqlCommand getNumberCmd = new SqlCommand(getNumberQuery, connection))
            {
                getNumberCmd.Parameters.AddWithValue("@id", agreementId);
                object result = getNumberCmd.ExecuteScalar();
                if (result != null)
                {
                    agreementNumber = result.ToString();
                }
            }

            // Get current status from database
            string currentDbStatus = GetAgreementStatus(agreementId);

            // Determine final status and dates
            string finalStatus;
            DateTime? submittedDate = null;
            DateTime issueDate = DateTime.Now;

            if (action == "Draft")
            {
                // Keep as Draft
                finalStatus = "Draft";
            }
            else // Submitted
            {
                if (currentDbStatus == "Draft")
                {
                    // First time submission - set to Pending
                    submittedDate = DateTime.Now;
                    issueDate = submittedDate.Value;
                    finalStatus = "Pending";
                }
                else if (currentDbStatus == "Pending")
                {
                    // Already submitted - update based on radio button selection
                    submittedDate = DateTime.Now;
                    issueDate = submittedDate.Value;
                    finalStatus = rbActive.Checked ? "Active" : "Inactive";
                }
                else
                {
                    // For Active/Inactive status, update based on radio button
                    submittedDate = DateTime.Now;
                    issueDate = submittedDate.Value;
                    finalStatus = rbActive.Checked ? "Active" : "Inactive";
                }
            }

            string query = @"
        UPDATE hardware_agreements SET
        model_id = @modelId,
        serial_number = @serialNumber,
        asset_number = @assetNumber,
        has_carry_bag = @hasCarryBag,
        has_power_adapter = @hasPowerAdapter,
        has_mouse = @hasMouse,
        mouse_type = @mouseType,
        has_vga_converter = @hasVGAConverter,
        other_accessories = @otherAccessories,
        it_staff_win_id = @itStaff,
        issue_date = @issueDate,
        remarks = @remarks,
        agreement_status = @status,
        submitted_date = @submittedDate,
        last_updated = GETDATE(),
        employee_email = @employeeEmail,
        hod_email = @hodEmail
        WHERE id = @id";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                // Use the provided modelId parameter
                command.Parameters.AddWithValue("@modelId", modelId);
                command.Parameters.AddWithValue("@serialNumber", txtSerialNumber.Text.Trim());
                command.Parameters.AddWithValue("@assetNumber", txtAssetNumber.Text.Trim());

                // Email fields
                command.Parameters.AddWithValue("@employeeEmail", hdnEmployeeEmail.Value);
                command.Parameters.AddWithValue("@hodEmail", hdnHODEmail.Value);

                // Accessories
                command.Parameters.AddWithValue("@hasCarryBag", chkCarryBag.Checked ? 1 : 0);
                command.Parameters.AddWithValue("@hasPowerAdapter", chkPowerAdapter.Checked ? 1 : 0);
                command.Parameters.AddWithValue("@hasMouse", chkMouse.Checked ? 1 : 0);

                string mouseType = "";
                if (chkMouse.Checked)
                {
                    mouseType = rbWired.Checked ? "Wired" :
                               rbWireless.Checked ? "Wireless" : "";
                }
                // Send NULL if empty string
                command.Parameters.AddWithValue("@mouseType",
                    string.IsNullOrEmpty(mouseType) ? (object)DBNull.Value : mouseType);

                command.Parameters.AddWithValue("@hasVGAConverter", chkVGAConverter.Checked ? 1 : 0);

                // Find txtOtherAccessories control
                TextBox txtOtherAccessoriesControl = (TextBox)FindControl("txtOtherAccessories");
                command.Parameters.AddWithValue("@otherAccessories",
                    (txtOtherAccessoriesControl != null && !string.IsNullOrEmpty(txtOtherAccessoriesControl.Text)) ?
                    (object)txtOtherAccessoriesControl.Text.Trim() : DBNull.Value);

                // IT Details
                command.Parameters.AddWithValue("@itStaff", txtITStaff.Text);
                command.Parameters.AddWithValue("@issueDate", issueDate);

                // Remarks - find control
                TextBox txtRemarksControl = (TextBox)FindControl("txtRemarks");
                command.Parameters.AddWithValue("@remarks",
                    (txtRemarksControl != null && !string.IsNullOrEmpty(txtRemarksControl.Text)) ?
                    (object)txtRemarksControl.Text.Trim() : DBNull.Value);

                // Status and dates
                command.Parameters.AddWithValue("@status", finalStatus);
                command.Parameters.AddWithValue("@submittedDate",
                    submittedDate.HasValue ? (object)submittedDate.Value : DBNull.Value);
                command.Parameters.AddWithValue("@id", agreementId);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    if (action == "Draft")
                    {
                        ShowSuccess("Draft updated successfully!");
                    }
                    else
                    {
                        bool emailSent = SendAgreementEmail(action, agreementNumber, finalStatus);
                        if (emailSent)
                        {
                            ShowSuccess($"Agreement updated successfully!");
                        }
                        else
                        {
                            ShowSuccess($"Agreement updated successfully! Status: {finalStatus}.");
                        }
                    }

                    // Always redirect to ExistingAgreements.aspx after update
                    string script = "<script type='text/javascript'>" +
                                    "setTimeout(function(){ window.location.href = 'ExistingAgreements.aspx'; }, 2000);" +
                                    "</script>";
                    ClientScript.RegisterStartupScript(this.GetType(), "redirect", script);
                }
                else
                {
                    ShowError("Failed to update agreement. Please try again.");
                }
            }
        }

        private string GenerateAgreementNumber()
        {
            // Generate agreement number: AGREEMENT-YYYYMMDD-XXXX
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            string randomPart = new Random().Next(1000, 9999).ToString();
            return $"AGREEMENT-{datePart}-{randomPart}";
        }

        protected void btnSaveDraft_Click(object sender, EventArgs e)
        {
            SaveAgreement("Draft");
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            SaveAgreement("Submitted");
        }

        protected void btnEdit_Click(object sender, EventArgs e)
        {
            if (!currentAgreementId.HasValue) return;

            // Check if it's actually a draft before allowing edit
            string actualStatus = GetCurrentAgreementStatus();
            if (actualStatus == "Draft")
            {
                // Switch to edit mode
                Response.Redirect($"Agreement.aspx?id={currentAgreementId}");
            }
            else
            {
                ShowError("Only draft agreements can be edited.");
            }
        }

        protected void btnExportPDF_Click(object sender, EventArgs e)
        {
            if (currentAgreementId.HasValue)
            {
                Response.Redirect($"ExportPDF.ashx?id={currentAgreementId.Value}");
            }
        }

        protected void btnSaveUpdate_Click(object sender, EventArgs e)
        {
            if (!currentAgreementId.HasValue)
            {
                ShowError("No agreement selected for update.");
                return;
            }

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Resolve model ID
                    int modelId = 0;
                    string selectedModel = ddlModel.SelectedValue;
                    if (selectedModel == "OTHER")
                    {
                        // Insert new model
                        string modelName = txtOtherModel.Text.Trim();
                        string modelType = ddlDeviceType.SelectedValue;
                        string insertModel = "INSERT INTO hardware_model (model, type) VALUES (@model, @type); SELECT SCOPE_IDENTITY();";
                        using (SqlCommand cmd = new SqlCommand(insertModel, connection))
                        {
                            cmd.Parameters.AddWithValue("@model", modelName);
                            cmd.Parameters.AddWithValue("@type", modelType);
                            object result = cmd.ExecuteScalar();
                            modelId = Convert.ToInt32(result);
                        }
                    }
                    else if (!string.IsNullOrEmpty(selectedModel))
                    {
                        modelId = Convert.ToInt32(selectedModel);
                    }

                    // Generate new token and expiry
                    string newToken = Guid.NewGuid().ToString("N").Substring(0, 32);
                    DateTime tokenExpiry = DateTime.Now.AddDays(7);

                    // Mouse type
                    string mouseType = "";
                    if (chkMouse.Checked)
                    {
                        mouseType = rbWired.Checked ? "Wired" : rbWireless.Checked ? "Wireless" : "";
                    }

                    // Get text from FindControl controls
                    TextBox txtOtherAccessoriesControl = (TextBox)FindControl("txtOtherAccessories");
                    TextBox txtRemarksControl = (TextBox)FindControl("txtRemarks");
                    string otherAccessories = txtOtherAccessoriesControl != null ? txtOtherAccessoriesControl.Text.Trim() : "";
                    string remarks = txtRemarksControl != null ? txtRemarksControl.Text.Trim() : "";

                    // Update Phase 1 fields + regenerate token
                    string updateQuery = @"
                        UPDATE hardware_agreements SET
                            model_id = @modelId,
                            serial_number = @serialNumber,
                            asset_number = @assetNumber,
                            has_carry_bag = @hasCarryBag,
                            has_power_adapter = @hasPowerAdapter,
                            has_mouse = @hasMouse,
                            mouse_type = @mouseType,
                            has_vga_converter = @hasVGAConverter,
                            other_accessories = @otherAccessories,
                            issue_date = @issueDate,
                            remarks = @remarks,
                            employee_email = @employeeEmail,
                            hod_email = @hodEmail,
                            agreement_view_token = @newToken,
                            token_expiry_date = @tokenExpiry,
                            last_updated = GETDATE(),
                            submitted_date = GETDATE()
                        WHERE id = @id AND agreement_status = 'Pending'";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", currentAgreementId.Value);
                        cmd.Parameters.AddWithValue("@modelId", modelId);
                        cmd.Parameters.AddWithValue("@serialNumber", txtSerialNumber.Text.Trim());
                        cmd.Parameters.AddWithValue("@assetNumber", txtAssetNumber.Text.Trim());
                        cmd.Parameters.AddWithValue("@hasCarryBag", chkCarryBag.Checked ? 1 : 0);
                        cmd.Parameters.AddWithValue("@hasPowerAdapter", chkPowerAdapter.Checked ? 1 : 0);
                        cmd.Parameters.AddWithValue("@hasMouse", chkMouse.Checked ? 1 : 0);
                        cmd.Parameters.AddWithValue("@mouseType", string.IsNullOrEmpty(mouseType) ? (object)"" : mouseType);
                        cmd.Parameters.AddWithValue("@hasVGAConverter", chkVGAConverter.Checked ? 1 : 0);
                        cmd.Parameters.AddWithValue("@otherAccessories", otherAccessories);
                        cmd.Parameters.AddWithValue("@issueDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@remarks", remarks);
                        cmd.Parameters.AddWithValue("@employeeEmail", string.IsNullOrEmpty(hdnEmployeeEmail.Value) ? DBNull.Value : (object)hdnEmployeeEmail.Value);
                        cmd.Parameters.AddWithValue("@hodEmail", string.IsNullOrEmpty(hdnHODEmail.Value) ? DBNull.Value : (object)hdnHODEmail.Value);
                        cmd.Parameters.AddWithValue("@newToken", newToken);
                        cmd.Parameters.AddWithValue("@tokenExpiry", tokenExpiry);

                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            // Send updated notification email (same as initial submission)
                            try
                            {
                                SendSaveUpdateEmail(currentAgreementId.Value, newToken);
                            }
                            catch (Exception emailEx)
                            {
                                System.Diagnostics.Debug.WriteLine("SaveUpdate email error: " + emailEx.Message);
                            }

                            ShowSuccess("Agreement updated successfully! A new notification email has been sent to the employee.");

                            string script = "<script type='text/javascript'>" +
                                            "setTimeout(function(){ window.location.href = 'ExistingAgreements.aspx'; }, 2000);" +
                                            "</script>";
                            ClientScript.RegisterStartupScript(this.GetType(), "redirect", script);
                        }
                        else
                        {
                            ShowError("Agreement could not be updated. It may no longer be in Pending status.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Error updating agreement: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("btnSaveUpdate error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Sends re-notification email after IT admin updates a Pending agreement.
        /// Uses the same template as the initial submission email.
        /// </summary>
        private void SendSaveUpdateEmail(int agreementId, string newToken)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            string agreementNumber = "", employeeEmail = "", hodEmail = "", itStaff = "";
            string modelName = "", serialNumber = "", assetNumber = "";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string q = @"SELECT a.agreement_number, a.employee_email, a.hod_email, a.it_staff_win_id,
                                    a.serial_number, a.asset_number, m.model
                             FROM hardware_agreements a
                             LEFT JOIN hardware_model m ON a.model_id = m.id
                             WHERE a.id = @id";
                using (SqlCommand cmd = new SqlCommand(q, conn))
                {
                    cmd.Parameters.AddWithValue("@id", agreementId);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            agreementNumber = SafeConvertToString(rdr["agreement_number"]);
                            employeeEmail = SafeConvertToString(rdr["employee_email"]);
                            hodEmail = SafeConvertToString(rdr["hod_email"]);
                            itStaff = SafeConvertToString(rdr["it_staff_win_id"]);
                            serialNumber = SafeConvertToString(rdr["serial_number"]);
                            assetNumber = SafeConvertToString(rdr["asset_number"]);
                            modelName = SafeConvertToString(rdr["model"]);
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(employeeEmail)) return;

            // Build token URL
            string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd('/');
            string tokenUrl = $"{baseUrl}/Agreement.aspx?token={newToken}";

            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
            mail.From = new System.Net.Mail.MailAddress("laptop_pc_agreement@pancentury.com", "Laptop/PC Agreement System");
            mail.To.Add(employeeEmail);

            if (!string.IsNullOrEmpty(hodEmail))
                mail.CC.Add(hodEmail);

            // CC all IT admins
            CcAllAdmins(mail);

            mail.Subject = $"[Updated] Laptop/PC Agreement {agreementNumber} - Pending";
            mail.IsBodyHtml = true;

            string tokenSection = $@"<div style='background-color: #f0f9ff; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #3b82f6;'>
                    <h3 style='color: #1e40af; margin-top: 0;'>Action Required</h3>
                    <p>The IT department has updated the details of your laptop/pc agreement. Please review and sign using the new link below:</p>
                    <p style='margin: 15px 0;'>
                        <a href='{tokenUrl}' style='display: inline-block; background-color: #3b82f6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>
                            Sign Agreement Now
                        </a>
                    </p>
                    <p style='font-size: 0.9rem; color: #4b5563;'>
                        <strong>Note:</strong> This link will expire in 7 days. Any previous signing links are no longer valid.
                    </p>
                    <p style='font-size: 0.8rem; color: #6b7280; margin-top: 10px;'>
                        Agreement Number: <strong>{agreementNumber}</strong><br>
                        Or copy this link: <span style='background-color: #f3f4f6; padding: 5px; border-radius: 4px; font-family: monospace;'>{tokenUrl}</span>
                    </p>
                </div>";

            mail.Body = $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #667eea; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #ddd; }}
        .details-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        .details-table th, .details-table td {{ padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }}
        .details-table th {{ background-color: #f2f2f2; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Laptop/PC Agreement Notification</h1>
            <p style='margin: 5px 0 0; opacity: 0.9; font-size: 0.95em;'>Agreement Updated by IT Department</p>
        </div>
        <div class='content'>
            <h2>Agreement Details</h2>
            <table class='details-table'>
                <tr><th>Agreement Number:</th><td>{agreementNumber}</td></tr>
                <tr><th>Status:</th><td>Pending</td></tr>
                <tr><th>Model:</th><td>{modelName}</td></tr>
                <tr><th>Serial Number:</th><td>{serialNumber}</td></tr>
                <tr><th>Asset Number:</th><td>{assetNumber}</td></tr>
                <tr><th>IT Staff:</th><td>{itStaff}</td></tr>
                <tr><th>Issue Date:</th><td>{DateTime.Now.ToString("dd/MM/yyyy")}</td></tr>
            </table>
            {tokenSection}
        </div>
    </div>
</body>
</html>";

            using (System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient())
            {
                smtp.Timeout = 10000;
                smtp.Send(mail);
            }
        }

        protected void btnDelete_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Archive button clicked");

            if (!currentAgreementId.HasValue)
            {
                ShowError("No agreement selected for archiving.");
                return;
            }

            ArchiveAgreement();
        }

        private void ArchiveAgreement()
        {
            if (!currentAgreementId.HasValue)
            {
                ShowError("No agreement to archive.");
                return;
            }

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "UPDATE hardware_agreements SET agreement_status = 'Archived', last_updated = GETDATE() WHERE id = @id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", currentAgreementId.Value);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            ShowSuccess("Agreement archived successfully!");

                            // Redirect to ExistingAgreements.aspx after 2 seconds
                            string script = "<script type='text/javascript'>" +
                                            "setTimeout(function(){ window.location.href = 'ExistingAgreements.aspx'; }, 2000);" +
                                            "</script>";
                            ClientScript.RegisterStartupScript(this.GetType(), "redirect", script);
                        }
                        else
                        {
                            ShowError("Agreement not found or could not be archived.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Error archiving agreement: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("Archive error: " + ex.Message);
                }
            }
        }

        private void ShowSuccess(string message)
        {
            if (successText != null)
            {
                successText.InnerText = message;
            }
            messageSuccess.Visible = true;
            messageError.Visible = false;
        }

        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.InnerText = message;
            }
            messageError.Visible = true;
            messageSuccess.Visible = false;
        }

        // Employee Methods
        private void ValidateEmployeeAccess(string token)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string currentWinId = User.Identity.Name;
                    System.Diagnostics.Debug.WriteLine($"ValidateEmployeeAccess - Token: {token}");
                    System.Diagnostics.Debug.WriteLine($"ValidateEmployeeAccess - Current User: {currentWinId}");

                    // Variables to store results
                    int? agreementId = null;
                    string status = null;
                    string employeeEmail = null;
                    string storedToken = null;
                    string existingName = null;
                    string existingId = null;
                    bool recordFound = false;

                    // FIRST QUERY: Get agreement by token
                    string getIdQuery = @"
                SELECT a.id, a.agreement_status, a.token_expiry_date, 
                       a.employee_email, a.agreement_view_token,
                       a.employee_name, a.employee_id, a.employee_position, a.employee_department
                FROM hardware_agreements a
                WHERE a.agreement_view_token = @token 
                AND (a.token_expiry_date IS NULL OR a.token_expiry_date > GETDATE())
                AND a.agreement_status IN ('Pending', 'Active')";

                    using (SqlCommand command = new SqlCommand(getIdQuery, connection))
                    {
                        command.Parameters.AddWithValue("@token", token);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                recordFound = true;
                                agreementId = Convert.ToInt32(reader["id"]);
                                status = reader["agreement_status"].ToString();
                                employeeEmail = SafeConvertToString(reader["employee_email"]);
                                storedToken = SafeConvertToString(reader["agreement_view_token"]);
                                existingName = SafeConvertToString(reader["employee_name"]);
                                existingId = SafeConvertToString(reader["employee_id"]);
                            }
                        } // DataReader is closed here
                    } // Command is disposed here

                    if (recordFound)
                    {
                        System.Diagnostics.Debug.WriteLine($"ValidateEmployeeAccess - Found Agreement ID: {agreementId}");
                        System.Diagnostics.Debug.WriteLine($"ValidateEmployeeAccess - Status: {status}");

                        // Token mismatch check
                        if (storedToken != token)
                        {
                            ShowError("Token mismatch. Please use the link provided in your email.");
                            currentAgreementId = null;
                            pnlEmployeeSignature.Visible = false;
                            phaseAgree.Visible = false;
                            return;
                        }

                        // Set agreement ID
                        currentAgreementId = agreementId;
                        currentStatus = status;

                        // Check if already completed or agreed
                        if (currentStatus == "Completed" || currentStatus == "Agreed" || !string.IsNullOrEmpty(existingName))
                        {
                            ShowError("This agreement has already been signed.");
                            DisableEmployeeForm();
                            pnlEmployeeSignature.Visible = true;
                            return;
                        }

                        System.Diagnostics.Debug.WriteLine($"ValidateEmployeeAccess - SUCCESS! Agreement ID set to: {currentAgreementId}");
                    }
                    else
                    {
                        // No matching record found - run debug query AFTER first reader is closed
                        string debugQuery = @"
                    SELECT id, agreement_status, token_expiry_date, agreement_view_token,
                           employee_name, employee_id
                    FROM hardware_agreements 
                    WHERE agreement_view_token = @debugToken";

                        using (SqlCommand debugCmd = new SqlCommand(debugQuery, connection))
                        {
                            debugCmd.Parameters.AddWithValue("@debugToken", token);
                            using (SqlDataReader debugReader = debugCmd.ExecuteReader())
                            {
                                if (debugReader.Read())
                                {
                                    string debugStatus = debugReader["agreement_status"].ToString();
                                    DateTime? expiryDate = debugReader["token_expiry_date"] != DBNull.Value
                                        ? (DateTime?)Convert.ToDateTime(debugReader["token_expiry_date"])
                                        : null;
                                    string debugName = SafeConvertToString(debugReader["employee_name"]);
                                    string debugEmployeeId = SafeConvertToString(debugReader["employee_id"]);

                                    System.Diagnostics.Debug.WriteLine($"ValidateEmployeeAccess - Debug: Status={debugStatus}, Expiry={expiryDate}");

                                    if (!string.IsNullOrEmpty(debugName) || !string.IsNullOrEmpty(debugEmployeeId))
                                    {
                                        ShowError("This agreement has already been completed and signed.");
                                    }
                                    else if (expiryDate.HasValue && expiryDate.Value < DateTime.Now)
                                    {
                                        ShowError("This access link has expired. Please contact IT support for a new link.");
                                    }
                                    else if (debugStatus == "Draft")
                                    {
                                        ShowError("This agreement has not been submitted yet. Please contact IT support.");
                                    }
                                    else if (debugStatus != "Pending" && debugStatus != "Active")
                                    {
                                        ShowError($"This agreement cannot be signed. Current status: {debugStatus}");
                                    }
                                    else
                                    {
                                        ShowError("Unable to access this agreement. Please contact IT support.");
                                    }
                                }
                                else
                                {
                                    ShowError("Invalid access token. The link may be incorrect or has been invalidated.");
                                }
                            }
                        }

                        currentAgreementId = null;
                        pnlEmployeeSignature.Visible = false;
                        phaseAgree.Visible = false;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ValidateEmployeeAccess - ERROR: {ex.Message}");
                    ShowError("Error validating access token: " + ex.Message);
                    currentAgreementId = null;
                    pnlEmployeeSignature.Visible = false;
                    phaseAgree.Visible = false;
                }
            }
        }
        private void LoadEmployeeData(int agreementId)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                SELECT employee_name, employee_id, employee_position, employee_department, employee_agreed_date
                FROM hardware_agreements 
                WHERE id = @id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", agreementId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string empName = SafeConvertToString(reader["employee_name"]);
                                if (!string.IsNullOrEmpty(empName))
                                    txtEmpName.Text = empName;

                                string empId = SafeConvertToString(reader["employee_id"]);
                                if (!string.IsNullOrEmpty(empId))
                                    txtEmpId.Text = empId;

                                string empPosition = SafeConvertToString(reader["employee_position"]);
                                if (!string.IsNullOrEmpty(empPosition))
                                    txtEmpPosition.Text = empPosition;

                                string empDepartment = SafeConvertToString(reader["employee_department"]);
                                if (!string.IsNullOrEmpty(empDepartment))
                                    txtEmpDepartment.Text = empDepartment;

                                DateTime? agreedDate = SafeConvertToDateTime(reader["employee_agreed_date"]);
                                if (agreedDate.HasValue)
                                    txtEmpSignatureDate.Text = agreedDate.Value.ToString("dd/MM/yyyy HH:mm");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Silent error - not critical
                }
            }
        }

        private void EnableEmployeeSignatureSection()
        {
            // Only auto-fill if fields are empty (to preserve user input on PostBack)
            if (string.IsNullOrEmpty(txtEmpId.Text))
            {
                string userName = User.Identity.Name;
                string shortUserName = userName;
                if (userName.Contains("\\"))
                {
                    shortUserName = userName.Split('\\')[1];
                }

                // Set the Employee ID (Windows ID) - readonly field
                txtEmpId.Text = shortUserName;
            }

            // Only set signature date if empty
            if (string.IsNullOrEmpty(txtEmpSignatureDate.Text))
            {
                txtEmpSignatureDate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            }
        }



        protected void btnSubmitEmployee_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== EMPLOYEE SUBMIT START ===");
            System.Diagnostics.Debug.WriteLine($"Page.IsPostBack: {IsPostBack}");
            System.Diagnostics.Debug.WriteLine($"Page.IsValid: {Page.IsValid}");
            System.Diagnostics.Debug.WriteLine($"Employee Mode: {isEmployeeMode}");

            // Check ALL field values
            System.Diagnostics.Debug.WriteLine($"txtEmpName.Text: '{txtEmpName.Text}'");
            System.Diagnostics.Debug.WriteLine($"txtEmpName.Text Length: {txtEmpName.Text.Length}");
            System.Diagnostics.Debug.WriteLine($"txtEmpPosition.Text: '{txtEmpPosition.Text}'");
            System.Diagnostics.Debug.WriteLine($"txtEmpDepartment.Text: '{txtEmpDepartment.Text}'");
            System.Diagnostics.Debug.WriteLine($"txtEmpId.Text: '{txtEmpId.Text}'");
            System.Diagnostics.Debug.WriteLine($"chkAgreeTerms.Checked: {chkAgreeTerms.Checked}");

            // Check if fields are enabled/readonly
            System.Diagnostics.Debug.WriteLine($"txtEmpName.Enabled: {txtEmpName.Enabled}");
            System.Diagnostics.Debug.WriteLine($"txtEmpName.ReadOnly: {txtEmpName.ReadOnly}");

            // Debug: Show current agreement ID
            if (!currentAgreementId.HasValue)
            {
                // Try multiple ways to get the agreement ID
                System.Diagnostics.Debug.WriteLine($"currentAgreementId is null, trying to recover...");

                // 1. From hidden field
                if (!string.IsNullOrEmpty(hdnAgreementId.Value))
                {
                    int parsedId;
                    if (int.TryParse(hdnAgreementId.Value, out parsedId))
                    {
                        currentAgreementId = parsedId;
                        System.Diagnostics.Debug.WriteLine($"Recovered from hdnAgreementId: {currentAgreementId}");
                    }
                }

                // 2. From ViewState
                if (!currentAgreementId.HasValue && ViewState["CurrentAgreementId"] != null)
                {
                    currentAgreementId = (int?)ViewState["CurrentAgreementId"];
                    System.Diagnostics.Debug.WriteLine($"Recovered from ViewState: {currentAgreementId}");
                }

                if (!currentAgreementId.HasValue)
                {
                    ShowError($"No agreement selected. CurrentAgreementId is null. Token: {accessToken}");
                    return;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Agreement ID: {currentAgreementId}");

            // Validate employee name
            if (string.IsNullOrWhiteSpace(txtEmpName.Text))
            {
                ShowError("Please enter your name.");
                System.Diagnostics.Debug.WriteLine("ERROR: Employee name is empty!");
                return;
            }

            // Validate position
            if (string.IsNullOrWhiteSpace(txtEmpPosition.Text))
            {
                ShowError("Please enter your position/job title.");
                return;
            }

            // Validate department
            if (string.IsNullOrWhiteSpace(txtEmpDepartment.Text))
            {
                ShowError("Please enter your department.");
                return;
            }

            if (!chkAgreeTerms.Checked)
            {
                ShowError("You must agree to the terms and conditions.");
                return;
            }

            System.Diagnostics.Debug.WriteLine("All validations passed. Proceeding to save...");

            // Save to database
            SaveEmployeeAgreement();

            System.Diagnostics.Debug.WriteLine("=== EMPLOYEE SUBMIT END ===");
        }

        private void SaveEmployeeAgreement()
        {
            System.Diagnostics.Debug.WriteLine($"=== SaveEmployeeAgreement() START ===");

            // Get Windows ID from current user
            string windowsId = User.Identity.Name;
            if (windowsId.Contains("\\"))
            {
                windowsId = windowsId.Split('\\')[1];
            }

            System.Diagnostics.Debug.WriteLine($"WINDOWS ID: {windowsId}");
            System.Diagnostics.Debug.WriteLine($"Employee Name from form: '{txtEmpName.Text}'");
            System.Diagnostics.Debug.WriteLine($"Employee Position from form: '{txtEmpPosition.Text}'");
            System.Diagnostics.Debug.WriteLine($"Employee Department from form: '{txtEmpDepartment.Text}'");

            if (!currentAgreementId.HasValue)
            {
                ShowError("No agreement selected.");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"AGREEMENT ID: {currentAgreementId.Value}");

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Use a transaction for data integrity
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Execute the update
                            string updateQuery = @"
                        UPDATE hardware_agreements SET
                            employee_name = @employeeName,
                            employee_id = @employeeId,
                            employee_position = @employeePosition,
                            employee_department = @employeeDepartment,
                            employee_agreed_date = GETDATE(),
                            agreement_status = 'Agreed',
                            last_updated = GETDATE(),
                            agreement_view_token = NULL,
                            token_expiry_date = NULL
                        WHERE id = @id";

                            int rowsAffected = 0;

                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, connection, transaction))
                            {
                                updateCmd.Parameters.Add("@employeeName", SqlDbType.NVarChar, 255).Value = txtEmpName.Text.Trim();
                                updateCmd.Parameters.Add("@employeeId", SqlDbType.NVarChar, 50).Value = windowsId;
                                updateCmd.Parameters.Add("@employeePosition", SqlDbType.NVarChar, 255).Value = txtEmpPosition.Text.Trim();
                                updateCmd.Parameters.Add("@employeeDepartment", SqlDbType.NVarChar, 255).Value = txtEmpDepartment.Text.Trim();
                                updateCmd.Parameters.Add("@id", SqlDbType.Int).Value = currentAgreementId.Value;

                                rowsAffected = updateCmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"UPDATE rows affected: {rowsAffected}");
                            }

                            if (rowsAffected > 0)
                            {
                                // Verify the update using ExecuteScalar (avoids DataReader issues)
                                string verifyQuery = "SELECT employee_name FROM hardware_agreements WHERE id = @id";
                                string updatedName = null;

                                using (SqlCommand verifyCmd = new SqlCommand(verifyQuery, connection, transaction))
                                {
                                    verifyCmd.Parameters.AddWithValue("@id", currentAgreementId.Value);
                                    object result = verifyCmd.ExecuteScalar();
                                    updatedName = result as string;
                                }

                                System.Diagnostics.Debug.WriteLine($"Verification - Updated Name: '{updatedName}'");

                                if (string.IsNullOrEmpty(updatedName))
                                {
                                    System.Diagnostics.Debug.WriteLine("ERROR: Name is still NULL!");
                                    transaction.Rollback();
                                    ShowError("UPDATE executed but employee_name is still NULL. Database issue detected.");
                                    return;
                                }

                                // Success - commit the transaction
                                transaction.Commit();
                                System.Diagnostics.Debug.WriteLine("Transaction COMMITTED - Data saved successfully!");

                                // Send confirmation email
                                try
                                {
                                    SendConfirmationEmail(windowsId);
                                }
                                catch (Exception emailEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Email sending failed: {emailEx.Message}");
                                    // Don't fail the whole process if email fails
                                }

                                // Show success and redirect to Default.aspx (NOT reload!)
                                ShowSuccess("Agreement submitted successfully! Pending IT verification. Thank you for signing.");

                                // Disable the form to prevent re-submission
                                DisableEmployeeForm();
                                btnSubmitEmployee.Enabled = false;
                                btnSubmitEmployee.Visible = false;

                                // Redirect to Default.aspx after showing alert
                                string script = @"<script type='text/javascript'>
                            alert('Agreement submitted successfully! It is now pending IT verification. Thank you for signing.');
                            window.location.href = 'Default.aspx';
                        </script>";
                                ClientScript.RegisterStartupScript(this.GetType(), "successMsg", script);
                                return;
                            }
                            else
                            {
                                transaction.Rollback();
                                System.Diagnostics.Debug.WriteLine("No rows affected - transaction rolled back");
                                ShowError("No agreement found with that ID.");
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Transaction ERROR: {ex.Message}");
                            ShowError($"Database error: {ex.Message}");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"General ERROR: {ex.Message}");
                    ShowError($"Error: {ex.Message}");
                }
            }
        }

        private void DisableEmployeeForm()
        {
            // Disable employee info fields
            txtEmpName.Enabled = false;
            txtEmpPosition.Enabled = false;
            txtEmpDepartment.Enabled = false;

            // Disable checkboxes and buttons
            chkAgreeTerms.Enabled = false;
            btnSubmitEmployee.Enabled = false;
        }

        /// <summary>
        /// Returns email addresses for all active IT admins (admin=1, active=1)
        /// </summary>
        private System.Collections.Generic.List<string> GetAllAdminEmails()
        {
            var emails = new System.Collections.Generic.List<string>();
            string cs = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT email FROM hardware_users WHERE admin = 1 AND active = 1 AND email IS NOT NULL AND email != ''", conn))
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            string e = rdr["email"].ToString().Trim();
                            if (!string.IsNullOrEmpty(e)) emails.Add(e);
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("GetAllAdminEmails error: " + ex.Message); }
            }
            return emails;
        }

        /// <summary>
        /// CC all IT admins on a mail message, skipping duplicates already in To/CC
        /// </summary>
        private void CcAllAdmins(System.Net.Mail.MailMessage mail)
        {
            var existing = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in mail.To) existing.Add(a.Address);
            foreach (var a in mail.CC) existing.Add(a.Address);
            foreach (string email in GetAllAdminEmails())
            {
                if (!existing.Contains(email))
                {
                    mail.CC.Add(email);
                    existing.Add(email);
                }
            }
        }

        private void SendConfirmationEmail(string windowsId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== SendConfirmationEmail() START ===");
                System.Diagnostics.Debug.WriteLine($"Agreement ID: {currentAgreementId}");

                if (!currentAgreementId.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine("SendConfirmationEmail - No agreement ID!");
                    return;
                }

                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

                string agreementNumber = "";
                string employeeEmail = "";
                string hodEmail = "";
                string itStaff = "";
                string employeeName = "";
                string employeePosition = "";
                string employeeDepartment = "";
                string modelName = "";
                string serialNumber = "";
                string assetNumber = "";
                string itStaffEmail = "";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT a.agreement_number, a.employee_email, a.hod_email, a.it_staff_win_id,
                       COALESCE(a.employee_name, '') as employee_name, 
                       COALESCE(a.employee_position, '') as employee_position, 
                       COALESCE(a.employee_department, '') as employee_department,
                       a.serial_number, a.asset_number,
                       m.model
                FROM hardware_agreements a
                LEFT JOIN hardware_model m ON a.model_id = m.id
                WHERE a.id = @id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", currentAgreementId.Value);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                agreementNumber = SafeConvertToString(reader["agreement_number"]);
                                employeeEmail = SafeConvertToString(reader["employee_email"]);
                                hodEmail = SafeConvertToString(reader["hod_email"]);
                                itStaff = SafeConvertToString(reader["it_staff_win_id"]);
                                employeeName = SafeConvertToString(reader["employee_name"]);
                                employeePosition = SafeConvertToString(reader["employee_position"]);
                                employeeDepartment = SafeConvertToString(reader["employee_department"]);
                                serialNumber = SafeConvertToString(reader["serial_number"]);
                                assetNumber = SafeConvertToString(reader["asset_number"]);
                                modelName = SafeConvertToString(reader["model"]);
                            }
                        }
                    }

                    // Get IT staff email from hardware_users table
                    if (!string.IsNullOrEmpty(itStaff))
                    {
                        string itEmailQuery = "SELECT email FROM hardware_users WHERE win_id = @winId AND active = 1";
                        using (SqlCommand itCmd = new SqlCommand(itEmailQuery, connection))
                        {
                            itCmd.Parameters.AddWithValue("@winId", itStaff);
                            object result = itCmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                itStaffEmail = result.ToString();
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"EMAIL DATA - Agreement: {agreementNumber}, Employee: {employeeEmail}, HOD: {hodEmail}");

                if (string.IsNullOrEmpty(employeeEmail))
                {
                    System.Diagnostics.Debug.WriteLine("SendConfirmationEmail - No employee email!");
                    return;
                }

                // Build the email
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("hardware_agreement@pancentury.com", "Laptop/PC Agreement System");
                mail.To.Add(employeeEmail);

                if (!string.IsNullOrEmpty(hodEmail))
                {
                    mail.CC.Add(hodEmail);
                }

                // CC all IT admins so they get notified to verify
                CcAllAdmins(mail);

                // Build verification URL for IT admin
                string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd('/');
                string verifyUrl = $"{baseUrl}/Agreement.aspx?id={currentAgreementId.Value}&mode=view";

                mail.Subject = $"Laptop/PC Agreement {agreementNumber} - Agreed (Pending IT Verification)";
                mail.IsBodyHtml = true;

                string signatureDateStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                mail.Body = $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f59e0b; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #ddd; }}
        .details-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        .details-table th, .details-table td {{ padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }}
        .details-table th {{ background-color: #f2f2f2; width: 40%; }}
        .status-badge {{ background-color: #f59e0b; color: white; padding: 5px 15px; border-radius: 20px; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Agreement Signed by Employee</h1>
        </div>
        <div class='content'>
            <p>Dear {employeeName},</p>
            <p>Your laptop/pc agreement has been successfully signed. It is now <strong>pending IT verification</strong> before final completion.</p>
            
            <h3>Agreement Details</h3>
            <table class='details-table'>
                <tr><th>Agreement Number:</th><td>{agreementNumber}</td></tr>
                <tr><th>Status:</th><td><span class='status-badge'>Agreed - Pending IT Verification</span></td></tr>
                <tr><th>Employee Name:</th><td>{employeeName}</td></tr>
                <tr><th>Position:</th><td>{employeePosition}</td></tr>
                <tr><th>Department:</th><td>{employeeDepartment}</td></tr>
                <tr><th>Model:</th><td>{modelName}</td></tr>
                <tr><th>Serial Number:</th><td>{serialNumber}</td></tr>
                <tr><th>Asset Number:</th><td>{assetNumber}</td></tr>
                <tr><th>Signed Date:</th><td>{signatureDateStr}</td></tr>
                <tr><th>IT Staff:</th><td>{itStaff}</td></tr>
            </table>
            
            <div style='background-color: #fffbeb; padding: 15px; border-radius: 8px; border-left: 4px solid #f59e0b; margin: 20px 0;'>
                <p style='margin: 0; color: #92400e;'><strong>Next Step:</strong> IT department will verify the laptop/pc and system configuration. You will receive a final confirmation email once the verification is completed.</p>
            </div>

            <div style='background-color: #f0f9ff; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #3b82f6;'>
                <h3 style='color: #1e40af; margin-top: 0;'>IT Admin: Verification Required</h3>
                <p>The employee has signed this agreement. Please verify the laptop/pc and system configuration.</p>
                <p style='margin: 15px 0;'>
                    <a href='{verifyUrl}' style='display: inline-block; background-color: #f59e0b; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>
                        Verify Agreement Now
                    </a>
                </p>
                <p style='font-size: 0.8rem; color: #6b7280; margin-top: 10px;'>
                    Or copy this link: <span style='background-color: #f3f4f6; padding: 5px; border-radius: 4px; font-family: monospace;'>{verifyUrl}</span>
                </p>
            </div>
            
            <p>If you have any questions, please contact the IT department.</p>
            
            <p style='margin-top: 30px; color: #666; font-size: 0.9em;'>
                This is an automated message from the Laptop/PC Agreement System.
            </p>
        </div>
    </div>
</body>
</html>";

                // Send the email
                using (SmtpClient smtpClient = new SmtpClient())
                {
                    smtpClient.Timeout = 30000; // 30 seconds timeout
                    smtpClient.Send(mail);
                    System.Diagnostics.Debug.WriteLine($"SendConfirmationEmail - Email sent successfully to {employeeEmail}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendConfirmationEmail ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                // Don't throw - just log the error
            }
        }

        // Updated SendAgreementEmail method with token generation
        private bool SendAgreementEmail(string action, string agreementNumber, string status)
        {
            try
            {
                // Get email addresses from hidden fields
                string employeeEmail = hdnEmployeeEmail.Value;
                string hodEmail = hdnHODEmail.Value;

                if (string.IsNullOrEmpty(employeeEmail) || string.IsNullOrEmpty(hodEmail))
                {
                    return false;
                }

                // Get model name for email
                string modelName = "";
                if (ddlModel.SelectedValue == "OTHER")
                {
                    modelName = txtOtherModel.Text.Trim();
                }
                else if (ddlModel.SelectedItem != null)
                {
                    modelName = ddlModel.SelectedItem.Text;
                }

                // Get token and construct URL
                string tokenSection = "";

                if (action == "Submitted" && currentAgreementId.HasValue)
                {
                    // Get the agreement number from database (not parameter)
                    string dbAgreementNumber = "";
                    string token = "";

                    // Get both agreement number and token from database
                    using (SqlConnection connection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString))
                    {
                        connection.Open();
                        string query = "SELECT agreement_number, agreement_view_token FROM hardware_agreements WHERE id = @id";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@id", currentAgreementId.Value);
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    dbAgreementNumber = SafeConvertToString(reader["agreement_number"]);
                                    token = SafeConvertToString(reader["agreement_view_token"]);
                                }
                            }
                        }
                    }

                    // Use the actual agreement number from database
                    if (string.IsNullOrEmpty(dbAgreementNumber))
                    {
                        dbAgreementNumber = agreementNumber;
                    }

                    if (!string.IsNullOrEmpty(token))
                    {
                        // Construct the URL correctly
                        string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd('/');
                        string tokenUrl = $"{baseUrl}/Agreement.aspx?token={token}";

                        tokenSection = $@"<div style='background-color: #f0f9ff; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #3b82f6;'>
                    <h3 style='color: #1e40af; margin-top: 0;'>Action Required</h3>
                    <p>Please review and sign the laptop/pc agreement by clicking the link below:</p>
                    <p style='margin: 15px 0;'>
                        <a href='{tokenUrl}' style='display: inline-block; background-color: #3b82f6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>
                            Sign Agreement Now
                        </a>
                    </p>
                    <p style='font-size: 0.9rem; color: #4b5563;'>
                        <strong>Note:</strong> This link will expire in 7 days.
                    </p>
                    <p style='font-size: 0.8rem; color: #6b7280; margin-top: 10px;'>
                        Agreement Number: <strong>{dbAgreementNumber}</strong><br>
                        Or copy this link: <span style='background-color: #f3f4f6; padding: 5px; border-radius: 4px; font-family: monospace;'>{tokenUrl}</span>
                    </p>
                </div>";
                    }
                    else
                    {
                        tokenSection = $@"<div style='background-color: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f59e0b;'>
                    <h3 style='color: #b45309; margin-top: 0;'>Important Notice</h3>
                    <p>The agreement signing link could not be generated. Please contact IT support.</p>
                    <p>Agreement Number: <strong>{dbAgreementNumber}</strong></p>
                </div>";
                    }
                }

                // Create email message
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("laptop_pc_agreement@pancentury.com", "Laptop/PC Agreement System");
                mail.To.Add(employeeEmail);
                mail.CC.Add(hodEmail);

                // CC all IT admins
                CcAllAdmins(mail);

                if (action == "Submitted")
                {
                    mail.Subject = $"Laptop/PC Agreement {agreementNumber} - {status}";

                    string body = $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #667eea; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #ddd; }}
        .details-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        .details-table th, .details-table td {{ padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }}
        .details-table th {{ background-color: #f2f2f2; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Laptop/PC Agreement Notification</h1>
        </div>
        <div class='content'>
            <h2>Agreement Details</h2>
            <table class='details-table'>
                <tr><th>Agreement Number:</th><td>{agreementNumber}</td></tr>
                <tr><th>Status:</th><td>{status}</td></tr>
                <tr><th>Model:</th><td>{modelName}</td></tr>
                <tr><th>Serial Number:</th><td>{txtSerialNumber.Text}</td></tr>
                <tr><th>Asset Number:</th><td>{txtAssetNumber.Text}</td></tr>
                <tr><th>IT Staff:</th><td>{txtITStaff.Text}</td></tr>
                <tr><th>Issue Date:</th><td>{DateTime.Now.ToString("dd/MM/yyyy")}</td></tr>
            </table>
            {tokenSection}
        </div>
    </div>
</body>
</html>";

                    mail.Body = body;
                    mail.IsBodyHtml = true;
                }

                // Send email
                using (SmtpClient smtpClient = new SmtpClient())
                {
                    smtpClient.Timeout = 10000;
                    smtpClient.Send(mail);
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Log error quietly
                return false;
            }
        }

        protected void btnVerify_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== IT VERIFY START ===");

            if (!currentAgreementId.HasValue)
            {
                // Try to recover from ViewState/query string
                if (Request.QueryString["id"] != null)
                {
                    int parsedId;
                    if (int.TryParse(Request.QueryString["id"], out parsedId))
                    {
                        currentAgreementId = parsedId;
                    }
                }

                if (!currentAgreementId.HasValue)
                {
                    ShowError("No agreement selected for verification.");
                    return;
                }
            }

            // Validate at least both checkboxes are checked
            if (!chkVerifyHardware.Checked || !chkVerifySystemConfig.Checked)
            {
                ShowError("Please complete both 'Hardware Checklist - Updated' and 'System Configuration' checkboxes before verifying.");
                return;
            }

            // Verify the agreement status is Agreed
            string actualStatus = GetCurrentAgreementStatus();
            if (actualStatus != "Agreed")
            {
                ShowError($"Only agreements with 'Agreed' status can be verified. Current status: {actualStatus}");
                return;
            }

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string verifiedBy = User.Identity.Name;

                    string updateQuery = @"
                        UPDATE hardware_agreements SET
                            it_verify_hardware_checklist = @hardwareChecklist,
                            it_verify_system_config = @systemConfig,
                            it_verify_others = @others,
                            it_verified_by = @verifiedBy,
                            it_verified_date = GETDATE(),
                            agreement_status = 'Completed',
                            last_updated = GETDATE()
                        WHERE id = @id";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@hardwareChecklist", chkVerifyHardware.Checked ? 1 : 0);
                        command.Parameters.AddWithValue("@systemConfig", chkVerifySystemConfig.Checked ? 1 : 0);
                        command.Parameters.AddWithValue("@others",
                            string.IsNullOrEmpty(txtVerifyOthers.Text) ? (object)DBNull.Value : txtVerifyOthers.Text.Trim());
                        command.Parameters.AddWithValue("@verifiedBy", verifiedBy);
                        command.Parameters.AddWithValue("@id", currentAgreementId.Value);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            // Send final completion email
                            try
                            {
                                SendVerificationCompleteEmail(currentAgreementId.Value, verifiedBy);
                            }
                            catch (Exception emailEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Verification email failed: {emailEx.Message}");
                            }

                            ShowSuccess("Agreement verified and completed successfully! Final notification sent to employee and HOD.");

                            string script = "<script type='text/javascript'>" +
                                            "setTimeout(function(){ window.location.href = 'ExistingAgreements.aspx'; }, 2000);" +
                                            "</script>";
                            ClientScript.RegisterStartupScript(this.GetType(), "redirect", script);
                        }
                        else
                        {
                            ShowError("Failed to verify agreement. Please try again.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Verify ERROR: {ex.Message}");
                    ShowError($"Error verifying agreement: {ex.Message}");
                }
            }
        }

        private void LoadVerificationData(int agreementId)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                        SELECT employee_name, employee_id, employee_position, employee_department,
                               it_verify_hardware_checklist, it_verify_system_config, it_verify_others,
                               it_verified_by, it_verified_date
                        FROM hardware_agreements WHERE id = @id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", agreementId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Employee info for display
                                txtVerifyEmpName.Text = SafeConvertToString(reader["employee_name"]);
                                txtVerifyEmpId.Text = SafeConvertToString(reader["employee_id"]);
                                txtVerifyEmpPosition.Text = SafeConvertToString(reader["employee_position"]);
                                txtVerifyEmpDepartment.Text = SafeConvertToString(reader["employee_department"]);

                                // Verification data (for Completed view)
                                chkVerifyHardware.Checked = SafeConvertToBool(reader["it_verify_hardware_checklist"]);
                                chkVerifySystemConfig.Checked = SafeConvertToBool(reader["it_verify_system_config"]);
                                txtVerifyOthers.Text = SafeConvertToString(reader["it_verify_others"]);

                                string verifiedBy = SafeConvertToString(reader["it_verified_by"]);
                                if (!string.IsNullOrEmpty(verifiedBy))
                                {
                                    txtVerifiedBy.Text = verifiedBy;
                                }

                                DateTime? verifiedDate = SafeConvertToDateTime(reader["it_verified_date"]);
                                if (verifiedDate.HasValue)
                                {
                                    txtVerifiedDate.Text = verifiedDate.Value.ToString("dd/MM/yyyy HH:mm");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadVerificationData ERROR: {ex.Message}");
                }
            }
        }

        private void SendVerificationCompleteEmail(int agreementId, string verifiedBy)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            string agreementNumber = "";
            string employeeEmail = "";
            string hodEmail = "";
            string itStaff = "";
            string employeeName = "";
            string employeePosition = "";
            string employeeDepartment = "";
            string modelName = "";
            string serialNumber = "";
            string assetNumber = "";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT a.agreement_number, a.employee_email, a.hod_email, a.it_staff_win_id,
                           COALESCE(a.employee_name, '') as employee_name,
                           COALESCE(a.employee_position, '') as employee_position,
                           COALESCE(a.employee_department, '') as employee_department,
                           a.serial_number, a.asset_number, m.model
                    FROM hardware_agreements a
                    LEFT JOIN hardware_model m ON a.model_id = m.id
                    WHERE a.id = @id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", agreementId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            agreementNumber = SafeConvertToString(reader["agreement_number"]);
                            employeeEmail = SafeConvertToString(reader["employee_email"]);
                            hodEmail = SafeConvertToString(reader["hod_email"]);
                            itStaff = SafeConvertToString(reader["it_staff_win_id"]);
                            employeeName = SafeConvertToString(reader["employee_name"]);
                            employeePosition = SafeConvertToString(reader["employee_position"]);
                            employeeDepartment = SafeConvertToString(reader["employee_department"]);
                            serialNumber = SafeConvertToString(reader["serial_number"]);
                            assetNumber = SafeConvertToString(reader["asset_number"]);
                            modelName = SafeConvertToString(reader["model"]);
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(employeeEmail))
            {
                System.Diagnostics.Debug.WriteLine("SendVerificationCompleteEmail - No employee email!");
                return;
            }

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("laptop_pc_agreement@pancentury.com", "Laptop/PC Agreement System");
            mail.To.Add(employeeEmail);

            if (!string.IsNullOrEmpty(hodEmail))
            {
                mail.CC.Add(hodEmail);
            }

            // CC all IT admins
            CcAllAdmins(mail);

            mail.Subject = $"Laptop/PC Agreement {agreementNumber} - Completed & Verified";
            mail.IsBodyHtml = true;

            string verifiedDateStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            mail.Body = $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #10b981; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #ddd; }}
        .details-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        .details-table th, .details-table td {{ padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }}
        .details-table th {{ background-color: #f2f2f2; width: 40%; }}
        .success-badge {{ background-color: #10b981; color: white; padding: 5px 15px; border-radius: 20px; font-weight: bold; }}
        .check-item {{ padding: 8px 0; }}
        .check-icon {{ color: #10b981; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>&#10004; Agreement Completed & Verified</h1>
        </div>
        <div class='content'>
            <p>Dear {employeeName},</p>
            <p>Your laptop/pc agreement has been <strong>verified by IT</strong> and is now fully completed.</p>
            
            <h3>Agreement Details</h3>
            <table class='details-table'>
                <tr><th>Agreement Number:</th><td>{agreementNumber}</td></tr>
                <tr><th>Status:</th><td><span class='success-badge'>Completed</span></td></tr>
                <tr><th>Employee Name:</th><td>{employeeName}</td></tr>
                <tr><th>Position:</th><td>{employeePosition}</td></tr>
                <tr><th>Department:</th><td>{employeeDepartment}</td></tr>
                <tr><th>Model:</th><td>{modelName}</td></tr>
                <tr><th>Serial Number:</th><td>{serialNumber}</td></tr>
                <tr><th>Asset Number:</th><td>{assetNumber}</td></tr>
                <tr><th>IT Staff:</th><td>{itStaff}</td></tr>
                <tr><th>Verified By:</th><td>{verifiedBy}</td></tr>
                <tr><th>Verification Date:</th><td>{verifiedDateStr}</td></tr>
            </table>
            
            <div style='background-color: #f0fdf4; padding: 15px; border-radius: 8px; border-left: 4px solid #10b981; margin: 20px 0;'>
                <h4 style='color: #15803d; margin-top: 0;'>IT Verification Summary</h4>
                <div class='check-item'><span class='check-icon'>&#10004;</span> Hardware Checklist - Updated</div>
                <div class='check-item'><span class='check-icon'>&#10004;</span> System Configuration</div>
            </div>
            
            <p>Please keep this email for your records. This agreement is now fully complete.</p>
            <p>If you have any questions, please contact the IT department.</p>
            
            <p style='margin-top: 30px; color: #666; font-size: 0.9em;'>
                This is an automated message from the Laptop/PC Agreement System.
            </p>
        </div>
    </div>
</body>
</html>";

            using (SmtpClient smtpClient = new SmtpClient())
            {
                smtpClient.Timeout = 30000;
                smtpClient.Send(mail);
                System.Diagnostics.Debug.WriteLine($"SendVerificationCompleteEmail - Email sent to {employeeEmail}");
            }
        }

        private string GetAgreementToken(int agreementId)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT agreement_view_token FROM hardware_agreements WHERE id = @id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", agreementId);
                    object result = command.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        return result.ToString();
                    }
                }
            }

            return null;
        }
        private string GenerateAccessToken()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 32);
        }
    }
}