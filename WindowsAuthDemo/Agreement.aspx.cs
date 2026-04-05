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
        protected bool isEmployeeMode = false;
        protected bool isEmployeeViewMode = false;
        protected bool isAdminUser = false;   // always set regardless of token/mode
        private string currentStatus = "";
        private bool accessoriesSectionVisible = false;
        private string accessToken = "";
        protected System.Web.UI.WebControls.Button btnExportPDF;
        protected System.Web.UI.WebControls.Button btnAdminSave;
        protected System.Web.UI.WebControls.Button btnAdminSaveNotify;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            // CSRF protection: tie ViewState to the authenticated user
            ViewStateUserKey = User.Identity.IsAuthenticated ? User.Identity.Name : Session.SessionID;
        }

        /// <summary>Adds security response headers to every page response.</summary>
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
                if (ViewState["IsEmployeeViewMode"] != null)
                {
                    isEmployeeViewMode = (bool)ViewState["IsEmployeeViewMode"];
                }

                // ADD THIS LINE to restore token
                if (ViewState["AccessToken"] != null)
                {
                    accessToken = (string)ViewState["AccessToken"];
                }

                System.Diagnostics.Trace.WriteLine($"PostBack - CurrentAgreementId: {currentAgreementId}, hdnAgreementId: {hdnAgreementId.Value}, Token: {accessToken}");

                // ADD DEBUG INFO
                System.Diagnostics.Trace.WriteLine($"PostBack Debug - IsEmployeeMode: {isEmployeeMode}");
                System.Diagnostics.Trace.WriteLine($"PostBack Debug - txtEmpName exists: {txtEmpName != null}");

                // Restore isAdminUser from Session on PostBack
                if (Session["IsAdmin"] != null)
                    isAdminUser = (bool)Session["IsAdmin"];

                return;
            }

            // Initial load (not PostBack)
            // Add this line to populate the sidebar user name
            lblUserName.Text = User.Identity.Name;
            lblTopUserName.Text = User.Identity.Name;
            lblTopUserRole.Text = isEmployeeMode ? "Employee" : (isEmployeeViewMode ? "Employee" : "Administrator");

            // Debug: Log current user info
            System.Diagnostics.Trace.WriteLine($"=== Page_Load (Initial) ===");
            System.Diagnostics.Trace.WriteLine($"Current Windows ID: {User.Identity.Name}");
            System.Diagnostics.Trace.WriteLine($"Token from URL: {accessToken}");

            // Check if employee mode (via token)
            // Check if employee mode (via token)
            if (isEmployeeMode)
            {
                // ── ADMIN SHORT-CIRCUIT ───────────────────────────────────────
                // IT admins are CC'd on every token email. If an admin opens a
                // token link, check whether they are the EMPLOYEE for this acknowledgement receipt.
                // • If YES → let them through as employee so they can sign.
                // • If NO  → redirect to the admin view URL (?id=X&mode=view).
                bool adminCheck = CheckUserIsAdminFromDB(User.Identity.Name);
                isAdminUser = adminCheck;
                Session["IsAdmin"] = adminCheck;

                if (adminCheck)
                {
                    // Resolve agreement ID from token
                    int? tokenAgreementId = GetAgreementIdByToken(accessToken);
                    if (tokenAgreementId.HasValue)
                    {
                        // Check if this admin is actually the employee on this acknowledgement receipt
                        if (IsCurrentUserTheEmployee(tokenAgreementId.Value))
                        {
                            // Admin IS the employee — let them continue in employee mode
                            System.Diagnostics.Trace.WriteLine(
                                $"Admin short-circuit BYPASSED: admin {User.Identity.Name} is the employee for acknowledgement {tokenAgreementId.Value}");
                            // Fall through to employee path below
                        }
                        else
                        {
                            // Admin is NOT the employee — redirect to admin view
                            Response.Redirect($"Agreement.aspx?id={tokenAgreementId.Value}&mode=view");
                            return;
                        }
                    }
                    else
                    {
                        // Token not found / expired — fall through to normal empty-form admin view
                        isEmployeeMode = false;
                        goto adminPath;
                    }
                }

                // Employee accessing via token
                System.Diagnostics.Trace.WriteLine($"=== EMPLOYEE MODE ===");
                System.Diagnostics.Trace.WriteLine($"Employee mode detected. Token: {accessToken}");
                System.Diagnostics.Trace.WriteLine($"Current Windows ID: {User.Identity.Name}");

                ValidateEmployeeAccess(accessToken);

                // IMPORTANT: Return here if validation failed
                if (!currentAgreementId.HasValue)
                {
                    System.Diagnostics.Trace.WriteLine($"Validation failed. currentAgreementId is null.");

                    // Try one more time to get from query string if available
                    if (Request.QueryString["id"] != null)
                    {
                        int agreementId;
                        if (int.TryParse(Request.QueryString["id"], out agreementId))
                        {
                            currentAgreementId = agreementId;
                            System.Diagnostics.Trace.WriteLine($"Got Acknowledgement Receipt from query string: {currentAgreementId}");
                        }
                    }

                    if (!currentAgreementId.HasValue)
                    {
                        ShowError("Unable to validate your access. Please use the link from your email.");
                        return;
                    }
                }

                System.Diagnostics.Trace.WriteLine($"Validation successful. Acknowledgement Receipt: {currentAgreementId.Value}");

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

                System.Diagnostics.Trace.WriteLine($"Stored Agreement ID: {currentAgreementId.Value}");
                System.Diagnostics.Trace.WriteLine($"hdnAgreementId.Value: {hdnAgreementId.Value}");
            }
        adminPath:
            if (!isEmployeeMode)
            {
                // Check admin status from DB on every load (not stale Session)
                bool isAdmin = CheckUserIsAdminFromDB(User.Identity.Name);
                isAdminUser = isAdmin;
                Session["IsAdmin"] = isAdmin;

                // Check mode from query string
                string mode = Request.QueryString["mode"];

                if (!isAdmin)
                {
                    // Non-admin: allow employees to view their own acknowledgement receipt (read-only)
                    if (mode == "empview" && Request.QueryString["id"] != null)
                    {
                        int agreementId;
                        if (int.TryParse(Request.QueryString["id"], out agreementId))
                        {
                            if (CanEmployeeViewAgreement(agreementId))
                            {
                                currentAgreementId = agreementId;
                                isEmployeeViewMode = true;
                                LoadAgreementStatus(agreementId);
                                ViewState["CurrentAgreementId"] = currentAgreementId;
                                ViewState["CurrentStatus"] = currentStatus;
                                ViewState["IsEmployeeViewMode"] = isEmployeeViewMode;
                            }
                            else
                            {
                                ShowError("Access denied. You can only view your own acknowledgement receipts.");
                                return;
                            }
                        }
                        else
                        {
                            Response.Redirect("Default.aspx");
                            return;
                        }
                    }
                    else
                    {
                        Response.Redirect("Default.aspx");
                        return;
                    }
                }
                else
                {
                    // Admin path — full access
                    isViewMode = (mode == "view");

                    if (Request.QueryString["id"] != null)
                    {
                        int agreementId;
                        if (int.TryParse(Request.QueryString["id"], out agreementId))
                        {
                            currentAgreementId = agreementId;

                            // Load the acknowledgement receipt to check its status
                            LoadAgreementStatus(agreementId);

                            // Set modes based on status and query string
                            if (isViewMode)
                            {
                                isEditMode = false;
                            }
                            else
                            {
                                // Allow edit mode for Draft (uses Save Draft / Submit flow)
                                // All other statuses use view mode with admin save (always notifies)
                                isEditMode = (currentStatus == "Draft");

                                if (!isEditMode)
                                {
                                    // Non-draft: redirect to view mode (admin save buttons shown there)
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

                            // Also store in hidden field (most reliable for PostBack)
                            hdnAgreementId.Value = currentAgreementId.Value.ToString();
                        }
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

            // Load departments from database
            LoadDepartments();

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
                            System.Diagnostics.Trace.WriteLine("Error checking device type: " + ex.Message);
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
                    System.Diagnostics.Trace.WriteLine("Error loading acknowledgement receipt status: " + ex.Message);
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
                txtEmpStaffId.Enabled = true;
                txtEmpStaffId.ReadOnly = false;
                txtEmpPosition.Enabled = true;
                txtEmpPosition.ReadOnly = false;
                ddlEmpDepartment.Enabled = true;
                txtEmpId.Enabled = true;
                txtEmpId.ReadOnly = true; // This should be readonly (auto-filled)
                txtEmpSignatureDate.Enabled = true;
                txtEmpSignatureDate.ReadOnly = true;

                // Remove any readonly CSS classes and aspNetDisabled
                txtEmpName.CssClass = "form-control";
                txtEmpStaffId.CssClass = "form-control";
                txtEmpPosition.CssClass = "form-control";
                ddlEmpDepartment.CssClass = "form-select";
                txtEmpId.CssClass = "form-control auto-fill";
                txtEmpSignatureDate.CssClass = "form-control auto-fill";

                // Remove any server-side disabled state
                txtEmpName.Attributes.Remove("disabled");
                txtEmpStaffId.Attributes.Remove("disabled");
                txtEmpPosition.Attributes.Remove("disabled");
                ddlEmpDepartment.Attributes.Remove("disabled");

                litPageTitle.Text = "Employee Acknowledgement - Laptop & Desktop Acknowledgement Receipt System";
                litHeaderTitle.Text = "Laptop & Desktop Acknowledgement Receipt - Employee Signature";
                litHeaderDescription.Text = "Please review and sign the laptop & desktop acknowledgement receipt";
                litBreadcrumbTitle.Text = "Employee Acknowledgement";

                // Hide admin action buttons, show employee button
                btnSaveDraft.Visible = false;
                btnSubmit.Visible = false;
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                btnSubmitEmployee.Visible = true;
                btnVerify.Visible = false;
                btnReject.Visible = false;
                btnAdminSave.Visible = false;
                btnAdminSaveNotify.Visible = false;
                btnExportPDF.Visible = false;

                // Show acknowledgement receipt info
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
            else if (isEmployeeViewMode)
            {
                // ── Employee Read-Only View of their own acknowledgement receipt ────────────
                SetFormReadOnly(true);
                litPageTitle.Text = "My Acknowledgement Receipt - Laptop & Desktop Acknowledgement Receipt System";
                litHeaderTitle.Text = "My Laptop & Desktop Acknowledgement Receipt";
                litHeaderDescription.Text = "Your acknowledgement receipt details (Read-only)";
                litBreadcrumbTitle.Text = "My Acknowledgement Receipt";

                // Hide all action buttons — employees just view
                btnSaveDraft.Visible = false;
                btnSubmit.Visible = false;
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                btnSubmitEmployee.Visible = false;
                btnVerify.Visible = false;
                btnReject.Visible = false;
                btnAdminSave.Visible = false;
                btnAdminSaveNotify.Visible = false;
                btnExportPDF.Visible = (currentStatus == "Completed");

                agreementInfo.Visible = true;
                lblCurrentStatus.Text = currentStatus;

                if (accessoriesSection != null) accessoriesSection.Visible = false;

                // Show employee signature section read-only
                if (currentStatus == "Agreed" || currentStatus == "Completed" || currentStatus == "Archived")
                {
                    phaseAgree.Visible = true;
                    pnlEmployeeSignature.Visible = true;
                    txtEmpName.ReadOnly = true; txtEmpName.Enabled = false; txtEmpName.CssClass = "form-control readonly-control";
                    txtEmpStaffId.ReadOnly = true; txtEmpStaffId.Enabled = false; txtEmpStaffId.CssClass = "form-control readonly-control";
                    txtEmpPosition.ReadOnly = true; txtEmpPosition.Enabled = false; txtEmpPosition.CssClass = "form-control readonly-control";
                    ddlEmpDepartment.Enabled = false; ddlEmpDepartment.CssClass = "form-select readonly-control";
                    txtEmpId.ReadOnly = true; txtEmpId.Enabled = false; txtEmpId.CssClass = "form-control readonly-control";
                    txtEmpSignatureDate.ReadOnly = true; txtEmpSignatureDate.Enabled = false; txtEmpSignatureDate.CssClass = "form-control readonly-control";
                    chkAgreeTerms.Enabled = false;
                    chkAgreeTerms.Checked = true;
                    if (currentAgreementId.HasValue) LoadEmployeeData(currentAgreementId.Value);
                }
                else
                {
                    phaseAgree.Visible = false;
                    pnlEmployeeSignature.Visible = false;
                }

                // Show IT verification section read-only for Completed/Archived
                if (currentStatus == "Completed" || currentStatus == "Archived")
                {
                    phaseVerify.Visible = true;
                    pnlITVerification.Visible = true;
                    chkVerifyHardware.Enabled = false;
                    chkVerifySystemConfig.Enabled = false;
                    txtVerifyOthers.ReadOnly = true;
                    txtVerifyOthers.CssClass += " readonly-control";
                    if (currentAgreementId.HasValue) LoadVerificationData(currentAgreementId.Value);
                }
                else
                {
                    phaseVerify.Visible = false;
                    pnlITVerification.Visible = false;
                }
            }
            else if (isViewMode)
            {
                // View Mode - Readonly
                SetFormReadOnly(true);
                litPageTitle.Text = "View Acknowledgement Receipt - Windows Auth Demo";
                litHeaderTitle.Text = "View Laptop & Desktop Acknowledgement Receipt";
                litHeaderDescription.Text = "View acknowledgement receipt details (Read-only)";

                // Hide action buttons
                btnSaveDraft.Visible = false;
                btnSubmit.Visible = false;
                btnEdit.Visible = (currentStatus == "Draft"); // Only show Edit if it's a draft
                btnDelete.Visible = (currentStatus != "Archived"); // Hide Archive on already-archived acknowledgement receipts
                btnSubmitEmployee.Visible = false;
                btnVerify.Visible = false;
                btnReject.Visible = false;
                btnAdminSave.Visible = false;
                btnAdminSaveNotify.Visible = false;

                // Show Export PDF button for non-draft statuses
                btnExportPDF.Visible = (currentStatus != "Draft");

                // For IT admin: unlock Phase 1 hardware fields and show Save button
                // Draft uses Edit mode (Save Draft / Submit), so admin Save not needed for Draft
                if (currentStatus != "Draft" && Session["IsAdmin"] != null && (bool)Session["IsAdmin"])
                {
                    SetFormReadOnly(false);  // Unlock Phase 1 hardware fields
                    btnAdminSaveNotify.Visible = true;
                    btnEdit.Visible = false;  // No Edit button for non-draft (admin Save replaces it)
                    btnDelete.Visible = (currentStatus == "Pending"); // Archive only for Pending

                    litHeaderTitle.Text = "Edit Acknowledgement Receipt";
                    litHeaderDescription.Text = "Edit acknowledgement receipt fields. Every save sends notification to relevant parties.";
                }

                // ── Phase 3 Verification visibility ─────────────────────────
                if (currentStatus == "Agreed")
                {
                    // Agreed: Phase 3 editable — IT admin can verify or reject
                    phaseVerify.Visible = true;
                    pnlITVerification.Visible = true;
                    btnVerify.Visible = true;
                    btnReject.Visible = true;
                    btnDelete.Visible = false;

                    litHeaderTitle.Text = "Verify Laptop & Desktop Acknowledgement Receipt";
                    litHeaderDescription.Text = "Review employee acknowledgement and complete IT verification";

                    // Auto-fill verified by
                    txtVerifiedBy.Text = User.Identity.Name;
                    txtVerifiedDate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                    // Load employee info into verification display
                    LoadVerificationData(currentAgreementId.Value);
                }
                else if (currentStatus == "Completed" || currentStatus == "Archived")
                {
                    // Completed/Archived: Phase 3 visible but readonly
                    phaseVerify.Visible = true;
                    pnlITVerification.Visible = true;
                    chkVerifyHardware.Enabled = false;
                    chkVerifySystemConfig.Enabled = false;
                    txtVerifyOthers.ReadOnly = true;
                    txtVerifyOthers.CssClass += " readonly-control";

                    LoadVerificationData(currentAgreementId.Value);
                }
                else
                {
                    // Draft/Pending: Phase 3 not applicable yet
                    phaseVerify.Visible = false;
                    pnlITVerification.Visible = false;
                }

                // Show acknowledgement receipt info
                agreementInfo.Visible = true;

                // Update status label
                lblCurrentStatus.Text = currentStatus;

                litBreadcrumbTitle.Text = (currentStatus == "Agreed") ? "Verify Acknowledgement Receipt" : "View Acknowledgement Receipt";

                // Show employee acknowledgement section for IT admin
                // when status is Pending, Agreed, Completed, or Archived
                if (currentStatus == "Pending" || currentStatus == "Agreed" || currentStatus == "Completed" || currentStatus == "Archived")
                {
                    phaseAgree.Visible = true;
                    pnlEmployeeSignature.Visible = true;

                    // Phase 2 employee fields are ALWAYS readonly for IT admins.
                    // Only the employee can fill these (via token link in Phase 2).
                    txtEmpName.ReadOnly = true; txtEmpName.Enabled = false; txtEmpName.CssClass = "form-control readonly-control";
                    txtEmpStaffId.ReadOnly = true; txtEmpStaffId.Enabled = false; txtEmpStaffId.CssClass = "form-control readonly-control";
                    txtEmpPosition.ReadOnly = true; txtEmpPosition.Enabled = false; txtEmpPosition.CssClass = "form-control readonly-control";
                    ddlEmpDepartment.Enabled = false; ddlEmpDepartment.CssClass = "form-select readonly-control";
                    txtEmpId.ReadOnly = true; txtEmpId.Enabled = false; txtEmpId.CssClass = "form-control readonly-control";
                    txtEmpSignatureDate.ReadOnly = true; txtEmpSignatureDate.Enabled = false; txtEmpSignatureDate.CssClass = "form-control readonly-control";
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
                        if (string.IsNullOrEmpty(txtEmpStaffId.Text))
                            txtEmpStaffId.Text = "(Pending employee signature)";
                        if (string.IsNullOrEmpty(txtEmpPosition.Text))
                            txtEmpPosition.Text = "(Pending employee signature)";
                        if (string.IsNullOrEmpty(ddlEmpDepartment.SelectedValue))
                        {
                            // Dropdown shows "-- Select Department --" which is sufficient
                        }
                        if (string.IsNullOrEmpty(txtEmpId.Text))
                            txtEmpId.Text = "(Pending employee signature)";
                        if (string.IsNullOrEmpty(txtEmpSignatureDate.Text))
                            txtEmpSignatureDate.Text = "(Pending employee signature)";
                    }

                    // For Agreed/Completed/Archived, show checkbox as checked (read-only)
                    if (currentStatus == "Agreed" || currentStatus == "Completed" || currentStatus == "Archived")
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
                    litPageTitle.Text = "View Acknowledgement Receipt - Windows Auth Demo";
                    litHeaderTitle.Text = "View Acknowledgement Receipt (Read-only)";
                    litHeaderDescription.Text = "This acknowledgement receipt cannot be edited as it's not in draft status.";

                    // Hide edit buttons
                    btnSaveDraft.Visible = false;
                    btnSubmit.Visible = false;
                    btnEdit.Visible = false;
                    btnDelete.Visible = true;
                    btnSubmitEmployee.Visible = false;
                    btnVerify.Visible = false;
                    btnReject.Visible = false;
                    btnExportPDF.Visible = false;
                    btnAdminSave.Visible = false;
                    btnAdminSaveNotify.Visible = false;
                }
                else
                {
                    // Valid edit mode for draft
                    SetFormReadOnly(false);
                    litPageTitle.Text = "Edit Acknowledgement Receipt - Windows Auth Demo";
                    litHeaderTitle.Text = "Edit Laptop & Desktop Acknowledgement Receipt";
                    litHeaderDescription.Text = "Edit draft acknowledgement receipt details";

                    btnSaveDraft.Text = "Update Draft";
                    btnSubmit.Text = "Update and Submit";
                    btnSaveDraft.Visible = true;
                    btnSubmit.Visible = true;
                    btnEdit.Visible = false;
                    btnDelete.Visible = true;
                    btnSubmitEmployee.Visible = false;
                    btnVerify.Visible = false;
                    btnReject.Visible = false;
                    btnExportPDF.Visible = false;
                    btnAdminSave.Visible = false;
                    btnAdminSaveNotify.Visible = false;
                }

                // Show acknowledgement receipt info
                agreementInfo.Visible = true;

                // Update status label
                lblCurrentStatus.Text = currentStatus;

                litBreadcrumbTitle.Text = "Edit Acknowledgement Receipt";

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
                litPageTitle.Text = "Create New Acknowledgement Receipt - Windows Auth Demo";
                litHeaderTitle.Text = "Create New Laptop & Desktop Acknowledgement Receipt";
                litHeaderDescription.Text = "Fill in the details for the new laptop & desktop acknowledgement receipt";

                btnSaveDraft.Text = "Save as Draft";
                btnSubmit.Text = "Submit Acknowledgement Receipt";
                btnSaveDraft.Visible = true;
                btnSubmit.Visible = true;
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                btnSubmitEmployee.Visible = false;
                btnVerify.Visible = false;
                btnReject.Visible = false;
                btnExportPDF.Visible = false;
                btnAdminSave.Visible = false;
                btnAdminSaveNotify.Visible = false;

                // Hide acknowledgement receipt info for new acknowledgement receipts
                agreementInfo.Visible = false;
                litBreadcrumbTitle.Text = "Create Acknowledgement Receipt";

                // Hide employee signature section for admin create
                pnlEmployeeSignature.Visible = false;
                phaseAgree.Visible = false;

                // Hide IT verification section for create
                pnlITVerification.Visible = false;
                phaseVerify.Visible = false;
            }

            // Show status section only in edit mode for non-draft acknowledgement receipts
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

                    ShowError("This acknowledgement receipt cannot be edited as it's not in draft status.");
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
                            // FIX: reuse existing open connection – no second SqlConnection
                            string status = GetAgreementStatus(agreementId, connection);
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
                    System.Diagnostics.Trace.WriteLine(
                        $"SaveAgreement error | Action={action} | {ex.GetType().Name}: {ex.Message}");
                    ShowError($"Error saving acknowledgement receipt: {ex.Message}");
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
                        int newModelId = Convert.ToInt32(result);
                        // Invalidate the model cache so the new model appears immediately
                        System.Web.HttpRuntime.Cache.Remove("HardwareModels_Cache");
                        return newModelId;
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
                        return SafeConvertToString(result);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("GetAgreementStatus error: " + ex.Message);
                    return ""; // Return empty on DB error; callers must handle
                }
            }
        }

        // FIX: Overload that reuses an existing open connection (avoids second connection in SaveAgreement)
        private string GetAgreementStatus(int agreementId, SqlConnection connection)
        {
            try
            {
                using (SqlCommand command = new SqlCommand(
                    "SELECT agreement_status FROM hardware_agreements WHERE id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", agreementId);
                    object result = command.ExecuteScalar();
                    return SafeConvertToString(result);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("GetAgreementStatus(conn) error: " + ex.Message);
                return "";
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
                    System.Diagnostics.Trace.WriteLine("Error getting current agreement status: " + ex.Message);
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
            TextBox txtOtherAccessoriesControl = (TextBox)FindControl("txtOtherAccessories");
            TextBox txtOtherModelControl = (TextBox)FindControl("txtOtherModel");

            if (txtRemarksControl != null) txtRemarksControl.ReadOnly = readOnly;
            if (txtSerialNumberControl != null) txtSerialNumberControl.ReadOnly = readOnly;
            if (txtOtherAccessoriesControl != null) txtOtherAccessoriesControl.ReadOnly = readOnly;
            if (txtOtherModelControl != null) txtOtherModelControl.ReadOnly = readOnly;
            txtAssetNumber.ReadOnly = readOnly;

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
                if (txtOtherAccessoriesControl != null) txtOtherAccessoriesControl.CssClass += " readonly-control";
                if (txtRemarksControl != null) txtRemarksControl.CssClass += " readonly-control";
                if (txtOtherModelControl != null) txtOtherModelControl.CssClass += " readonly-control";
                txtAssetNumber.CssClass += " readonly-control";
                ddlModel.CssClass += " readonly-control";
                ddlDeviceType.CssClass += " readonly-control";
                txtEmployeeEmailSearch.CssClass += " readonly-control";
                txtHODEmailSearch.CssClass += " readonly-control";
            }
            else
            {
                // Remove readonly classes
                if (txtSerialNumberControl != null) txtSerialNumberControl.CssClass = txtSerialNumberControl.CssClass.Replace("readonly-control", "").Trim();
                if (txtOtherAccessoriesControl != null) txtOtherAccessoriesControl.CssClass = txtOtherAccessoriesControl.CssClass.Replace("readonly-control", "").Trim();
                if (txtRemarksControl != null) txtRemarksControl.CssClass = txtRemarksControl.CssClass.Replace("readonly-control", "").Trim();
                if (txtOtherModelControl != null) txtOtherModelControl.CssClass = txtOtherModelControl.CssClass.Replace("readonly-control", "").Trim();
                txtAssetNumber.CssClass = txtAssetNumber.CssClass.Replace("readonly-control", "").Trim();
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
                    ShowError("Error loading acknowledgement receipt: " + ex.Message);
                }
            }
        }

        // FIX: Cache hardware model dropdown for 30 min – avoids DB hit on every PostBack
        private void LoadHardwareModels()
        {
            ddlModel.Items.Clear();

            const string cacheKey = "HardwareModels_Cache";
            System.Data.DataTable dt = System.Web.HttpRuntime.Cache[cacheKey] as System.Data.DataTable;

            if (dt == null)
            {
                dt = new System.Data.DataTable();
                string connectionString = System.Configuration.ConfigurationManager
                    .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        using (SqlCommand command = new SqlCommand(
                            "SELECT id, model, type FROM hardware_model ORDER BY model", connection))
                        using (System.Data.SqlClient.SqlDataAdapter adapter =
                            new System.Data.SqlClient.SqlDataAdapter(command))
                            adapter.Fill(dt);

                        System.Web.HttpRuntime.Cache.Insert(
                            cacheKey, dt, null,
                            DateTime.Now.AddMinutes(30),
                            System.Web.Caching.Cache.NoSlidingExpiration);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine("LoadHardwareModels error: " + ex.Message);
                        ddlModel.Items.Add(new ListItem("-- Error loading models --", ""));
                        return;
                    }
                }
            }

            ddlModel.Items.Add(new ListItem("-- Select Model --", ""));
            foreach (System.Data.DataRow row in dt.Rows)
            {
                ddlModel.Items.Add(new ListItem(
                    $"{row["model"]} ({row["type"]})",
                    row["id"].ToString()));
            }
            ddlModel.Items.Add(new ListItem("-- Other (Add New) --", "OTHER"));
        }

        // Load department dropdown from hardware_department table (cached 30 min)
        private void LoadDepartments()
        {
            ddlEmpDepartment.Items.Clear();

            const string cacheKey = "Departments_Cache";
            DataTable dt = System.Web.HttpRuntime.Cache[cacheKey] as DataTable;

            if (dt == null)
            {
                dt = new DataTable();
                string cs = System.Configuration.ConfigurationManager
                    .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    try
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT id, department FROM hardware_department WHERE active = 1 ORDER BY department", conn))
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            da.Fill(dt);

                        System.Web.HttpRuntime.Cache.Insert(cacheKey, dt, null,
                            DateTime.Now.AddMinutes(30),
                            System.Web.Caching.Cache.NoSlidingExpiration);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine("LoadDepartments error: " + ex.Message);
                        ddlEmpDepartment.Items.Add(new ListItem("-- Error loading departments --", ""));
                        return;
                    }
                }
            }

            ddlEmpDepartment.Items.Add(new ListItem("-- Select Department --", ""));
            foreach (DataRow row in dt.Rows)
            {
                ddlEmpDepartment.Items.Add(new ListItem(
                    row["department"].ToString(),
                    row["department"].ToString()));
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

        // Create new acknowledgement receipt with modelId parameter
        // Create new acknowledgement receipt with modelId parameter
        private void CreateNewAgreement(SqlConnection connection, string action, int modelId)
        {
            // Generate acknowledgement receipt number
            string agreementNumber = GenerateAgreementNumber();

            // Generate access token for employee link
            string accessToken = null;
            DateTime? tokenExpiryDate = null;

            if (action == "Submitted")
            {
                accessToken = GenerateSecureToken();
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
            // asset_number is optional in Phase 1 — saved if provided, mandatory only in Phase 3 (btnVerify_Click)

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                // Use the provided modelId parameter
                command.Parameters.AddWithValue("@agreementNumber", agreementNumber);
                command.Parameters.AddWithValue("@modelId", modelId);
                command.Parameters.AddWithValue("@serialNumber", txtSerialNumber.Text.Trim());
                command.Parameters.AddWithValue("@assetNumber", string.IsNullOrEmpty(txtAssetNumber.Text) ? (object)DBNull.Value : txtAssetNumber.Text.Trim());

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
                            // Fallback: get by acknowledgement receipt number
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
                                ShowSuccess($"Acknowledgement receipt submitted successfully! Receipt Number: {agreementNumber}. Email sent to {hdnEmployeeEmail.Value} and {hdnHODEmail.Value}");
                            }
                            else
                            {
                                ShowSuccess($"Acknowledgement receipt submitted successfully! Receipt Number: {agreementNumber}. Status: {finalStatus}. Note: Email notification failed.");
                            }
                        }
                        else
                        {
                            ShowSuccess($"Draft saved successfully! Receipt Number: {agreementNumber}");
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
                        ShowSuccess($"Acknowledgement receipt saved successfully! Acknowledgement Number: {agreementNumber}");

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
                    ShowError("Failed to save acknowledgement receipt. Please try again.");
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
            // Get acknowledgement receipt number from database first
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

            // FIX: Generate signing token when Draft → Pending (first submission).
            // Previously only CreateNewAgreement generated a token; UpdateAgreement
            // left agreement_view_token as NULL, causing "link could not be generated".
            string newToken = null;
            DateTime? tokenExpiry = null;

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
                    newToken = GenerateSecureToken();
                    tokenExpiry = DateTime.Now.AddDays(7);
                }
                else if (currentDbStatus == "Pending")
                {
                    // FIX (MED): Was setting finalStatus to "Active" or "Inactive" based on
                    // a radio button (rbActive) — neither of those are valid workflow statuses.
                    // An agreement re-saved while Pending must remain Pending.
                    submittedDate = DateTime.Now;
                    issueDate = submittedDate.Value;
                    finalStatus = "Pending";
                    newToken = GenerateSecureToken();
                    tokenExpiry = DateTime.Now.AddDays(7);
                }
                else
                {
                    // FIX (MED): Dead branch also produced "Active"/"Inactive".
                    // For any other saveable status, preserve the current DB status unchanged.
                    submittedDate = DateTime.Now;
                    issueDate = submittedDate.Value;
                    finalStatus = currentDbStatus;
                }
            }

            // Include token columns in UPDATE when a new token was generated
            string tokenClause = (newToken != null)
                ? ", agreement_view_token = @newToken, token_expiry_date = @tokenExpiry"
                : "";

            string query = $@"
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
        {tokenClause}
        WHERE id = @id";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                // Use the provided modelId parameter
                command.Parameters.AddWithValue("@modelId", modelId);
                command.Parameters.AddWithValue("@serialNumber", txtSerialNumber.Text.Trim());
                command.Parameters.AddWithValue("@assetNumber", string.IsNullOrEmpty(txtAssetNumber.Text) ? (object)DBNull.Value : txtAssetNumber.Text.Trim());

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

                // Token parameters (only present when tokenClause is non-empty)
                if (newToken != null)
                {
                    command.Parameters.AddWithValue("@newToken", newToken);
                    command.Parameters.AddWithValue("@tokenExpiry", (object)tokenExpiry.Value);
                }

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
                            ShowSuccess($"Acknowledgement receipt updated successfully!");
                        }
                        else
                        {
                            ShowSuccess($"Acknowledgement receipt updated successfully! Status: {finalStatus}.");
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
                    ShowError("Failed to update acknowledgement receipt. Please try again.");
                }
            }
        }

        private string GenerateAgreementNumber()
        {
            // Collision-resistant: AGMT-YYYYMMDD-<6 hex chars from CSPRNG>
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            byte[] buf = new byte[3];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                rng.GetBytes(buf);
            string uniquePart = BitConverter.ToString(buf).Replace("-", "").ToUpper();
            return $"AGMT-{datePart}-{uniquePart}";
        }

        protected void btnSaveDraft_Click(object sender, EventArgs e)
        {
            SaveAgreement("Draft");
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            SaveAgreement("Submitted");
        }

        // ── Server-side validation for email CustomValidators ─────────────────
        // These run on PostBack to enforce that the user picked an email from
        // the searchable dropdown (i.e. hdnEmployeeEmail / hdnHODEmail is set),
        // not just typed text into the visible search box.
        protected void rfvEmployeeEmail_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = !string.IsNullOrEmpty(hdnEmployeeEmail.Value);
        }

        protected void rfvHODEmail_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = !string.IsNullOrEmpty(hdnHODEmail.Value);
        }
        // ─────────────────────────────────────────────────────────────────────

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
                ShowError("Only draft acknowledgement receipts can be edited.");
            }
        }

        protected void btnExportPDF_Click(object sender, EventArgs e)
        {
            if (currentAgreementId.HasValue)
            {
                Response.Redirect($"ExportPDF.ashx?id={currentAgreementId.Value}");
            }
        }

        protected void btnAdminSave_Click(object sender, EventArgs e)
        {
            // All saves now send notification
            AdminSaveAgreement(sendNotification: true);
        }

        protected void btnAdminSaveNotify_Click(object sender, EventArgs e)
        {
            AdminSaveAgreement(sendNotification: true);
        }

        /// <summary>
        /// Shared logic for admin Save button.
        /// Saves all acknowledgement receipt fields (all statuses including Archived).
        /// Logs the action to agreement_audit_log.
        /// Always sends notification email to relevant parties.
        /// </summary>
        private void AdminSaveAgreement(bool sendNotification)
        {
            if (!currentAgreementId.HasValue)
            {
                ShowError("No acknowledgement receipt selected.");
                return;
            }

            string cs = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(cs))
            {
                try
                {
                    conn.Open();

                    // ── Resolve model ID ───────────────────────────────────────
                    int modelId = 0;
                    string selectedModel = ddlModel.SelectedValue;
                    if (selectedModel == "OTHER")
                    {
                        string modelName = txtOtherModel.Text.Trim();
                        string modelType = ddlDeviceType.SelectedValue;
                        if (string.IsNullOrEmpty(modelName)) { ShowError("Please enter a model name for the 'Other' option."); return; }
                        if (string.IsNullOrEmpty(modelType)) { ShowError("Please select a device type for the new model."); return; }

                        using (SqlCommand chk = new SqlCommand("SELECT id FROM hardware_model WHERE model = @model", conn))
                        {
                            chk.Parameters.AddWithValue("@model", modelName);
                            object eid = chk.ExecuteScalar();
                            if (eid != null && eid != DBNull.Value) modelId = Convert.ToInt32(eid);
                        }
                        if (modelId == 0)
                        {
                            string insQ = "INSERT INTO hardware_model (model, type, created_date) VALUES (@m, @t, GETDATE()); SELECT SCOPE_IDENTITY();";
                            using (SqlCommand ins = new SqlCommand(insQ, conn))
                            {
                                ins.Parameters.AddWithValue("@m", modelName);
                                ins.Parameters.AddWithValue("@t", modelType);
                                object r = ins.ExecuteScalar();
                                if (r == null || r == DBNull.Value) { ShowError("Failed to add new model. Please try again."); return; }
                                modelId = Convert.ToInt32(r);
                                System.Web.HttpRuntime.Cache.Remove("HardwareModels_Cache");
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(selectedModel))
                    {
                        modelId = Convert.ToInt32(selectedModel);
                    }

                    // Get current DB status (for token logic and audit log)
                    string dbStatus = GetAgreementStatus(currentAgreementId.Value, conn);

                    // ── Mouse type ─────────────────────────────────────────────
                    string mouseType = "";
                    if (chkMouse.Checked)
                        mouseType = rbWired.Checked ? "Wired" : rbWireless.Checked ? "Wireless" : "";

                    TextBox txtOtherAcc = (TextBox)FindControl("txtOtherAccessories");
                    TextBox txtRem = (TextBox)FindControl("txtRemarks");
                    string otherAcc = txtOtherAcc != null ? txtOtherAcc.Text.Trim() : "";
                    string remarks = txtRem != null ? txtRem.Text.Trim() : "";

                    // ── For Pending: regenerate signing token (invalidates old link) ──
                    string newToken = null;
                    DateTime? tokenExpiry = null;
                    if (dbStatus == "Pending")
                    {
                        newToken = GenerateSecureToken();
                        tokenExpiry = DateTime.Now.AddDays(7);
                    }

                    string tokenClause = (dbStatus == "Pending")
                        ? ", agreement_view_token = @newToken, token_expiry_date = @tokenExpiry, submitted_date = GETDATE()"
                        : "";

                    // ── UPDATE all fields for any non-Archived agreement ────────
                    string updateQ = $@"
                        UPDATE hardware_agreements SET
                            model_id          = @modelId,
                            serial_number     = @serialNumber,
                            asset_number      = @assetNumber,
                            has_carry_bag     = @hasCarryBag,
                            has_power_adapter = @hasPowerAdapter,
                            has_mouse         = @hasMouse,
                            mouse_type        = @mouseType,
                            has_vga_converter = @hasVGAConverter,
                            other_accessories = @otherAcc,
                            it_staff_win_id   = @itStaff,
                            issue_date        = @issueDate,
                            remarks           = @remarks,
                            employee_email    = @employeeEmail,
                            hod_email         = @hodEmail,
                            employee_name     = @empName,
                            employee_id       = @empId,
                            employee_staff_id = @empStaffId,
                            employee_position = @empPosition,
                            employee_department = @empDept,
                            last_updated      = GETDATE()
                            {tokenClause}
                        WHERE id = @id";

                    int rows;
                    using (SqlCommand cmd = new SqlCommand(updateQ, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", currentAgreementId.Value);
                        cmd.Parameters.AddWithValue("@modelId", modelId);
                        cmd.Parameters.AddWithValue("@serialNumber", txtSerialNumber.Text.Trim());
                        cmd.Parameters.AddWithValue("@assetNumber", string.IsNullOrEmpty(txtAssetNumber.Text) ? (object)DBNull.Value : txtAssetNumber.Text.Trim());
                        cmd.Parameters.AddWithValue("@hasCarryBag", chkCarryBag.Checked ? 1 : 0);
                        cmd.Parameters.AddWithValue("@hasPowerAdapter", chkPowerAdapter.Checked ? 1 : 0);
                        cmd.Parameters.AddWithValue("@hasMouse", chkMouse.Checked ? 1 : 0);
                        cmd.Parameters.AddWithValue("@mouseType", string.IsNullOrEmpty(mouseType) ? (object)"" : mouseType);
                        cmd.Parameters.AddWithValue("@hasVGAConverter", chkVGAConverter.Checked ? 1 : 0);
                        cmd.Parameters.AddWithValue("@otherAcc", otherAcc);
                        cmd.Parameters.AddWithValue("@itStaff", txtITStaff.Text.Trim());
                        cmd.Parameters.AddWithValue("@issueDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@remarks", remarks);
                        cmd.Parameters.AddWithValue("@employeeEmail", string.IsNullOrEmpty(hdnEmployeeEmail.Value) ? (object)DBNull.Value : hdnEmployeeEmail.Value);
                        cmd.Parameters.AddWithValue("@hodEmail", string.IsNullOrEmpty(hdnHODEmail.Value) ? (object)DBNull.Value : hdnHODEmail.Value);
                        cmd.Parameters.AddWithValue("@empName", string.IsNullOrEmpty(txtEmpName.Text) ? (object)DBNull.Value : txtEmpName.Text.Trim());
                        cmd.Parameters.AddWithValue("@empId", string.IsNullOrEmpty(txtEmpId.Text) ? (object)DBNull.Value : txtEmpId.Text.Trim());
                        cmd.Parameters.AddWithValue("@empStaffId", string.IsNullOrEmpty(txtEmpStaffId.Text) ? (object)DBNull.Value : txtEmpStaffId.Text.Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@empPosition", string.IsNullOrEmpty(txtEmpPosition.Text) ? (object)DBNull.Value : txtEmpPosition.Text.Trim());
                        cmd.Parameters.AddWithValue("@empDept", string.IsNullOrEmpty(ddlEmpDepartment.SelectedValue) ? (object)DBNull.Value : ddlEmpDepartment.SelectedValue.Trim());
                        if (dbStatus == "Pending")
                        {
                            cmd.Parameters.AddWithValue("@newToken", newToken);
                            cmd.Parameters.AddWithValue("@tokenExpiry", tokenExpiry.Value);
                        }
                        rows = cmd.ExecuteNonQuery();
                    }

                    if (rows == 0)
                    {
                        ShowError("Acknowledgement receipt could not be saved. It may be archived or no longer exists.");
                        return;
                    }

                    // ── Audit log ──────────────────────────────────────────────
                    string actionCode = sendNotification ? "ADMIN_SAVE_NOTIFY" : "ADMIN_SAVE";
                    try
                    {
                        using (SqlCommand auditCmd = new SqlCommand(@"
                            INSERT INTO agreement_audit_log
                                (agreement_id, old_status, new_status, changed_by, source_action)
                            VALUES (@id, @status, @status, @by, @action)", conn))
                        {
                            auditCmd.Parameters.AddWithValue("@id", currentAgreementId.Value);
                            auditCmd.Parameters.AddWithValue("@status", dbStatus);
                            auditCmd.Parameters.AddWithValue("@by", User.Identity.Name);
                            auditCmd.Parameters.AddWithValue("@action", actionCode);
                            auditCmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception auditEx)
                    {
                        System.Diagnostics.Trace.WriteLine("AdminSave audit log failed: " + auditEx.Message);
                        // audit failure must not block the save
                    }

                    // ── Send notification if requested ─────────────────────────
                    if (sendNotification)
                    {
                        try
                        {
                            SendAdminSaveNotifyEmail(currentAgreementId.Value, dbStatus, newToken);
                            ShowSuccess("Acknowledgement receipt saved! Notification email sent to employee (CC: HOD & IT admins).");
                        }
                        catch (Exception emailEx)
                        {
                            System.Diagnostics.Trace.WriteLine("SendAdminSaveNotifyEmail failed: " + emailEx.Message);
                            ShowSuccess("Acknowledgement receipt saved! Warning: notification email could not be sent.");
                        }
                    }
                    else
                    {
                        ShowSuccess("Acknowledgement receipt saved successfully.");
                    }

                    string script = "<script type='text/javascript'>setTimeout(function(){ window.location.href = 'ExistingAgreements.aspx'; }, 2000);</script>";
                    ClientScript.RegisterStartupScript(GetType(), "redirect", script);
                }
                catch (Exception ex)
                {
                    ShowError("Error saving acknowledgement receipt: " + ex.Message);
                    System.Diagnostics.Trace.WriteLine("AdminSaveAgreement error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Sends re-notification email after IT admin updates a Pending acknowledgement receipt.
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

            string tokenSection = $@"<div style='background-color:#f0f9ff;padding:20px;border-radius:8px;margin:20px 0;border-left:4px solid #3b82f6'>
                    <h3 style='color:#1e40af;margin-top:0'>Action Required</h3>
                    <p>The IT department has updated the details of your laptop & desktop acknowledgement receipt. Please review and sign using the new link below:</p>
                    <p style='margin:15px 0'>
                        <a href='{tokenUrl}' style='display:inline-block;background-color:#3b82f6;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;font-weight:bold'>
                            Sign Acknowledgement Receipt Now
                        </a>
                    </p>
                    <p style='font-size:0.9rem;color:#4b5563'><strong>Note:</strong> This link expires in 7 days. Any previous signing links are no longer valid.</p>
                </div>";

            string sharedStyle = @"<style>body{font-family:Arial,sans-serif;line-height:1.6;color:#333}
                .container{max-width:600px;margin:0 auto;padding:20px}
                .header{background-color:#667eea;color:white;padding:20px;text-align:center;border-radius:10px 10px 0 0}
                .content{background-color:#f9f9f9;padding:20px;border-radius:0 0 10px 10px;border:1px solid #ddd}
                table{width:100%;border-collapse:collapse;margin:20px 0}th,td{padding:10px;text-align:left;border-bottom:1px solid #ddd}th{background-color:#f2f2f2}</style>";

            string detailsTable = $@"<table>
                <tr><th>Acknowledgement Receipt Number:</th><td>{agreementNumber}</td></tr>
                <tr><th>Status:</th><td>Pending</td></tr>
                <tr><th>Model:</th><td>{modelName}</td></tr>
                <tr><th>Serial Number:</th><td>{serialNumber}</td></tr>
                <tr><th>Asset Number:</th><td>{assetNumber}</td></tr>
                <tr><th>IT Staff:</th><td>{itStaff}</td></tr>
                <tr><th>Issue Date:</th><td>{DateTime.Now:dd/MM/yyyy}</td></tr>
            </table>";

            // ── Single email: employee To, IT admins CC ─────────────
            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
            mail.From = new System.Net.Mail.MailAddress(System.Configuration.ConfigurationManager.AppSettings["SmtpSenderAddress"] ?? "laptop_desktop_acknowledgement@pancentury.com", System.Configuration.ConfigurationManager.AppSettings["SmtpSenderDisplayName"] ?? "Laptop & Desktop Acknowledgement Receipt System");
            mail.To.Add(employeeEmail);
            CcAllAdmins(mail);
            mail.Subject = $"[Phase 1 Updated → 2] Laptop & Desktop Acknowledgement Receipt {agreementNumber} — Please Re-sign";
            mail.IsBodyHtml = true;
            mail.Body = $@"<!DOCTYPE html><html><head>{sharedStyle}</head><body>
                <div class='container'>
                    <div class='header'><p style='margin:0 0 4px;font-size:.78rem;opacity:.75;letter-spacing:.07em;text-transform:uppercase;'>Phase 1 — Updated</p><h1 style='margin:0;'>Acknowledgement Receipt Updated — Re-sign Required</h1><p style='margin:6px 0 0;opacity:.9;font-size:.9rem;'>IT department has updated the acknowledgement receipt details</p></div>
                    <div class='content'>
                        <p>This acknowledgement receipt has been updated and the employee is required to sign it again.</p>
                        <h2>Acknowledgement Receipt Details</h2>{detailsTable}{tokenSection}
                    </div>
                </div></body></html>";

            using (System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient())
            { smtp.Timeout = 10000; smtp.Send(mail); }
        }

        protected void btnDelete_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("Archive button clicked");

            if (!currentAgreementId.HasValue)
            {
                ShowError("No acknowledgement receipt selected for archiving.");
                return;
            }

            // FEEDBACK FIX: Require archive remarks before allowing archive
            string archiveRemarks = hdnArchiveRemarks.Value?.Trim();
            if (string.IsNullOrEmpty(archiveRemarks))
            {
                ShowError("Please provide a reason for archiving. Fill in the Archive Remarks field before proceeding.");
                return;
            }

            ArchiveAgreement(archiveRemarks);
        }

        private void ArchiveAgreement(string archiveRemarks)
        {
            if (!currentAgreementId.HasValue)
            {
                ShowError("No acknowledgement receipt to archive.");
                return;
            }

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    // FIX (LOW): Added AND agreement_status != 'Archived' so re-archiving an
                    // already-archived agreement fails gracefully (rowsAffected = 0) rather
                    // than silently overwriting the existing archive_remarks.
                    string query = "UPDATE hardware_agreements SET agreement_status = 'Archived', archive_remarks = @archiveRemarks, last_updated = GETDATE() WHERE id = @id AND agreement_status != 'Archived'";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", currentAgreementId.Value);
                        command.Parameters.AddWithValue("@archiveRemarks", archiveRemarks);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            ShowSuccess("Acknowledgement receipt archived successfully!");

                            // Redirect to ExistingAgreements.aspx after 2 seconds
                            string script = "<script type='text/javascript'>" +
                                            "setTimeout(function(){ window.location.href = 'ExistingAgreements.aspx'; }, 2000);" +
                                            "</script>";
                            ClientScript.RegisterStartupScript(this.GetType(), "redirect", script);
                        }
                        else
                        {
                            ShowError("Acknowledgement receipt could not be archived. It may already be archived.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Error archiving acknowledgement receipt: " + ex.Message);
                    System.Diagnostics.Trace.WriteLine("Archive error: " + ex.Message);
                }
            }
        }

        private void ShowSuccess(string message)
        {
            if (successText != null) successText.InnerText = message;
            messageSuccess.Visible = true;
            messageError.Visible = false;
            // Also fire a toast popup
            string safe = message.Replace("\\", "\\\\").Replace("'", "\\'")
                                 .Replace("\r\n", " ").Replace("\n", " ");
            ClientScript.RegisterStartupScript(GetType(),
                "toast_ok_" + DateTime.Now.Ticks,
                $"if(typeof showToast==='function')showToast('success','Done','{safe}');", true);
        }

        private void ShowError(string message)
        {
            if (errorText != null) errorText.InnerText = message;
            messageError.Visible = true;
            messageSuccess.Visible = false;
            string safe = message.Replace("\\", "\\\\").Replace("'", "\\'")
                                 .Replace("\r\n", " ").Replace("\n", " ");
            ClientScript.RegisterStartupScript(GetType(),
                "toast_err_" + DateTime.Now.Ticks,
                $"if(typeof showToast==='function')showToast('error','Error','{safe}');", true);
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
                    System.Diagnostics.Trace.WriteLine($"ValidateEmployeeAccess - Token: {token}");
                    System.Diagnostics.Trace.WriteLine($"ValidateEmployeeAccess - Current User: {currentWinId}");

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
                       a.employee_name, a.employee_id, a.employee_staff_id, a.employee_position, a.employee_department
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
                        System.Diagnostics.Trace.WriteLine($"ValidateEmployeeAccess - Found Acknowledgement Receipt: {agreementId}");
                        System.Diagnostics.Trace.WriteLine($"ValidateEmployeeAccess - Status: {status}");

                        // Token mismatch check
                        if (storedToken != token)
                        {
                            ShowError("Token mismatch. Please use the link provided in your email.");
                            currentAgreementId = null;
                            pnlEmployeeSignature.Visible = false;
                            phaseAgree.Visible = false;
                            return;
                        }

                        // ── IDENTITY CHECK ────────────────────────────────────────────
                        // Only the designated employee may sign. Look up the current
                        // Windows user's email from hardware_users and compare it to
                        // the employee_email stored on the acknowledgement receipt.
                        // This prevents the HOD (who receives a CC) or anyone else who
                        // forwards / shares the link from signing on behalf of the employee.
                        if (!string.IsNullOrEmpty(employeeEmail))
                        {
                            string currentUserEmail = "";
                            using (SqlCommand emailCmd = new SqlCommand(
                                "SELECT email FROM hardware_users WHERE win_id = @win_id AND active = 1", connection))
                            {
                                emailCmd.Parameters.AddWithValue("@win_id", currentWinId);
                                object emailResult = emailCmd.ExecuteScalar();
                                currentUserEmail = emailResult != null && emailResult != DBNull.Value
                                    ? emailResult.ToString().Trim().ToLowerInvariant()
                                    : "";
                            }

                            // FIX (HIGH): If the employee has never logged into the portal before,
                            // they won't be in hardware_users yet. Auto-register them (admin=0, active=1)
                            // so the identity check can succeed and they can sign their acknowledgement receipt.
                            if (string.IsNullOrEmpty(currentUserEmail))
                            {
                                try
                                {
                                    using (SqlCommand regCmd = new SqlCommand(
                                        "INSERT INTO hardware_users (win_id, email, active, admin) VALUES (@w, @e, 1, 0)", connection))
                                    {
                                        regCmd.Parameters.AddWithValue("@w", currentWinId);
                                        regCmd.Parameters.AddWithValue("@e", employeeEmail.Trim());
                                        regCmd.ExecuteNonQuery();
                                    }
                                    currentUserEmail = employeeEmail.Trim().ToLowerInvariant();
                                }
                                catch (Exception regEx)
                                {
                                    System.Diagnostics.Trace.WriteLine("Employee auto-register failed: " + regEx.Message);
                                }
                            }

                            if (string.IsNullOrEmpty(currentUserEmail) ||
                                !currentUserEmail.Equals(employeeEmail.Trim().ToLowerInvariant(),
                                                         StringComparison.OrdinalIgnoreCase))
                            {
                                ShowError("Access denied. This acknowledgement receipt link is intended for the designated employee only. " +
                                          "Please contact IT support if you believe this is an error.");
                                currentAgreementId = null;
                                pnlEmployeeSignature.Visible = false;
                                phaseAgree.Visible = false;
                                return;
                            }
                        }
                        // ── END IDENTITY CHECK ────────────────────────────────────────

                        // Set agreement ID
                        currentAgreementId = agreementId;
                        currentStatus = status;

                        // FIX (HIGH): Was checking !string.IsNullOrEmpty(existingName) which caused
                        // a false-positive "already signed" block when employee_name was pre-filled
                        // by the IT admin at draft time. Only treat as already-signed when the
                        // workflow status proves the employee has actually acted.
                        if (currentStatus == "Completed" || currentStatus == "Agreed")
                        {
                            ShowError("This acknowledgement receipt has already been signed.");
                            DisableEmployeeForm();
                            pnlEmployeeSignature.Visible = true;
                            return;
                        }

                        System.Diagnostics.Trace.WriteLine($"ValidateEmployeeAccess - SUCCESS! Acknowledgement Receipt set to: {currentAgreementId}");
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

                                    System.Diagnostics.Trace.WriteLine($"ValidateEmployeeAccess - Debug: Status={debugStatus}, Expiry={expiryDate}");

                                    if (!string.IsNullOrEmpty(debugName) || !string.IsNullOrEmpty(debugEmployeeId))
                                    {
                                        ShowError("This acknowledgement receipt has already been completed and signed.");
                                    }
                                    else if (expiryDate.HasValue && expiryDate.Value < DateTime.Now)
                                    {
                                        ShowError("This access link has expired. Please contact IT support for a new link.");
                                    }
                                    else if (debugStatus == "Draft")
                                    {
                                        ShowError("This link is no longer valid. Please contact IT support.");
                                    }
                                    else if (debugStatus != "Pending" && debugStatus != "Active")
                                    {
                                        ShowError("This link is no longer valid. Please contact IT support.");
                                    }
                                    else
                                    {
                                        ShowError("Unable to access this acknowledgement receipt. Please contact IT support.");
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
                    System.Diagnostics.Trace.WriteLine($"ValidateEmployeeAccess - ERROR: {ex.Message}");
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
                SELECT employee_name, employee_id, employee_staff_id, employee_position, employee_department, employee_agreed_date
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

                                string empStaffId = SafeConvertToString(reader["employee_staff_id"]);
                                if (!string.IsNullOrEmpty(empStaffId))
                                    txtEmpStaffId.Text = empStaffId;

                                string empPosition = SafeConvertToString(reader["employee_position"]);
                                if (!string.IsNullOrEmpty(empPosition))
                                    txtEmpPosition.Text = empPosition;

                                string empDepartment = SafeConvertToString(reader["employee_department"]);
                                if (!string.IsNullOrEmpty(empDepartment))
                                {
                                    ListItem deptItem = ddlEmpDepartment.Items.FindByValue(empDepartment);
                                    if (deptItem != null)
                                        ddlEmpDepartment.SelectedValue = empDepartment;
                                    else
                                    {
                                        // Value from DB not in dropdown — add it so it displays
                                        ddlEmpDepartment.Items.Add(new ListItem(empDepartment, empDepartment));
                                        ddlEmpDepartment.SelectedValue = empDepartment;
                                    }
                                }

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
            System.Diagnostics.Trace.WriteLine($"Page.IsPostBack: {IsPostBack}");
            System.Diagnostics.Trace.WriteLine($"Page.IsValid: {Page.IsValid}");
            System.Diagnostics.Trace.WriteLine($"Employee Mode: {isEmployeeMode}");

            // Check ALL field values
            System.Diagnostics.Trace.WriteLine($"txtEmpName.Text: '{txtEmpName.Text}'");
            System.Diagnostics.Trace.WriteLine($"txtEmpName.Text Length: {txtEmpName.Text.Length}");
            System.Diagnostics.Trace.WriteLine($"txtEmpPosition.Text: '{txtEmpPosition.Text}'");
            System.Diagnostics.Trace.WriteLine($"ddlEmpDepartment.SelectedValue: '{ddlEmpDepartment.SelectedValue}'");
            System.Diagnostics.Trace.WriteLine($"txtEmpId.Text: '{txtEmpId.Text}'");
            System.Diagnostics.Trace.WriteLine($"chkAgreeTerms.Checked: {chkAgreeTerms.Checked}");

            // Check if fields are enabled/readonly
            System.Diagnostics.Trace.WriteLine($"txtEmpName.Enabled: {txtEmpName.Enabled}");
            System.Diagnostics.Trace.WriteLine($"txtEmpName.ReadOnly: {txtEmpName.ReadOnly}");

            // Debug: Show current agreement ID
            if (!currentAgreementId.HasValue)
            {
                // Try multiple ways to get the acknowledgement receipt ID
                System.Diagnostics.Trace.WriteLine($"currentAgreementId is null, trying to recover...");

                // 1. From hidden field
                if (!string.IsNullOrEmpty(hdnAgreementId.Value))
                {
                    int parsedId;
                    if (int.TryParse(hdnAgreementId.Value, out parsedId))
                    {
                        currentAgreementId = parsedId;
                        System.Diagnostics.Trace.WriteLine($"Recovered from hdnAgreementId: {currentAgreementId}");
                    }
                }

                // 2. From ViewState
                if (!currentAgreementId.HasValue && ViewState["CurrentAgreementId"] != null)
                {
                    currentAgreementId = (int?)ViewState["CurrentAgreementId"];
                    System.Diagnostics.Trace.WriteLine($"Recovered from ViewState: {currentAgreementId}");
                }

                if (!currentAgreementId.HasValue)
                {
                    ShowError($"No acknowledgement receipt selected. CurrentAgreementId is null. Token: {accessToken}");
                    return;
                }
            }

            System.Diagnostics.Trace.WriteLine($"Acknowledgement ID: {currentAgreementId}");

            // Validate employee name
            if (string.IsNullOrWhiteSpace(txtEmpName.Text))
            {
                ShowError("Please enter your name.");
                System.Diagnostics.Trace.WriteLine("ERROR: Employee name is empty!");
                return;
            }

            // Validate employee staff ID
            if (string.IsNullOrWhiteSpace(txtEmpStaffId.Text))
            {
                ShowError("Please enter your Employee ID.");
                return;
            }

            // Validate position
            if (string.IsNullOrWhiteSpace(txtEmpPosition.Text))
            {
                ShowError("Please enter your position/job title.");
                return;
            }

            // Validate department
            if (string.IsNullOrWhiteSpace(ddlEmpDepartment.SelectedValue))
            {
                ShowError("Please select your department.");
                return;
            }

            if (!chkAgreeTerms.Checked)
            {
                ShowError("You must acknowledge the receipt.");
                return;
            }

            System.Diagnostics.Trace.WriteLine("All validations passed. Proceeding to save...");

            // Save to database
            SaveEmployeeAgreement();
        }

        private void SaveEmployeeAgreement()
        {
            System.Diagnostics.Trace.WriteLine($"=== SaveEmployeeAgreement() START ===");

            // Get Windows ID from current user
            string windowsId = User.Identity.Name;
            if (windowsId.Contains("\\"))
            {
                windowsId = windowsId.Split('\\')[1];
            }

            System.Diagnostics.Trace.WriteLine($"WINDOWS ID: {windowsId}");
            System.Diagnostics.Trace.WriteLine($"Employee Name from form: '{txtEmpName.Text}'");
            System.Diagnostics.Trace.WriteLine($"Employee Position from form: '{txtEmpPosition.Text}'");
            System.Diagnostics.Trace.WriteLine($"Employee Department from form: '{ddlEmpDepartment.SelectedValue}'");

            if (!currentAgreementId.HasValue)
            {
                ShowError("No acknowledgement receipt selected.");
                return;
            }

            System.Diagnostics.Trace.WriteLine($"ACKNOWLEDGEMENT RECEIPT: {currentAgreementId.Value}");

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
                            // The WHERE clause guards against race conditions:
                            // agreement_status = 'Pending' ensures only one submission wins
                            // if two people somehow reach Submit simultaneously.
                            // agreement_view_token IS NOT NULL prevents double-submit after
                            // the token has already been nulled by the first submission.
                            string updateQuery = @"
                        UPDATE hardware_agreements SET
                            employee_name = @employeeName,
                            employee_id = @employeeId,
                            employee_staff_id = @employeeStaffId,
                            employee_position = @employeePosition,
                            employee_department = @employeeDepartment,
                            employee_agreed_date = GETDATE(),
                            agreement_status = 'Agreed',
                            last_updated = GETDATE(),
                            agreement_view_token = NULL,
                            token_expiry_date = NULL
                        WHERE id = @id
                          AND agreement_status = 'Pending'
                          AND agreement_view_token IS NOT NULL";

                            int rowsAffected = 0;

                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, connection, transaction))
                            {
                                updateCmd.Parameters.Add("@employeeName", SqlDbType.NVarChar, 255).Value = txtEmpName.Text.Trim().ToUpper();
                                updateCmd.Parameters.Add("@employeeId", SqlDbType.NVarChar, 50).Value = windowsId;
                                updateCmd.Parameters.Add("@employeeStaffId", SqlDbType.NVarChar, 50).Value = txtEmpStaffId.Text.Trim().ToUpper();
                                updateCmd.Parameters.Add("@employeePosition", SqlDbType.NVarChar, 255).Value = txtEmpPosition.Text.Trim().ToUpper();
                                updateCmd.Parameters.Add("@employeeDepartment", SqlDbType.NVarChar, 255).Value = ddlEmpDepartment.SelectedValue.Trim();
                                updateCmd.Parameters.Add("@id", SqlDbType.Int).Value = currentAgreementId.Value;

                                rowsAffected = updateCmd.ExecuteNonQuery();
                                System.Diagnostics.Trace.WriteLine($"UPDATE rows affected: {rowsAffected}");
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

                                System.Diagnostics.Trace.WriteLine($"Verification - Updated Name: '{updatedName}'");

                                if (string.IsNullOrEmpty(updatedName))
                                {
                                    System.Diagnostics.Trace.WriteLine("ERROR: Name is still NULL!");
                                    transaction.Rollback();
                                    ShowError("UPDATE executed but employee_name is still NULL. Database issue detected.");
                                    return;
                                }

                                // Success - commit the transaction
                                transaction.Commit();
                                System.Diagnostics.Trace.WriteLine("Transaction COMMITTED - Data saved successfully!");

                                // Send confirmation email
                                try
                                {
                                    SendConfirmationEmail(windowsId);
                                }
                                catch (Exception emailEx)
                                {
                                    System.Diagnostics.Trace.WriteLine($"Email sending failed: {emailEx.Message}");
                                    // Don't fail the whole process if email fails
                                }

                                // Show success and redirect to Default.aspx (NOT reload!)
                                ShowSuccess("Acknowledgement receipt submitted successfully! Pending IT verification. Thank you for signing.");

                                // Disable the form to prevent re-submission
                                DisableEmployeeForm();
                                btnSubmitEmployee.Enabled = false;
                                btnSubmitEmployee.Visible = false;

                                // Redirect to Default.aspx after showing alert
                                string script = @"<script type='text/javascript'>
                            alert('Acknowledgement receipt submitted successfully! It is now pending IT verification. Thank you for signing.');
                            window.location.href = 'Default.aspx';
                        </script>";
                                ClientScript.RegisterStartupScript(this.GetType(), "successMsg", script);
                                return;
                            }
                            else
                            {
                                transaction.Rollback();
                                System.Diagnostics.Trace.WriteLine("No rows affected — acknowledgement receipt was either already signed or token was consumed");
                                // Could be a race: another session signed just before this one
                                ShowError("This acknowledgement receipt has already been signed or is no longer available. " +
                                          "Please contact IT support if you believe this is an error.");
                                DisableEmployeeForm();
                                btnSubmitEmployee.Visible = false;
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Trace.WriteLine($"Transaction ERROR: {ex.Message}");
                            ShowError($"Database error: {ex.Message}");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"General ERROR: {ex.Message}");
                    ShowError($"Error: {ex.Message}");
                }
            }
        }

        private void DisableEmployeeForm()
        {
            // Disable employee info fields
            txtEmpName.Enabled = false;
            txtEmpStaffId.Enabled = false;
            txtEmpPosition.Enabled = false;
            ddlEmpDepartment.Enabled = false;

            // Disable checkboxes and buttons
            chkAgreeTerms.Enabled = false;
            btnSubmitEmployee.Enabled = false;
        }

        /// <summary>
        /// Returns email addresses for all active IT admins who have email notifications enabled
        /// (admin=1, active=1, receive_notification=1)
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
                    using (SqlCommand cmd = new SqlCommand("SELECT email FROM hardware_users WHERE admin = 1 AND active = 1 AND ISNULL(receive_notification, 1) = 1 AND email IS NOT NULL AND email != ''", conn))
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            string e = rdr["email"].ToString().Trim();
                            if (!string.IsNullOrEmpty(e)) emails.Add(e);
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Trace.WriteLine("GetAllAdminEmails error: " + ex.Message); }
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
                System.Diagnostics.Trace.WriteLine($"=== SendConfirmationEmail() START ===");
                System.Diagnostics.Trace.WriteLine($"Acknowledgement Receipt: {currentAgreementId}");

                if (!currentAgreementId.HasValue)
                {
                    System.Diagnostics.Trace.WriteLine("SendConfirmationEmail - No acknowledgement receipt!");
                    return;
                }

                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

                string agreementNumber = "";
                string employeeEmail = "";
                string hodEmail = "";
                string itStaff = "";
                string employeeName = "";
                string employeeStaffId = "";
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
                       COALESCE(a.employee_staff_id, '') as employee_staff_id,
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
                                employeeStaffId = SafeConvertToString(reader["employee_staff_id"]);
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

                System.Diagnostics.Trace.WriteLine($"EMAIL DATA - Acknowledgement: {agreementNumber}, Employee: {employeeEmail}, HOD: {hodEmail}");

                if (string.IsNullOrEmpty(employeeEmail))
                {
                    System.Diagnostics.Trace.WriteLine("SendConfirmationEmail - No employee email!");
                    return;
                }

                // ── Phase 2: Single email — IT Admins (To), Employee (CC) ─────────
                // Employee has just submitted; notify IT admins to verify.
                // No separate employee email needed
                // since the employee sees the success message on-screen.
                string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd('/');
                string verifyUrl = $"{baseUrl}/Agreement.aspx?id={currentAgreementId.Value}&mode=view";
                string signatureDateStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                System.Net.Mail.MailMessage adminMail = new System.Net.Mail.MailMessage();
                adminMail.From = new MailAddress(System.Configuration.ConfigurationManager.AppSettings["SmtpSenderAddress"] ?? "laptop_desktop_acknowledgement@pancentury.com", System.Configuration.ConfigurationManager.AppSettings["SmtpSenderDisplayName"] ?? "Laptop & Desktop Acknowledgement Receipt System");
                // IT admins → TO (they need to take action)
                foreach (string adminEmail in GetAllAdminEmails())
                {
                    adminMail.To.Add(adminEmail);
                }
                if (!string.IsNullOrEmpty(employeeEmail)) adminMail.CC.Add(employeeEmail); // Employee → CC
                adminMail.Subject = $"[Phase 2 → 3] Acknowledgement Receipt {agreementNumber} — Employee Signed, IT Verification Required";
                adminMail.IsBodyHtml = true;
                adminMail.Body = $@"<!DOCTYPE html>
<html>
<head><style>
  body{{font-family:Arial,sans-serif;line-height:1.6;color:#333}}
  .container{{max-width:600px;margin:0 auto;padding:20px}}
  .header{{background-color:#3b82f6;color:white;padding:20px;text-align:center;border-radius:10px 10px 0 0}}
  .content{{background-color:#f9f9f9;padding:20px;border-radius:0 0 10px 10px;border:1px solid #ddd}}
  table{{width:100%;border-collapse:collapse;margin:20px 0}}
  th,td{{padding:10px;text-align:left;border-bottom:1px solid #ddd}}
  th{{background-color:#f2f2f2;width:40%}}
</style></head>
<body>
  <div class='container'>
    <div class='header'><p style='margin:0 0 4px;font-size:.78rem;opacity:.75;letter-spacing:.07em;text-transform:uppercase;'>Phase 2 Complete</p><h1 style='margin:0;'>IT Verification Required</h1><p style='margin:6px 0 0;opacity:.9;font-size:.9rem;'>Employee has agreed — IT admin action needed to complete</p></div>
    <div class='content'>
      <p>The employee has signed acknowledgement receipt <strong>{agreementNumber}</strong>. Please verify the laptop/desktop and system configuration.</p>
      <table>
        <tr><th>Acknowledgement Receipt Number:</th><td>{agreementNumber}</td></tr>
        <tr><th>Employee:</th><td>{employeeName}</td></tr>
        <tr><th>Employee ID:</th><td>{employeeStaffId}</td></tr>
        <tr><th>Position:</th><td>{employeePosition}</td></tr>
        <tr><th>Department:</th><td>{employeeDepartment}</td></tr>
        <tr><th>Model:</th><td>{modelName}</td></tr>
        <tr><th>Serial Number:</th><td>{serialNumber}</td></tr>
        <tr><th>Signed Date:</th><td>{signatureDateStr}</td></tr>
        <tr><th>IT Staff:</th><td>{itStaff}</td></tr>
      </table>
      <div style='background-color:#f0f9ff;padding:20px;border-radius:8px;border-left:4px solid #3b82f6;margin:20px 0'>
        <h3 style='color:#1e40af;margin-top:0'>Action Required</h3>
        <p>Please access the acknowledgement receipt to complete IT verification:</p>
        <p style='margin:15px 0'>
          <a href='{verifyUrl}' style='display:inline-block;background-color:#f59e0b;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;font-weight:bold'>Verify Acknowledgement Receipt Now</a>
        </p>
        <p style='font-size:0.8rem;color:#6b7280'>Or copy: <span style='background:#f3f4f6;padding:5px;border-radius:4px;font-family:monospace'>{verifyUrl}</span></p>
      </div>
      <p style='color:#666;font-size:0.9em'>This is an automated message from the Laptop & Desktop Acknowledgement Receipt System.</p>
    </div>
  </div>
</body></html>";

                if (adminMail.To.Count > 0 || adminMail.CC.Count > 0)
                {
                    using (SmtpClient smtpAdmin = new SmtpClient())
                    {
                        smtpAdmin.Timeout = 30000;
                        smtpAdmin.Send(adminMail);
                        System.Diagnostics.Trace.WriteLine("SendConfirmationEmail - IT admin + HOD notification sent");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"SendConfirmationEmail ERROR: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"StackTrace: {ex.StackTrace}");
                // Don't throw - just log the error
            }
        }

        // Updated SendAgreementEmail method with token generation
        private bool SendAgreementEmail(string action, string agreementNumber, string status)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine($"=== SendAgreementEmail START === action={action}, agreementNumber={agreementNumber}, status={status}, currentAgreementId={currentAgreementId}");

                // Get email addresses from hidden fields
                string employeeEmail = hdnEmployeeEmail.Value;
                string hodEmail = hdnHODEmail.Value;

                System.Diagnostics.Trace.WriteLine($"  employeeEmail='{employeeEmail}', hodEmail='{hodEmail}'");

                if (string.IsNullOrEmpty(employeeEmail) || string.IsNullOrEmpty(hodEmail))
                {
                    System.Diagnostics.Trace.WriteLine("  ABORT: employeeEmail or hodEmail is empty — no email sent.");
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
                    System.Diagnostics.Trace.WriteLine($"  Fetching token for agreement ID {currentAgreementId.Value}...");
                    // Get the acknowledgement receipt number from database (not parameter)
                    string dbAgreementNumber = "";
                    string token = "";

                    // Get both acknowledgement receipt number and token from database
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

                    // Use the actual acknowledgement receipt number from database
                    if (string.IsNullOrEmpty(dbAgreementNumber))
                    {
                        dbAgreementNumber = agreementNumber;
                    }

                    System.Diagnostics.Trace.WriteLine($"  DB token='{(string.IsNullOrEmpty(token) ? "(EMPTY)" : token.Substring(0, Math.Min(8, token.Length)) + "...")}', dbAgreementNumber='{dbAgreementNumber}'");

                    if (!string.IsNullOrEmpty(token))
                    {
                        // Construct the URL correctly
                        string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd('/');
                        string tokenUrl = $"{baseUrl}/Agreement.aspx?token={token}";

                        tokenSection = $@"<div style='background-color: #f0f9ff; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #3b82f6;'>
                    <h3 style='color: #1e40af; margin-top: 0;'>Action Required</h3>
                    <p>Please review and sign the laptop & desktop acknowledgement receipt by clicking the link below:</p>
                    <p style='margin: 15px 0;'>
                        <a href='{tokenUrl}' style='display: inline-block; background-color: #3b82f6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>
                            Sign Acknowledgement Receipt Now
                        </a>
                    </p>
                    <p style='font-size: 0.9rem; color: #4b5563;'>
                        <strong>Note:</strong> This link will expire in 7 days.
                    </p>
                    <p style='font-size: 0.8rem; color: #6b7280; margin-top: 10px;'>
                        Acknowledgement Receipt Number: <strong>{dbAgreementNumber}</strong><br>
                        Or copy this link: <span style='background-color: #f3f4f6; padding: 5px; border-radius: 4px; font-family: monospace;'>{tokenUrl}</span>
                    </p>
                </div>";
                    }
                    else
                    {
                        tokenSection = $@"<div style='background-color: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f59e0b;'>
                    <h3 style='color: #b45309; margin-top: 0;'>Important Notice</h3>
                    <p>The acknowledgement receipt signing link could not be generated. Please contact IT support.</p>
                    <p>Acknowledgement Receipt Number: <strong>{dbAgreementNumber}</strong></p>
                </div>";
                    }
                }

                // ── Phase 1: Single email — Employee (To), HOD + IT Admins (CC) ──
                if (action == "Submitted")
                {
                    string sharedStyle = @"<style>
                        body{font-family:Arial,sans-serif;line-height:1.6;color:#333}
                        .container{max-width:600px;margin:0 auto;padding:20px}
                        .header{background-color:#667eea;color:white;padding:20px;text-align:center;border-radius:10px 10px 0 0}
                        .content{background-color:#f9f9f9;padding:20px;border-radius:0 0 10px 10px;border:1px solid #ddd}
                        table{width:100%;border-collapse:collapse;margin:20px 0}
                        th,td{padding:10px;text-align:left;border-bottom:1px solid #ddd}
                        th{background-color:#f2f2f2}
                    </style>";

                    string detailsTable = $@"<table>
                        <tr><th>Acknowledgement Receipt Number:</th><td>{agreementNumber}</td></tr>
                        <tr><th>Status:</th><td>{status}</td></tr>
                        <tr><th>Model:</th><td>{modelName}</td></tr>
                        <tr><th>Serial Number:</th><td>{txtSerialNumber.Text}</td></tr>
                        <tr><th>IT Staff:</th><td>{txtITStaff.Text}</td></tr>
                        <tr><th>Issue Date:</th><td>{DateTime.Now:dd/MM/yyyy}</td></tr>
                    </table>";

                    // Single email: Employee (To), IT Admins (CC)
                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress(System.Configuration.ConfigurationManager.AppSettings["SmtpSenderAddress"] ?? "laptop_desktop_acknowledgement@pancentury.com", System.Configuration.ConfigurationManager.AppSettings["SmtpSenderDisplayName"] ?? "Laptop & Desktop Acknowledgement Receipt System");
                    mail.To.Add(employeeEmail);
                    CcAllAdmins(mail);
                    mail.Subject = $"[Phase 1 → 2] Laptop & Desktop Acknowledgement Receipt {agreementNumber} — Employee Acknowledgement Required";
                    mail.IsBodyHtml = true;
                    mail.Body = $@"<!DOCTYPE html><html><head>{sharedStyle}</head><body>
                        <div class='container'>
                            <div class='header'><p style='margin:0 0 4px;font-size:.78rem;opacity:.75;letter-spacing:.07em;text-transform:uppercase;'>Phase 1 Complete</p><h1 style='margin:0;'>Employee Acknowledgement Required</h1><p style='margin:6px 0 0;opacity:.9;font-size:.9rem;'>IT admin has raised an acknowledgement receipt — employee action needed</p></div>
                            <div class='content'>
                                <h2>Acknowledgement Receipt Details</h2>
                                {detailsTable}
                                {tokenSection}
                            </div>
                        </div></body></html>";

                    System.Diagnostics.Trace.WriteLine($"  Sending email: To={employeeEmail}, CC={hodEmail}, Subject={mail.Subject}");
                    using (SmtpClient smtp = new SmtpClient()) { smtp.Timeout = 10000; smtp.Send(mail); }

                    System.Diagnostics.Trace.WriteLine("  EMAIL SENT SUCCESSFULLY.");
                    return true;
                }

                // Non-Submitted actions (e.g. future use): no email sent
                System.Diagnostics.Trace.WriteLine($"  ABORT: action='{action}' is not 'Submitted' — no email sent.");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"SendAgreementEmail FAILED: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"  Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                    System.Diagnostics.Trace.WriteLine($"  Inner: {ex.InnerException.Message}");
                return false;
            }
        }

        protected void btnVerify_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("=== IT VERIFY START ===");

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
                    ShowError("No acknowledgement receipt selected for verification.");
                    return;
                }
            }

            // Phase 3: Asset Number is mandatory — show popup notification if empty
            if (string.IsNullOrWhiteSpace(txtAssetNumber.Text))
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "assetRequiredPopup",
                    "document.getElementById('assetRequiredModal').style.display='flex';", true);
                return;
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
                ShowError($"Only acknowledgement receipts with 'Agreed' status can be verified. Current status: {actualStatus}");
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
                            asset_number = @assetNumber,
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
                        command.Parameters.AddWithValue("@assetNumber", txtAssetNumber.Text.Trim());
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
                            bool verifyEmailSent = true;
                            try
                            {
                                SendVerificationCompleteEmail(currentAgreementId.Value, verifiedBy);
                            }
                            catch (Exception emailEx)
                            {
                                System.Diagnostics.Trace.WriteLine("SendVerificationCompleteEmail failed: " + emailEx.Message);
                                verifyEmailSent = false;
                            }

                            if (verifyEmailSent)
                                ShowSuccess("Acknowledgement receipt verified and completed successfully! Final notification sent to employee and HOD.");
                            else
                                ShowSuccess("Acknowledgement receipt verified and completed! Warning: final email notification could not be sent.");

                            string script = "<script type='text/javascript'>" +
                                            "setTimeout(function(){ window.location.href = 'ExistingAgreements.aspx'; }, 2000);" +
                                            "</script>";
                            ClientScript.RegisterStartupScript(this.GetType(), "redirect", script);
                        }
                        else
                        {
                            ShowError("Failed to verify acknowledgement receipt. Please try again.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Verify ERROR: {ex.Message}");
                    ShowError($"Error verifying acknowledgement receipt: {ex.Message}");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // REJECT — returns agreement to Pending, wipes employee Phase 2 data,
        //          generates a fresh token and emails the employee with reason.
        // ─────────────────────────────────────────────────────────────────────
        protected void btnReject_Click(object sender, EventArgs e)
        {
            // Recover agreement ID if missing
            if (!currentAgreementId.HasValue)
            {
                int parsedId;
                if (Request.QueryString["id"] != null &&
                    int.TryParse(Request.QueryString["id"], out parsedId))
                    currentAgreementId = parsedId;

                if (!currentAgreementId.HasValue)
                {
                    ShowError("No acknowledgement receipt selected for rejection.");
                    return;
                }
            }

            // Only Agreed agreements can be rejected
            string actualStatus = GetCurrentAgreementStatus();
            if (actualStatus != "Agreed")
            {
                ShowError($"Only acknowledgement receipts with 'Agreed' status can be rejected. Current status: {actualStatus}");
                return;
            }

            string rejectReason = txtRejectReason.Text.Trim();
            if (string.IsNullOrEmpty(rejectReason))
            {
                ShowError("Please provide a rejection reason so the employee knows what to correct.");
                return;
            }

            string connStr = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();

                    // Generate a fresh signing token
                    string newToken = GenerateSecureToken();
                    DateTime tokenExpiry = DateTime.Now.AddDays(7);

                    // Reset agreement back to Pending:
                    //  • Clear all Phase 2 employee fields
                    //  • Issue a new signing token
                    //  • Status → Pending
                    string updateQuery = @"
                        UPDATE hardware_agreements SET
                            agreement_status        = 'Pending',
                            employee_name           = NULL,
                            employee_id             = NULL,
                            employee_staff_id       = NULL,
                            employee_position       = NULL,
                            employee_department     = NULL,
                            employee_agreed_date    = NULL,
                            agreement_view_token    = @newToken,
                            token_expiry_date       = @tokenExpiry,
                            last_updated            = GETDATE()
                        WHERE id     = @id
                          AND agreement_status = 'Agreed'";

                    int rows;
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@newToken", newToken);
                        cmd.Parameters.AddWithValue("@tokenExpiry", tokenExpiry);
                        cmd.Parameters.AddWithValue("@id", currentAgreementId.Value);
                        rows = cmd.ExecuteNonQuery();
                    }

                    if (rows == 0)
                    {
                        ShowError("Acknowledgement receipt could not be rejected. It may have already been processed.");
                        return;
                    }

                    // Log to audit table
                    try
                    {
                        using (SqlCommand auditCmd = new SqlCommand(@"
                            INSERT INTO agreement_audit_log
                                (agreement_id, old_status, new_status, changed_by, source_action)
                            VALUES (@id, 'Agreed', 'Pending', @by, 'IT_REJECT')", conn))
                        {
                            auditCmd.Parameters.AddWithValue("@id", currentAgreementId.Value);
                            auditCmd.Parameters.AddWithValue("@by", User.Identity.Name);
                            auditCmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* audit failure must not block the rejection */ }

                    // Send rejection email to employee
                    try
                    {
                        SendRejectionEmail(currentAgreementId.Value, newToken, rejectReason, User.Identity.Name, conn);
                    }
                    catch (Exception emailEx)
                    {
                        System.Diagnostics.Trace.WriteLine("SendRejectionEmail failed: " + emailEx.Message);
                        // Still show success — the DB change happened, only email failed
                        ShowError("Acknowledgement receipt rejected and reset to Pending. Warning: notification email could not be sent — please inform the employee manually.");
                        return;
                    }

                    ShowSuccess("Acknowledgement receipt rejected and returned to the employee for correction. A new signing link has been emailed to them.");

                    string script = "<script type='text/javascript'>" +
                                    "setTimeout(function(){ window.location.href = 'ExistingAgreements.aspx'; }, 2500);" +
                                    "</script>";
                    ClientScript.RegisterStartupScript(this.GetType(), "redirect", script);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"btnReject_Click ERROR: {ex.Message}");
                    ShowError($"Error rejecting acknowledgement receipt: {ex.Message}");
                }
            }
        }

        private void SendRejectionEmail(int agreementId, string newToken,
                                         string rejectReason, string rejectedBy,
                                         SqlConnection conn)
        {
            // Load agreement data
            string agreementNumber = "", employeeEmail = "", hodEmail = "",
                   modelName = "", serialNumber = "", assetNumber = "", itStaff = "";

            using (SqlCommand cmd = new SqlCommand(@"
                SELECT ha.agreement_number, ha.employee_email, ha.hod_email,
                       ha.serial_number, ha.asset_number, ha.it_staff_win_id,
                       hm.model
                FROM   hardware_agreements ha
                INNER JOIN hardware_model hm ON ha.model_id = hm.id
                WHERE  ha.id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", agreementId);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        agreementNumber = SafeConvertToString(r["agreement_number"]);
                        employeeEmail = SafeConvertToString(r["employee_email"]);
                        hodEmail = SafeConvertToString(r["hod_email"]);
                        modelName = SafeConvertToString(r["model"]);
                        serialNumber = SafeConvertToString(r["serial_number"]);
                        assetNumber = SafeConvertToString(r["asset_number"]);
                        itStaff = SafeConvertToString(r["it_staff_win_id"]);
                    }
                }
            }

            if (string.IsNullOrEmpty(employeeEmail)) return;

            string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority +
                              Request.ApplicationPath.TrimEnd('/');
            string tokenUrl = $"{baseUrl}/Agreement.aspx?token={newToken}";

            string sharedStyle = @"<style>
                body{font-family:Arial,sans-serif;line-height:1.6;color:#333}
                .container{max-width:600px;margin:0 auto;padding:20px}
                .header{background:linear-gradient(135deg,#ef4444,#dc2626);color:white;padding:20px;text-align:center;border-radius:10px 10px 0 0}
                .content{background:#f9f9f9;padding:20px;border-radius:0 0 10px 10px;border:1px solid #ddd}
                table{width:100%;border-collapse:collapse;margin:16px 0}
                th,td{padding:10px;text-align:left;border-bottom:1px solid #ddd}
                th{background:#f2f2f2}</style>";

            string detailsTable = $@"<table>
                <tr><th>Acknowledgement Receipt Number:</th><td>{agreementNumber}</td></tr>
                <tr><th>Model:</th><td>{modelName}</td></tr>
                <tr><th>Serial Number:</th><td>{serialNumber}</td></tr>
                <tr><th>Asset Number:</th><td>{assetNumber}</td></tr>
                <tr><th>IT Staff:</th><td>{itStaff}</td></tr>
                <tr><th>Rejected By:</th><td>{rejectedBy}</td></tr>
                <tr><th>Rejection Date:</th><td>{DateTime.Now:dd/MM/yyyy HH:mm}</td></tr>
            </table>";

            string reasonBox = $@"<div style='background:#fef2f2;padding:16px;border-radius:8px;border-left:4px solid #ef4444;margin:20px 0'>
                <h3 style='color:#dc2626;margin-top:0'>
                    <i>⚠</i> Reason for Rejection
                </h3>
                <p style='margin:0;white-space:pre-wrap'>{System.Web.HttpUtility.HtmlEncode(rejectReason)}</p>
            </div>";

            string signingSection = $@"<div style='background:#f0f9ff;padding:20px;border-radius:8px;border-left:4px solid #3b82f6;margin:20px 0'>
                <h3 style='color:#1e40af;margin-top:0'>Action Required — Please Re-sign</h3>
                <p>Please correct the information described above and re-submit your acknowledgement receipt:</p>
                <p style='margin:15px 0'>
                    <a href='{tokenUrl}' style='display:inline-block;background:#3b82f6;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;font-weight:bold'>
                        Re-sign Acknowledgement Receipt
                    </a>
                </p>
                <p style='font-size:.85rem;color:#6b7280'>
                    This link expires in 7 days. Agreement: <strong>{agreementNumber}</strong>
                </p>
            </div>";

            // ── Single email: Employee (To), IT Admins (CC) ────────
            MailMessage empMail = new MailMessage();
            empMail.From = new MailAddress(System.Configuration.ConfigurationManager.AppSettings["SmtpSenderAddress"] ?? "laptop_desktop_acknowledgement@pancentury.com", System.Configuration.ConfigurationManager.AppSettings["SmtpSenderDisplayName"] ?? "Laptop & Desktop Acknowledgement Receipt System");
            empMail.To.Add(employeeEmail);
            CcAllAdmins(empMail);
            empMail.Subject = $"[Phase 2 — Returned] Laptop & Desktop Acknowledgement Receipt {agreementNumber} — Corrections Required";
            empMail.IsBodyHtml = true;
            empMail.Body = $@"<!DOCTYPE html><html><head>{sharedStyle}</head><body>
                <div class='container'>
                    <div class='header'>
                        <p style='margin:0 0 4px;font-size:.78rem;opacity:.75;letter-spacing:.07em;text-transform:uppercase;'>Phase 2 — Returned</p><h1 style='margin:0;'>Acknowledgement Receipt Returned for Correction</h1><p style='margin:6px 0 0;opacity:.9'>IT admin has returned your acknowledgement receipt — please review and re-sign</p>
                    </div>
                    <div class='content'>
                        <p>Dear Employee,</p>
                        <p>The IT team has reviewed your acknowledgement receipt submission and found that some information needs to be corrected.</p>
                        <h2>Acknowledgement Receipt Details</h2>
                        {detailsTable}
                        {reasonBox}
                        {signingSection}
                    </div>
                </div></body></html>";

            using (SmtpClient smtp = new SmtpClient()) { smtp.Timeout = 10000; smtp.Send(empMail); }
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
                        SELECT employee_name, employee_id, employee_staff_id, employee_position, employee_department,
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
                                txtVerifyEmpStaffId.Text = SafeConvertToString(reader["employee_staff_id"]);
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
                    System.Diagnostics.Trace.WriteLine($"LoadVerificationData ERROR: {ex.Message}");
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
            string employeeStaffId = "";
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
                           COALESCE(a.employee_staff_id, '') as employee_staff_id,
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
                            employeeStaffId = SafeConvertToString(reader["employee_staff_id"]);
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
                System.Diagnostics.Trace.WriteLine("SendVerificationCompleteEmail - No employee email!");
                return;
            }

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(System.Configuration.ConfigurationManager.AppSettings["SmtpSenderAddress"] ?? "laptop_desktop_acknowledgement@pancentury.com", System.Configuration.ConfigurationManager.AppSettings["SmtpSenderDisplayName"] ?? "Laptop & Desktop Acknowledgement Receipt System");
            mail.To.Add(employeeEmail);

            // CC all IT admins
            CcAllAdmins(mail);

            mail.Subject = $"[Phase 3 Complete] Laptop & Desktop Acknowledgement Receipt {agreementNumber} — Completed & Verified";
            mail.IsBodyHtml = true;

            string verifiedDateStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd('/');
            string viewUrl = $"{baseUrl}/Agreement.aspx?id={agreementId}&mode=empview";

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
            <p style='margin:0 0 4px;font-size:.78rem;opacity:.75;letter-spacing:.07em;text-transform:uppercase;'>Phase 3 Complete</p><h1 style='margin:0;'>&#10004; Acknowledgement Receipt Completed &amp; Verified</h1>
        </div>
        <div class='content'>
            <p>Dear {employeeName},</p>
            <p>Your laptop & desktop acknowledgement receipt has been <strong>verified by IT</strong> and is now fully completed.</p>

            <div style='text-align:center;margin:28px 0;'>
                <a href='{viewUrl}'
                   style='display:inline-block;background:#10b981;color:#fff;padding:14px 36px;
                          text-decoration:none;border-radius:8px;font-weight:bold;font-size:15px;'>
                    &#10004; View Completed Acknowledgement Receipt
                </a>
                <p style='margin:10px 0 0;font-size:0.82rem;color:#6b7280;'>
                    Or copy: <span style='background:#f3f4f6;padding:3px 7px;border-radius:4px;
                    font-family:monospace;font-size:0.8rem;word-break:break-all;'>{viewUrl}</span>
                </p>
            </div>

            <h3>Acknowledgement Receipt Details</h3>
            <table class='details-table'>
                <tr><th>Acknowledgement Receipt Number:</th><td>{agreementNumber}</td></tr>
                <tr><th>Status:</th><td><span class='success-badge'>Completed</span></td></tr>
                <tr><th>Employee Name:</th><td>{employeeName}</td></tr>
                <tr><th>Employee ID:</th><td>{employeeStaffId}</td></tr>
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
            
            <p>Please keep this email for your records. This acknowledgement receipt is now fully complete.</p>
            <p>If you have any questions, please contact the IT department.</p>
            
            <p style='margin-top: 30px; color: #666; font-size: 0.9em;'>
                This is an automated message from the Laptop & Desktop Acknowledgement Receipt System.
            </p>
        </div>
    </div>
</body>
</html>";

            using (SmtpClient smtpClient = new SmtpClient())
            {
                smtpClient.Timeout = 30000;
                smtpClient.Send(mail);
                System.Diagnostics.Trace.WriteLine($"SendVerificationCompleteEmail - Email sent to {employeeEmail}");
            }
        }

        /// <summary>
        /// Resolves an acknowledgement receipt ID from a token string.
        /// Used by the admin short-circuit so IT admins who click token links
        /// get redirected to the normal admin view instead of an empty form.
        /// </summary>
        private int? GetAgreementIdByToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            try
            {
                string cs = System.Configuration.ConfigurationManager
                    .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT id FROM hardware_agreements WHERE agreement_view_token = @t", conn))
                    {
                        cmd.Parameters.AddWithValue("@t", token);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("GetAgreementIdByToken error: " + ex.Message);
            }
            return null;
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

        // ── FIX: Per-request DB admin validation (replaces Session-only trust) ──
        private bool CheckUserIsAdminFromDB(string windowsUser)
        {
            string cs = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT admin FROM hardware_users WHERE win_id = @win_id AND active = 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@win_id", windowsUser);
                        object r = cmd.ExecuteScalar();
                        return r != null && r != DBNull.Value && Convert.ToInt32(r) == 1;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("CheckUserIsAdminFromDB: " + ex.Message);
                    return false; // fail closed
                }
            }
        }

        /// <summary>
        /// Checks whether the currently logged-in Windows user is the designated employee
        /// for the given agreement (compares hardware_users.email with hardware_agreements.employee_email).
        /// </summary>
        private bool IsCurrentUserTheEmployee(int agreementId)
        {
            string cs = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
            try
            {
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();

                    // Get the employee email on this acknowledgement receipt
                    string empEmail = "";
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT employee_email FROM hardware_agreements WHERE id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", agreementId);
                        object r = cmd.ExecuteScalar();
                        empEmail = (r != null && r != DBNull.Value) ? r.ToString().Trim() : "";
                    }
                    if (string.IsNullOrEmpty(empEmail)) return false;

                    // Get the current Windows user's email
                    string userEmail = "";
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT email FROM hardware_users WHERE win_id = @w AND active = 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@w", User.Identity.Name);
                        object r = cmd.ExecuteScalar();
                        userEmail = (r != null && r != DBNull.Value) ? r.ToString().Trim() : "";
                    }

                    return !string.IsNullOrEmpty(userEmail) &&
                           userEmail.Equals(empEmail, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("IsCurrentUserTheEmployee error: " + ex.Message);
                return false; // fail closed
            }
        }

        // Cryptographically strong token.
        // 36 random bytes → 48-char Base64url output — fits safely in nvarchar(50).
        // (48 bytes would produce 64 chars and cause SQL string-truncation on INSERT.)
        private string GenerateSecureToken()
        {
            byte[] data = new byte[36];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                rng.GetBytes(data);
            return Convert.ToBase64String(data)
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        /// <summary>
        /// Verifies the currently logged-in (non-admin) user is the designated employee
        /// for the given agreement, based on their email in hardware_users.
        /// </summary>
        private bool CanEmployeeViewAgreement(int agreementId)
        {
            string cs = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                try
                {
                    conn.Open();
                    // Get the employee_email stored on the acknowledgement receipt
                    string empEmail = "";
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT employee_email FROM hardware_agreements WHERE id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", agreementId);
                        object r = cmd.ExecuteScalar();
                        empEmail = (r != null && r != DBNull.Value) ? r.ToString().Trim() : "";
                    }
                    if (string.IsNullOrEmpty(empEmail)) return false;

                    // Look up the current Windows user's email from hardware_users
                    string userEmail = "";
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT email FROM hardware_users WHERE win_id = @w AND active = 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@w", User.Identity.Name);
                        object r = cmd.ExecuteScalar();
                        userEmail = (r != null && r != DBNull.Value) ? r.ToString().Trim() : "";
                    }

                    return !string.IsNullOrEmpty(userEmail) &&
                           userEmail.Equals(empEmail, StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("CanEmployeeViewAgreement error: " + ex.Message);
                    return false; // fail closed
                }
            }
        }

        /// <summary>
        /// Sends IT-admin-triggered update notification email.
        /// To: Employee. CC: HOD + all IT admins.
        /// If the acknowledgement receipt was Pending, a new signing token is supplied.
        /// </summary>
        private void SendAdminSaveNotifyEmail(int agreementId, string dbStatus, string newToken)
        {
            string cs = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            string agreementNumber = "", employeeEmail = "", hodEmail = "", itStaff = "",
                   modelName = "", serialNumber = "", assetNumber = "";

            using (SqlConnection conn = new SqlConnection(cs))
            {
                conn.Open();
                string q = @"SELECT a.agreement_number, a.employee_email, a.hod_email,
                                    a.it_staff_win_id, a.serial_number, a.asset_number, m.model
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

            string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority +
                             Request.ApplicationPath.TrimEnd('/');

            // Build the action section (sign link for Pending, view link for others)
            string actionSection;
            if (dbStatus == "Pending" && !string.IsNullOrEmpty(newToken))
            {
                string tokenUrl = $"{baseUrl}/Agreement.aspx?token={newToken}";
                actionSection = $@"<div style='background:#f0f9ff;padding:20px;border-radius:8px;border-left:4px solid #3b82f6;margin:20px 0'>
                    <h3 style='color:#1e40af;margin-top:0'>Action Required — Please Re-sign</h3>
                    <p>The IT department has updated your acknowledgement receipt details. Please review and sign using the new link below:</p>
                    <p style='margin:15px 0'>
                        <a href='{tokenUrl}' style='display:inline-block;background:#3b82f6;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;font-weight:bold'>
                            Sign Acknowledgement Receipt Now
                        </a>
                    </p>
                    <p style='font-size:.85rem;color:#6b7280'><strong>Note:</strong> This link expires in 7 days. Any previous signing links are no longer valid.</p>
                </div>";
            }
            else
            {
                // For Agreed/Completed — employee can view their acknowledgement receipt
                string viewUrl = $"{baseUrl}/Agreement.aspx?id={agreementId}&mode=empview";
                actionSection = $@"<div style='background:#f0fdf4;padding:20px;border-radius:8px;border-left:4px solid #10b981;margin:20px 0'>
                    <h3 style='color:#065f46;margin-top:0'>Acknowledgement Receipt Updated by IT</h3>
                    <p>Your acknowledgement receipt has been updated by the IT department. You can view your acknowledgement receipt using the link below:</p>
                    <p style='margin:15px 0'>
                        <a href='{viewUrl}' style='display:inline-block;background:#10b981;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;font-weight:bold'>
                            View My Acknowledgement Receipt
                        </a>
                    </p>
                </div>";
            }

            string sharedStyle = @"<style>
                body{font-family:Arial,sans-serif;line-height:1.6;color:#333}
                .container{max-width:600px;margin:0 auto;padding:20px}
                .header{background:linear-gradient(135deg,#667eea,#764ba2);color:white;padding:20px;text-align:center;border-radius:10px 10px 0 0}
                .content{background:#f9f9f9;padding:20px;border-radius:0 0 10px 10px;border:1px solid #ddd}
                table{width:100%;border-collapse:collapse;margin:16px 0}
                th,td{padding:10px;text-align:left;border-bottom:1px solid #ddd}
                th{background:#f2f2f2}</style>";

            string detailsTable = $@"<table>
                <tr><th>Acknowledgement Receipt Number:</th><td>{agreementNumber}</td></tr>
                <tr><th>Status:</th><td>{dbStatus}</td></tr>
                <tr><th>Model:</th><td>{modelName}</td></tr>
                <tr><th>Serial Number:</th><td>{serialNumber}</td></tr>
                <tr><th>Asset Number:</th><td>{assetNumber}</td></tr>
                <tr><th>IT Staff:</th><td>{itStaff}</td></tr>
                <tr><th>Updated By:</th><td>{User.Identity.Name}</td></tr>
                <tr><th>Update Date:</th><td>{DateTime.Now:dd/MM/yyyy HH:mm}</td></tr>
            </table>";

            // ── Single email: Employee (To), IT Admins (CC) ──────────
            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
            mail.From = new System.Net.Mail.MailAddress(System.Configuration.ConfigurationManager.AppSettings["SmtpSenderAddress"] ?? "laptop_desktop_acknowledgement@pancentury.com", System.Configuration.ConfigurationManager.AppSettings["SmtpSenderDisplayName"] ?? "Laptop & Desktop Acknowledgement Receipt System");
            mail.To.Add(employeeEmail);
            CcAllAdmins(mail);
            mail.Subject = $"[IT Update] Laptop & Desktop Acknowledgement Receipt {agreementNumber} — Details Updated";
            mail.IsBodyHtml = true;
            mail.Body = $@"<!DOCTYPE html><html><head>{sharedStyle}</head><body>
                <div class='container'>
                    <div class='header'>
                        <p style='margin:0 0 4px;font-size:.78rem;opacity:.75;letter-spacing:.07em;text-transform:uppercase;'>IT Admin Update</p><h1 style='margin:0;'>Acknowledgement Receipt Details Updated</h1><p style='margin:5px 0 0;opacity:.9;font-size:.9rem;'>IT department has made changes to your laptop & desktop acknowledgement receipt</p>
                    </div>
                    <div class='content'>
                        <h2>Acknowledgement Receipt Details</h2>
                        {detailsTable}
                        {actionSection}
                        <p style='color:#666;font-size:.9em'>This is an automated message from the Laptop & Desktop Acknowledgement Receipt System.</p>
                    </div>
                </div></body></html>";

            using (System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient())
            {
                smtp.Timeout = 10000;
                smtp.Send(mail);
            }
        }
    }
}