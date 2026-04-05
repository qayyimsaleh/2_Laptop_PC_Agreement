<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Agreement.aspx.cs" Inherits="WindowsAuthDemo.Agreement" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title><asp:Literal ID="litPageTitle" runat="server"></asp:Literal></title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta http-equiv="X-Content-Type-Options" content="nosniff">
    <meta name="referrer" content="strict-origin-when-cross-origin">
    <script>document.documentElement.setAttribute("data-theme",localStorage.getItem("portalTheme")||"light");</script>
    <link href="https://fonts.googleapis.com/css2?family=Sora:wght@300;400;500;600;700;800&family=DM+Mono:ital,wght@0,400;0,500;1,400&display=swap" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
    <link rel="stylesheet" href="hardware-portal-styles.css">

</head>
<body>
    <form id="form1" runat="server" autocomplete="off">
        <!-- Hidden fields for backup -->
        <asp:HiddenField ID="hdnEmpNameBackup" runat="server" />
        <asp:HiddenField ID="hdnEmpPositionBackup" runat="server" />
        <asp:HiddenField ID="hdnEmpDepartmentBackup" runat="server" />
        <!-- FEEDBACK FIX: Hidden field for archive remarks -->
        <asp:HiddenField ID="hdnArchiveRemarks" runat="server" />
        <!-- Backup token in hidden field — survives PostBack even if QS is stripped -->
        <asp:HiddenField ID="hdnAccessToken" runat="server" />
        
        <!-- Sidebar -->
        <aside class="sidebar">
            <div class="sidebar-header">
                <i class="fas fa-laptop-code"></i>
                <h2>Laptop & Desktop Acknowledgement Receipt</h2>
            </div>

            <ul class="nav-links">
                <%-- isEmployeeMode = token/signing link; isEmployeeViewMode = ?mode=empview
                     Both mean the user is an employee/HOD — hide all admin-only items --%>
                <% if (isAdminUser) { %>
                <li class="nav-item">
                    <a href="Default.aspx" class="nav-link">
                        <i class="fas fa-home"></i>
                        <span>Dashboard</span>
                    </a>
                </li>
                <li class="nav-item">
                    <a href="Agreement.aspx" class="nav-link active">
                        <i class="fas fa-file-contract"></i>
                        <span>New Acknowledgement Receipt</span>
                    </a>
                </li>
                <% } %>
                <li class="nav-item">
                    <a href="ExistingAgreements.aspx" class="nav-link">
                        <i class="fas fa-list-alt"></i>
                        <span>Acknowledgement Receipts</span>
                    </a>
                </li>
                <% if (isAdminUser) { %>
                <li class="nav-item">
                    <a href="UserManagement.aspx" class="nav-link">
                        <i class="fas fa-users"></i>
                        <span>Users</span>
                    </a>
                </li>
                <li class="nav-item">
                    <a href="ReportPage.aspx" class="nav-link">
                        <i class="fas fa-chart-bar"></i>
                        <span>Reports</span>
                    </a>
                </li>
                <% } %>
                <li class="nav-item">
                    <a href="Guideline_IT_Portal.html" class="nav-link" target="_blank">
                        <i class="fas fa-book-open"></i>
                        <span>Guideline</span>
                    </a>
                </li>

            </ul>

            <div class="nav-divider"></div>

                        <div class="user-info-sidebar">
                <div style="display: flex; align-items: center; gap: 12px;">
                    <div class="user-avatar">
                        <i class="fas fa-user"></i>
                    </div>
                    <div>
                        <div style="font-weight: 600; color: white;">
                            <asp:Label ID="lblUserRole" runat="server" Text="Administrator"></asp:Label>
                        </div>
                        <div style="font-size: 0.85rem; color: #94a3b8;">
                            <asp:Label ID="lblUserName" runat="server"></asp:Label>
                        </div>
                    </div>
                </div>
            </div>
        </aside>

        <!-- Main Content -->
        <main class="main-content">
            <!-- Header -->
            <header class="top-header">
                <div class="page-title">
                    <h1>
                        <asp:Literal ID="litHeaderTitle" runat="server"></asp:Literal>
                    </h1>
                    <p>
                        <asp:Literal ID="litHeaderDescription" runat="server"></asp:Literal>
                    </p>
                </div>
                <div class="user-profile">
                    <i class="fas fa-user-circle"></i>
                    <div>
                        <div style="font-weight: 600;">
                            <asp:Label ID="lblTopUserName" runat="server"></asp:Label>
                        </div>
                        <div style="font-size: 0.85rem; color: var(--text-secondary);">
                            <asp:Label ID="lblTopUserRole" runat="server"></asp:Label>
                        </div>
                    </div>
                </div>
            </header>

            <!-- Breadcrumb -->
            <div class="breadcrumb">
                <a href="Default.aspx">
                    <i class="fas fa-home"></i>
                    Dashboard
                </a>
                <span class="separator">/</span>
                <a href="Default.aspx">Computer Panel</a>
                <span class="separator">/</span>
                <span style="color: var(--text-secondary);">
                    <asp:Literal ID="litBreadcrumbTitle" runat="server"></asp:Literal>
                </span>
            </div>

            <!-- Status Badge -->
            <div style="margin-bottom: 24px;">
                <asp:Literal ID="litStatusBadge" runat="server"></asp:Literal>
            </div>

            <!-- Form Container -->
            <div class="form-container" id="formContainer" runat="server">
                <!-- Agreement Info Bar -->
                <div class="agreement-info-bar" id="agreementInfo" runat="server" visible="false">
                    <div class="info-item">
                        <span class="info-label">Acknowledgement Receipt Number</span>
                        <span class="info-value" id="agreementNumberDisplay" runat="server"></span>
                    </div>
                    <div class="info-item">
                        <span class="info-label">Created Date</span>
                        <span class="info-value" id="createdDateDisplay" runat="server"></span>
                    </div>
                    <div class="info-item">
                        <span class="info-label">Last Updated</span>
                        <span class="info-value" id="updatedDateDisplay" runat="server"></span>
                    </div>
                    <div class="info-item">
                        <span class="info-label">Status</span>
                        <span class="info-value">
                            <asp:Label ID="lblCurrentStatus" runat="server"></asp:Label>
                        </span>
                    </div>
                </div>

                <!-- Archive Remarks Banner — shown only when status = Archived -->
                <asp:Panel ID="pnlArchiveRemarks" runat="server" Visible="false">
                    <div style="display:flex; align-items:flex-start; gap:14px; background:rgba(239,68,68,0.08); border:1.5px solid rgba(239,68,68,0.35); border-radius:12px; padding:16px 20px; margin-bottom:20px;">
                        <div style="width:36px; height:36px; border-radius:50%; background:rgba(239,68,68,0.15); display:flex; align-items:center; justify-content:center; flex-shrink:0; margin-top:2px;">
                            <i class="fas fa-archive" style="color:#ef4444; font-size:1rem;"></i>
                        </div>
                        <div>
                            <div style="font-weight:700; color:#ef4444; font-size:0.92rem; margin-bottom:4px;">
                                <i class="fas fa-info-circle" style="margin-right:6px;"></i>Archive Reason
                            </div>
                            <asp:Label ID="lblArchiveRemarks" runat="server" 
                                style="color:var(--text-primary); font-size:0.93rem; line-height:1.6;"></asp:Label>
                        </div>
                    </div>
                </asp:Panel>

                <!-- Messages -->
                <div id="messageSuccess" runat="server" class="message message-success">
                    <i class="fas fa-check-circle"></i>
                    <span id="successText" runat="server"></span>
                </div>
                
                <div id="messageError" runat="server" class="message message-error">
                    <i class="fas fa-exclamation-circle"></i>
                    <span id="errorText" runat="server"></span>
                </div>

                <!-- ── Phase Progress Stepper ───────────────────────── -->
                <div class="phase-stepper">
                    <div class="stepper-step stepper-raise">
                        <div class="stepper-circle">1</div>
                        <div class="stepper-label">
                            <div class="stepper-title">Raise</div>
                            <div class="stepper-sub">IT Admin</div>
                        </div>
                    </div>
                    <div class="stepper-connector"></div>
                    <div class="stepper-step stepper-agree">
                        <div class="stepper-circle">2</div>
                        <div class="stepper-label">
                            <div class="stepper-title">Agree</div>
                            <div class="stepper-sub">Employee</div>
                        </div>
                    </div>
                    <div class="stepper-connector"></div>
                    <div class="stepper-step stepper-verify">
                        <div class="stepper-circle">3</div>
                        <div class="stepper-label">
                            <div class="stepper-title">Verify</div>
                            <div class="stepper-sub">IT Admin</div>
                        </div>
                    </div>
                </div>

                <!-- Form Sections -->
                <div class="form-sections">

                    <!-- ========== PHASE 1: RAISE (IT Admin) ========== -->
                    <div class="phase-container phase-raise">
                        <div class="phase-header">
                            <div class="phase-num">1</div>
                            <div class="phase-title-group">
                                <div style="display:flex;align-items:center;gap:10px;flex-wrap:wrap;margin-bottom:4px;">
                                    <div class="phase-badge phase-badge-raise">
                                        <i class="fas fa-paper-plane"></i>Phase 1 — Raise
                                    </div>
                                    <span class="phase-who">IT Admin</span>
                                </div>
                                <div class="phase-title">Raise Acknowledgement Receipt</div>
                                <div class="phase-subtitle">IT admin fills in laptop/desktop details and sends to the employee for acknowledgement receipt</div>
                            </div>
                        </div>
                        <div class="phase-content">
                    <!-- Hardware Details Section -->
                    <div class="form-section">
                        <div class="section-header">
                            <div class="section-icon">
                                <i class="fas fa-laptop"></i>
                            </div>
                            <div>
                                <div class="section-title">Laptop/Desktop Details</div>
                                <div class="section-subtitle">Enter Laptop/Desktop specifications</div>
                            </div>
                        </div>

                        <div class="form-grid">
                            <div class="form-group">
                                <label class="form-label required">Model</label>
                                <asp:DropDownList ID="ddlModel" runat="server" CssClass="form-select" 
                                    AutoPostBack="true" OnSelectedIndexChanged="ddlModel_SelectedIndexChanged" autocomplete="off">
                                    <asp:ListItem Value="">-- Select Model --</asp:ListItem>
                                </asp:DropDownList>
                                <asp:RequiredFieldValidator ID="rfvModel" runat="server" 
                                    ControlToValidate="ddlModel" InitialValue=""
                                    ErrorMessage="Please select a model" 
                                    Display="Dynamic" ForeColor="#ef4444">
                                </asp:RequiredFieldValidator>
                                
                                <!-- Other Model Panel -->
                                <asp:Panel ID="pnlOtherModel" runat="server" Visible="false" CssClass="other-model-panel">
                                    <div class="other-model-header">
                                        <i class="fas fa-plus-circle"></i>
                                        Add New Model
                                    </div>
                                    <div class="other-model-grid">
                                        <div class="form-group">
                                            <label class="form-label required">Model Name</label>
                                            <asp:TextBox ID="txtOtherModel" runat="server" CssClass="form-control" 
                                                placeholder="Enter new model name" autocomplete="one-time-code"></asp:TextBox>
                                            <asp:RequiredFieldValidator ID="rfvOtherModel" runat="server" 
                                                ControlToValidate="txtOtherModel" ErrorMessage="Please enter model name"
                                                Display="Dynamic" ForeColor="#ef4444" Enabled="false">
                                            </asp:RequiredFieldValidator>
                                        </div>
                                        
                                        <div class="form-group">
                                            <label class="form-label required">Device Type</label>
                                            <asp:DropDownList ID="ddlDeviceType" runat="server" CssClass="form-select"
                                                AutoPostBack="true" OnSelectedIndexChanged="ddlDeviceType_SelectedIndexChanged" autocomplete="off">
                                                <asp:ListItem Value="">-- Select Type --</asp:ListItem>
                                                <asp:ListItem Value="Laptop">Laptop</asp:ListItem>
                                                <asp:ListItem Value="Desktop">Desktop</asp:ListItem>
                                                <asp:ListItem Value="All-in-One">All-in-One Desktop</asp:ListItem>
                                                <asp:ListItem Value="Workstation">Workstation</asp:ListItem>
                                                <asp:ListItem Value="Tablet">Tablet</asp:ListItem>
                                                <asp:ListItem Value="Other">Other</asp:ListItem>
                                            </asp:DropDownList>
                                            <asp:RequiredFieldValidator ID="rfvDeviceType" runat="server" 
                                                ControlToValidate="ddlDeviceType" InitialValue=""
                                                ErrorMessage="Please select device type"
                                                Display="Dynamic" ForeColor="#ef4444" Enabled="false">
                                            </asp:RequiredFieldValidator>
                                        </div>
                                    </div>
                                    <small class="helper-text">This model will be added to the database for future use</small>
                                </asp:Panel>
                            </div>

                            <div class="form-group">
                                <label class="form-label required">Serial Number</label>
                                <asp:TextBox ID="txtSerialNumber" runat="server" CssClass="form-control" 
                                    placeholder="Enter serial number" autocomplete="one-time-code"></asp:TextBox>
                                <asp:RequiredFieldValidator ID="rfvSerialNumber" runat="server" 
                                    ControlToValidate="txtSerialNumber" ErrorMessage="Serial number is required"
                                    Display="Dynamic" ForeColor="#ef4444">
                                </asp:RequiredFieldValidator>
                            </div>

                            <div class="form-group">
                                <label class="form-label">Asset Number
                                    <span id="spanAssetRequired" style="color:#ef4444; display:none;"> *</span>
                                </label>
                                <asp:TextBox ID="txtAssetNumber" runat="server" CssClass="form-control" 
                                    placeholder="Get from finance for new device (optional)" autocomplete="one-time-code"></asp:TextBox>
                                <div class="helper-text">Optional during creation — required for Phase 3 IT Verification</div>
                            </div>

                        </div>
                    </div>

                    <!-- Accessories Section (Wrapped in Panel) -->
                    <asp:Panel ID="accessoriesSection" runat="server" CssClass="form-section">
                        <div class="section-header">
                            <div class="section-icon">
                                <i class="fas fa-box-open"></i>
                            </div>
                            <div>
                                <div class="section-title">Accessories</div>
                                <div class="section-subtitle">Select all included accessories</div>
                            </div>
                        </div>

                        <div class="accessories-grid">
                            <div class="checkbox-group">
                                <asp:CheckBox ID="chkCarryBag" runat="server" />
                                <label for="chkCarryBag">Bag</label>
                            </div>

                            <div class="checkbox-group">
                                <asp:CheckBox ID="chkPowerAdapter" runat="server" />
                                <label for="chkPowerAdapter">Power Adapter</label>
                            </div>

                            <div class="checkbox-group">
                                <asp:CheckBox ID="chkMouse" runat="server" />
                                <label for="chkMouse">Mouse</label>
                            </div>

                            <div class="checkbox-group">
                                <asp:CheckBox ID="chkVGAConverter" runat="server" />
                                <label for="chkVGAConverter">VGA Converter / HDMI</label>
                            </div>
                        </div>

                        <!-- Mouse Type -->
                        <div class="form-group" style="margin-top: 16px;">
                            <label class="form-label">Mouse Type</label>
                            <div class="radio-group">
                                <div class="radio-option">
                                    <asp:RadioButton ID="rbWired" runat="server" GroupName="MouseType" />
                                    <label for="rbWired">Wired</label>
                                </div>
                                <div class="radio-option">
                                    <asp:RadioButton ID="rbWireless" runat="server" GroupName="MouseType" />
                                    <label for="rbWireless">Wireless</label>
                                </div>
                            </div>
                        </div>

                        <!-- Other Accessories -->
                        <div class="form-group">
                            <label class="form-label">Other Accessories</label>
                            <asp:TextBox ID="txtOtherAccessories" runat="server" CssClass="form-control" 
                                placeholder="List any other accessories..." TextMode="MultiLine" Rows="3" autocomplete="one-time-code"></asp:TextBox>
                        </div>
                    </asp:Panel>

                    <!-- IT Details Section -->
                    <div class="form-section">
                        <div class="section-header">
                            <div class="section-icon">
                                <i class="fas fa-user-tie"></i>
                            </div>
                            <div>
                                <div class="section-title">IT Staff Details</div>
                                <div class="section-subtitle">Information about the issuing IT staff</div>
                            </div>
                        </div>

                        <div class="form-grid">
                            <div class="form-group">
                                <label class="form-label">IT Staff</label>
                                <asp:TextBox ID="txtITStaff" runat="server" CssClass="form-control auto-fill" 
                                    ReadOnly="true" autocomplete="one-time-code"></asp:TextBox>
                                <div class="helper-text">Auto-filled based on your Windows ID</div>
                            </div>

                            <div class="form-group">
                                <label class="form-label">Issue Date</label>
                                <asp:TextBox ID="txtDateIssue" runat="server" CssClass="form-control auto-fill" 
                                    ReadOnly="true" autocomplete="one-time-code"></asp:TextBox>
                                <div class="helper-text">Auto-filled on submission</div>
                            </div>
                        </div>
                    </div>

                    <!-- Employee Information -->
                    <div class="form-section">
                        <div class="section-header">
                            <div class="section-icon">
                                <i class="fas fa-user"></i>
                            </div>
                            <div>
                                <div class="section-title">Employee Information</div>
                                <div class="section-subtitle">Contact details for notification</div>
                            </div>
                        </div>

                        <div class="form-grid">
                            <div class="form-group">
                                <label class="form-label required">Employee Email</label>
                                <div class="searchable-dropdown" id="empEmailContainer">
                                    <asp:TextBox ID="txtEmployeeEmailSearch" runat="server" CssClass="form-control" 
                                        placeholder="Type to search employee email..." autocomplete="one-time-code"></asp:TextBox>
                                    <asp:HiddenField ID="hdnEmployeeEmail" runat="server" />
                                    <div class="dropdown-list" id="empEmailDropdown" style="display:none;">
                                    </div>
                                </div>
                                <asp:HiddenField ID="hdnEmployeeEmailList" runat="server" />
                                <asp:CustomValidator ID="rfvEmployeeEmail" runat="server" 
                                    ClientValidationFunction="validateEmployeeEmail"
                                    OnServerValidate="rfvEmployeeEmail_ServerValidate"
                                    ErrorMessage="Please select a valid employee email" 
                                    Display="Dynamic" ForeColor="#ef4444">
                                </asp:CustomValidator>
                            </div>

                            <div class="form-group">
                                <label class="form-label required">HOD Email</label>
                                <div class="searchable-dropdown" id="hodEmailContainer">
                                    <asp:TextBox ID="txtHODEmailSearch" runat="server" CssClass="form-control" 
                                        placeholder="Type to search HOD email..." autocomplete="one-time-code"></asp:TextBox>
                                    <asp:HiddenField ID="hdnHODEmail" runat="server" />
                                    <div class="dropdown-list" id="hodEmailDropdown" style="display:none;">
                                    </div>
                                </div>
                                <asp:HiddenField ID="hdnHODEmailList" runat="server" />
                                <asp:CustomValidator ID="rfvHODEmail" runat="server" 
                                    ClientValidationFunction="validateHODEmail"
                                    OnServerValidate="rfvHODEmail_ServerValidate"
                                    ErrorMessage="Please select a valid HOD email" 
                                    Display="Dynamic" ForeColor="#ef4444">
                                </asp:CustomValidator>
                            </div>
                        </div>
                    </div>

                    <!-- Remarks Section -->
                    <div class="form-section">
                        <div class="section-header">
                            <div class="section-icon">
                                <i class="fas fa-sticky-note"></i>
                            </div>
                            <div>
                                <div class="section-title">Remarks & Status</div>
                                <div class="section-subtitle">Additional notes and acknowledgement receipt status</div>
                            </div>
                        </div>

                        <div class="form-group">
                            <label class="form-label">Remarks</label>
                            <asp:TextBox ID="txtRemarks" runat="server" CssClass="form-control remarks-box" 
                                placeholder="Enter any remarks or notes..." TextMode="MultiLine" Rows="4" autocomplete="one-time-code"></asp:TextBox>
                        </div>

                        <!-- Status Section (only for edit mode) -->
                        <asp:Panel ID="statusSection" runat="server" CssClass="status-section" Visible="false">
                            <label class="form-label required">Acknowledgement Status</label>
                            <div class="status-options">
                                <div class="radio-option">
                                    <asp:RadioButton ID="rbActive" runat="server" GroupName="AgreementStatus" Checked="true" />
                                    <label for="rbActive">Active</label>
                                </div>
                                <div class="radio-option">
                                    <asp:RadioButton ID="rbInactive" runat="server" GroupName="AgreementStatus" />
                                    <label for="rbInactive">Inactive</label>
                                </div>
                            </div>
                        </asp:Panel>
                    </div>
                        </div> <!-- /phase-content Phase 1 -->
                    </div> <!-- /phase-container Phase 1 -->

                    <!-- ── Phase 1 → 2 animated divider ── -->
                    <div class="phase-divider phase-divider-1to2">
                        <div class="phase-divider-line"></div>
                        <div class="phase-divider-badge">
                            <i class="fas fa-arrow-down"></i>
                        </div>
                        <div class="phase-divider-chip">Then employee agrees</div>
                        <div class="phase-divider-line"></div>
                    </div>

                    <!-- ========== PHASE 2: AGREE (Employee) ========== -->
                    <div class="phase-container phase-agree" id="phaseAgree" runat="server">
                        <div class="phase-header">
                            <div class="phase-num">2</div>
                            <div class="phase-title-group">
                                <div style="display:flex;align-items:center;gap:10px;flex-wrap:wrap;margin-bottom:4px;">
                                    <div class="phase-badge phase-badge-agree">
                                        <i class="fas fa-handshake"></i>Phase 2 — Agree
                                    </div>
                                    <span class="phase-who">Employee</span>
                                </div>
                                <div class="phase-title">Employee Acknowledgement</div>
                                <div class="phase-subtitle">Employee reviews the terms, fills in personal details and submits acknowledgement receipt</div>
                            </div>
                        </div>
                        <div class="phase-content">
                    <!-- Employee Acknowledgement Section -->
                    <asp:Panel ID="pnlEmployeeSignature" runat="server" CssClass="form-section" Visible="false">
                        <div class="section-header">
                            <div class="section-icon">
                                <i class="fas fa-file-contract"></i>
                            </div>
                            <div>
                                <div class="section-title">Employee Acknowledgement</div>
                                <div class="section-subtitle">Fill in your details and review the laptop & desktop acknowledgement receipt</div>
                            </div>
                        </div>

                        <!-- Employee Information Section -->
                        <div class="employee-details-section" style="background: #f0f9ff; padding: 20px; border-radius: 8px; margin-bottom: 20px; border: 1px solid #bfdbfe;">
                            <h4 style="color: var(--primary); margin-bottom: 15px;">
                                <i class="fas fa-user-circle" style="margin-right: 8px;"></i>Employee Information
                            </h4>
                            <div class="employee-info-grid">
                                <div class="form-group">
                                    <label class="form-label required">Employee Name</label>
                                    <asp:TextBox ID="txtEmpName" runat="server" CssClass="form-control" 
                                        placeholder="Enter your full name" autocomplete="one-time-code"
                                        style="text-transform: uppercase;"></asp:TextBox>
                                    <asp:RequiredFieldValidator ID="rfvEmpName" runat="server" 
                                        ControlToValidate="txtEmpName"
                                        ErrorMessage="Employee name is required"
                                        Display="Dynamic" ForeColor="#ef4444"
                                        ValidationGroup="EmployeeValidation">
                                    </asp:RequiredFieldValidator>
                                </div>

                                <div class="form-group">
                                    <label class="form-label required">Employee ID</label>
                                    <asp:TextBox ID="txtEmpStaffId" runat="server" CssClass="form-control" 
                                        placeholder="Enter your employee/staff ID" autocomplete="one-time-code"
                                        style="text-transform: uppercase;"></asp:TextBox>
                                    <asp:RequiredFieldValidator ID="rfvEmpStaffId" runat="server" 
                                        ControlToValidate="txtEmpStaffId"
                                        ErrorMessage="Employee ID is required"
                                        Display="Dynamic" ForeColor="#ef4444"
                                        ValidationGroup="EmployeeValidation">
                                    </asp:RequiredFieldValidator>
                                </div>

                                <div class="form-group">
                                    <label class="form-label">Employee ID (Windows ID)</label>
                                    <asp:TextBox ID="txtEmpId" runat="server" CssClass="form-control" 
                                        ReadOnly="true" style="background-color: #f1f5f9;" autocomplete="one-time-code"></asp:TextBox>
                                    <small style="color: var(--text-secondary); font-size: 0.8rem;">
                                        <i class="fas fa-info-circle"></i> Automatically captured from your Windows login
                                    </small>
                                </div>

                                <div class="form-group">
                                    <label class="form-label required">Position / Job Title</label>
                                    <asp:TextBox ID="txtEmpPosition" runat="server" CssClass="form-control" 
                                        placeholder="Enter your job title" autocomplete="one-time-code"
                                        style="text-transform: uppercase;"></asp:TextBox>
                                    <asp:RequiredFieldValidator ID="rfvEmpPosition" runat="server" 
                                        ControlToValidate="txtEmpPosition"
                                        ErrorMessage="Position is required"
                                        Display="Dynamic" ForeColor="#ef4444"
                                        ValidationGroup="EmployeeValidation">
                                    </asp:RequiredFieldValidator>
                                </div>

                                <div class="form-group">
                                    <label class="form-label required">Department</label>
                                    <asp:DropDownList ID="ddlEmpDepartment" runat="server" CssClass="form-select" 
                                        autocomplete="off">
                                        <asp:ListItem Value="">-- Select Department --</asp:ListItem>
                                    </asp:DropDownList>
                                    <asp:RequiredFieldValidator ID="rfvEmpDepartment" runat="server" 
                                        ControlToValidate="ddlEmpDepartment" InitialValue=""
                                        ErrorMessage="Department is required"
                                        Display="Dynamic" ForeColor="#ef4444"
                                        ValidationGroup="EmployeeValidation">
                                    </asp:RequiredFieldValidator>
                                </div>
                            </div>
                        </div>

                        <!-- Hidden field for agreement ID -->
                        <asp:HiddenField ID="hdnAgreementId" runat="server" Value="" />

                        <!-- Submission Date and Acknowledgement -->
                        <div style="margin-top: 10px;">
                            <!-- Submission Date (auto-filled) -->
                            <div class="employee-info-grid" style="margin-top: 20px;">
                                <div class="form-group">
                                    <label class="form-label">Submission Date</label>
                                    <asp:TextBox ID="txtEmpSignatureDate" runat="server" CssClass="form-control" 
                                        ReadOnly="true" autocomplete="one-time-code"></asp:TextBox>
                                </div>
                            </div>

                            <!-- Acknowledgement Acceptance -->
                            <div class="terms-acceptance">
                                <div style="display: flex; align-items: flex-start; gap: 10px;">
                                    <asp:CheckBox ID="chkAgreeTerms" runat="server" />
                                    <div>
                                        <label for="chkAgreeTerms" style="font-weight: 600; color: #2e7d32;">
                                            I acknowledge that I have received the laptop/desktop and all listed accessories in good condition
                                        </label>
                                        <asp:CustomValidator ID="cvAgreeTerms" runat="server" 
                                            ErrorMessage="You must acknowledge the receipt"
                                            ClientValidationFunction="validateEmployeeAgreement"
                                            ValidationGroup="EmployeeValidation"
                                            Display="Dynamic" ForeColor="#ef4444">
                                        </asp:CustomValidator>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </asp:Panel>
                        </div> <!-- /phase-content Phase 2 -->
                    </div> <!-- /phase-container Phase 2 -->

                    <!-- ── Phase 2 → 3 animated divider ── -->
                    <div class="phase-divider phase-divider-2to3">
                        <div class="phase-divider-line"></div>
                        <div class="phase-divider-badge">
                            <i class="fas fa-arrow-down"></i>
                        </div>
                        <div class="phase-divider-chip">Then IT verifies</div>
                        <div class="phase-divider-line"></div>
                    </div>

                    <!-- ========== PHASE 3: VERIFY (IT Admin) ========== -->
                    <div class="phase-container phase-verify" id="phaseVerify" runat="server">
                        <div class="phase-header">
                            <div class="phase-num">3</div>
                            <div class="phase-title-group">
                                <div style="display:flex;align-items:center;gap:10px;flex-wrap:wrap;margin-bottom:4px;">
                                    <div class="phase-badge phase-badge-verify">
                                        <i class="fas fa-clipboard-check"></i>Phase 3 — Verify
                                    </div>
                                    <span class="phase-who">IT Admin</span>
                                </div>
                                <div class="phase-title">IT Verification</div>
                                <div class="phase-subtitle">IT admin verifies hardware, system configuration and completes the acknowledgement receipt</div>
                            </div>
                        </div>
                        <div class="phase-content">
                    <!-- IT Verification Section -->
                    <asp:Panel ID="pnlITVerification" runat="server" CssClass="form-section" Visible="false">
                        <div class="section-header">
                            <div class="section-icon" style="background: linear-gradient(135deg, #f59e0b, #d97706);">
                                <i class="fas fa-clipboard-check"></i>
                            </div>
                            <div>
                                <div class="section-title">IT Verification</div>
                                <div class="section-subtitle">Complete the verification checklist before finalizing the acknowledgement receipt</div>
                            </div>
                        </div>

                        <%-- Employee fields kept hidden — populated by code-behind, not shown (duplicates Phase 2) --%>
                        <div style="display:none;">
                            <asp:TextBox ID="txtVerifyEmpName"       runat="server" autocomplete="one-time-code"></asp:TextBox>
                            <asp:TextBox ID="txtVerifyEmpStaffId"   runat="server" autocomplete="one-time-code"></asp:TextBox>
                            <asp:TextBox ID="txtVerifyEmpId"         runat="server" autocomplete="one-time-code"></asp:TextBox>
                            <asp:TextBox ID="txtVerifyEmpPosition"   runat="server" autocomplete="one-time-code"></asp:TextBox>
                            <asp:TextBox ID="txtVerifyEmpDepartment" runat="server" autocomplete="one-time-code"></asp:TextBox>
                        </div>

                        <!-- Verification Checklist -->
                        <div style="background: #fffbeb; padding: 20px; border-radius: 8px; margin-bottom: 20px; border: 1px solid #fde68a;">
                            <h4 style="color: #b45309; margin-bottom: 15px;">
                                <i class="fas fa-tasks" style="margin-right: 8px;"></i>Verification Checklist
                            </h4>

                            <div class="verification-checklist">
                                <div class="checkbox-group" style="padding: 12px; background: white; border-radius: 6px; margin-bottom: 10px; border: 1px solid #e5e7eb;">
                                    <asp:CheckBox ID="chkVerifyHardware" runat="server" />
                                    <label for="chkVerifyHardware" style="font-weight: 600;">
                                        <i class="fas fa-laptop" style="color: #3b82f6; margin-right: 6px;"></i>
                                        Hardware Checklist - Updated
                                    </label>
                                    <div style="margin-left: 28px; font-size: 0.85rem; color: #6b7280;">
                                        Confirm all hardware items have been checked and documented
                                    </div>
                                </div>

                                <div class="checkbox-group" style="padding: 12px; background: white; border-radius: 6px; margin-bottom: 10px; border: 1px solid #e5e7eb;">
                                    <asp:CheckBox ID="chkVerifySystemConfig" runat="server" />
                                    <label for="chkVerifySystemConfig" style="font-weight: 600;">
                                        <i class="fas fa-cogs" style="color: #8b5cf6; margin-right: 6px;"></i>
                                        System Configuration
                                    </label>
                                    <div style="margin-left: 28px; font-size: 0.85rem; color: #6b7280;">
                                        Confirm system has been configured according to company standards
                                    </div>
                                </div>

                                <div class="form-group" style="padding: 12px; background: white; border-radius: 6px; border: 1px solid #e5e7eb;">
                                    <label class="form-label" style="font-weight: 600;">
                                        <i class="fas fa-pen" style="color: #10b981; margin-right: 6px;"></i>
                                        Others / Additional Notes
                                    </label>
                                    <asp:TextBox ID="txtVerifyOthers" runat="server" CssClass="form-control" 
                                        placeholder="Enter any additional verification notes..." TextMode="MultiLine" Rows="3" autocomplete="one-time-code"></asp:TextBox>
                                </div>
                            </div>
                        </div>

                        <!-- Reject Reason (shown only when rejecting) -->
                        <div id="rejectReasonContainer" style="display:none; background:#fef2f2; padding:20px; border-radius:8px; margin-bottom:20px; border:1px solid #fecaca;">
                            <h4 style="color:#dc2626; margin-bottom:12px;">
                                <i class="fas fa-exclamation-triangle" style="margin-right:8px;"></i>Rejection Reason
                            </h4>
                            <asp:TextBox ID="txtRejectReason" runat="server" CssClass="form-control"
                                placeholder="Describe what the employee needs to correct (e.g. wrong department, incorrect position title)..."
                                TextMode="MultiLine" Rows="3" autocomplete="one-time-code"></asp:TextBox>
                            <div style="font-size:0.82rem;color:#6b7280;margin-top:6px;">
                                <i class="fas fa-info-circle"></i>
                                This message will be included in the email sent back to the employee.
                            </div>
                        </div>

                        <!-- Verified By Info -->
                        <div class="form-grid">
                            <div class="form-group">
                                <label class="form-label">Verified By</label>
                                <asp:TextBox ID="txtVerifiedBy" runat="server" CssClass="form-control auto-fill" ReadOnly="true" autocomplete="one-time-code"></asp:TextBox>
                                <div class="helper-text">Auto-filled based on your Windows ID</div>
                            </div>
                            <div class="form-group">
                                <label class="form-label">Verification Date</label>
                                <asp:TextBox ID="txtVerifiedDate" runat="server" CssClass="form-control auto-fill" ReadOnly="true" autocomplete="one-time-code"></asp:TextBox>
                                <div class="helper-text">Auto-filled on verification</div>
                            </div>
                        </div>
                    </asp:Panel>
                        </div> <!-- /phase-content Phase 3 -->
                    </div> <!-- /phase-container Phase 3 -->
                </div>

                <!-- Action Buttons -->
                <div class="action-buttons" id="actionButtons" runat="server">
                    <asp:Button ID="btnSaveDraft" runat="server" Text="Save as Draft" 
                        CssClass="btn btn-outline" OnClick="btnSaveDraft_Click" />
                    <asp:Button ID="btnSubmit" runat="server" Text="Submit Acknowledgement Receipt" 
                        CssClass="btn btn-primary" OnClick="btnSubmit_Click" />
                    <asp:Button ID="btnEdit" runat="server" Text="Edit Acknowledgement Receipt" 
                        CssClass="btn btn-outline" OnClick="btnEdit_Click" Visible="false" />
                    <asp:Button ID="btnDelete" runat="server" Text="Archive" 
                        CssClass="btn btn-warning" OnClick="btnDelete_Click" Visible="false"
                        OnClientClick="return showArchiveModal();"
                        style="background: linear-gradient(135deg, #f59e0b, #d97706); color: white; border: none;" />
                    <asp:Button ID="btnSubmitEmployee" runat="server" Text="Submit Employee Acknowledgement" 
                        CssClass="btn btn-primary" OnClick="btnSubmitEmployee_Click" Visible="false" 
                        CausesValidation="true" ValidationGroup="EmployeeValidation" />
                    <asp:Button ID="btnVerify" runat="server" Text="Verify &amp; Complete Acknowledgement Receipt" 
                        CssClass="btn btn-primary" OnClick="btnVerify_Click" Visible="false"
                        OnClientClick="return confirm('Are you sure you want to verify and complete this acknowledgement receipt? This will send a final notification to the employee and HOD.');" />
                    <asp:Button ID="btnReject" runat="server" Text="↩ Reject — Return to Employee" 
                        CssClass="btn btn-warning" OnClick="btnReject_Click" Visible="false"
                        style="background: linear-gradient(135deg, #ef4444, #dc2626); color: white; border: none;"
                        OnClientClick="return confirmReject();" />
                    <asp:Button ID="btnAdminSave" runat="server" Text="Save" 
                        CssClass="btn btn-primary" OnClick="btnAdminSave_Click" Visible="false"
                        style="display:none;" />
                    <asp:Button ID="btnAdminSaveNotify" runat="server" Text="Save" 
                        CssClass="btn btn-primary" OnClick="btnAdminSaveNotify_Click" Visible="false"
                        OnClientClick="return confirm('Save changes? A notification email will be sent to relevant parties.');"
                        style="background: linear-gradient(135deg, #4361ee, #3a0ca3); color: white; border: none;" />
                    <asp:Button ID="btnExportPDF" runat="server" Text="Export to PDF" 
                        CssClass="btn btn-outline btn-pdf-export" OnClick="btnExportPDF_Click" Visible="false"
                        OnClientClick="return startPdfDownload(this);"
                        style="background: linear-gradient(135deg, #ef4444, #dc2626); color: white; border: none; padding: 10px 24px; font-weight: 600;" />
                </div>

                <!-- PDF Download Overlay -->
                <div id="pdfOverlay" class="pdf-overlay" style="display:none;">
                    <div class="pdf-modal">
                        <div class="pdf-spinner-container">
                            <div class="pdf-spinner">
                                <div class="pdf-spinner-ring"></div>
                                <div class="pdf-spinner-ring"></div>
                                <div class="pdf-spinner-ring"></div>
                                <i class="fas fa-file-pdf pdf-spinner-icon"></i>
                            </div>
                        </div>
                        <h3 class="pdf-modal-title">Generating PDF</h3>
                        <p class="pdf-modal-text">Preparing your acknowledgement receipt document...</p>
                        <div class="pdf-progress-bar"><div class="pdf-progress-fill"></div></div>
                    </div>
                </div>

                <!-- FEEDBACK FIX: Archive Remarks Modal -->
                <div id="archiveModal" style="display:none; position:fixed; inset:0; z-index:9999; background:rgba(15,23,42,0.7); backdrop-filter:blur(6px); align-items:center; justify-content:center;">
                    <div style="background:white; border-radius:16px; padding:36px 32px; max-width:480px; width:90%; box-shadow:0 25px 60px rgba(0,0,0,0.3); animation:pdfSlideUp 0.3s ease;">
                        <div style="display:flex; align-items:center; gap:12px; margin-bottom:20px;">
                            <div style="width:44px; height:44px; background:linear-gradient(135deg,#f59e0b,#d97706); border-radius:10px; display:flex; align-items:center; justify-content:center;">
                                <i class="fas fa-archive" style="color:white; font-size:1.1rem;"></i>
                            </div>
                            <div>
                                <h3 style="margin:0; color:#1e293b; font-size:1.1rem;">Archive Acknowledgement Receipt</h3>
                                <p style="margin:2px 0 0; color:#64748b; font-size:0.85rem;">Please provide a reason for archiving</p>
                            </div>
                        </div>
                        <label style="display:block; font-weight:600; color:#374151; margin-bottom:6px; font-size:0.9rem;">
                            Archive Reason <span style="color:#ef4444;">*</span>
                        </label>
                        <textarea id="archiveRemarksInput" rows="4" placeholder="Enter the reason for archiving this acknowledgement receipt (e.g. device returned, employee resigned, duplicate record)..."
                            style="width:100%; padding:10px 12px; border:1px solid #d1d5db; border-radius:8px; font-size:0.9rem; font-family:inherit; resize:vertical; box-sizing:border-box; outline:none; transition:border-color 0.2s;"
                            onfocus="this.style.borderColor='#f59e0b'" onblur="this.style.borderColor='#d1d5db'"></textarea>
                        <p id="archiveRemarksError" style="display:none; color:#ef4444; font-size:0.82rem; margin:6px 0 0;">
                            <i class="fas fa-exclamation-circle"></i> Please enter a reason before archiving.
                        </p>
                        <div style="display:flex; gap:12px; margin-top:20px; justify-content:flex-end;">
                            <button type="button" onclick="closeArchiveModal()"
                                style="padding:9px 20px; border:1px solid #d1d5db; border-radius:8px; background:white; color:#374151; cursor:pointer; font-size:0.9rem; font-weight:600;">
                                Cancel
                            </button>
                            <button type="button" onclick="confirmArchive()"
                                style="padding:9px 20px; border:none; border-radius:8px; background:linear-gradient(135deg,#f59e0b,#d97706); color:white; cursor:pointer; font-size:0.9rem; font-weight:600;">
                                <i class="fas fa-archive" style="margin-right:6px;"></i>Confirm Archive
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Phase 3: Asset Number Required Popup -->
            <div id="assetRequiredModal" style="display:none; position:fixed; inset:0; z-index:9999; background:rgba(15,23,42,0.7); backdrop-filter:blur(6px); align-items:center; justify-content:center;">
                <div style="background:var(--card-bg); border-radius:16px; padding:32px 28px; max-width:420px; width:90%; box-shadow:0 24px 60px rgba(0,0,0,0.3); border:2px solid #ef4444; animation:slideDown 0.25s ease-out;">
                    <div style="display:flex; align-items:center; gap:14px; margin-bottom:16px;">
                        <div style="width:44px; height:44px; border-radius:50%; background:rgba(239,68,68,0.12); display:flex; align-items:center; justify-content:center; flex-shrink:0;">
                            <i class="fas fa-exclamation-triangle" style="color:#ef4444; font-size:1.2rem;"></i>
                        </div>
                        <h3 style="margin:0; color:#ef4444; font-size:1.05rem;">Asset Number Required</h3>
                    </div>
                    <p style="margin:0 0 20px; color:var(--text-secondary); font-size:0.95rem; line-height:1.6;">
                        Please fill in the <strong style="color:var(--text-primary);">Asset Number</strong> before completing IT Verification. This field is mandatory for Phase 3 completion.
                    </p>
                    <div style="display:flex; justify-content:flex-end;">
                        <button type="button" onclick="closeAssetRequiredModal()"
                            style="padding:10px 24px; background:var(--primary); color:#fff; border:none; border-radius:8px; font-size:0.9rem; font-weight:600; cursor:pointer;">
                            <i class="fas fa-edit" style="margin-right:6px;"></i>OK, I'll fill it in
                        </button>
                    </div>
                </div>
            </div>

            <!-- Footer -->
            <div class="footer">
                <p>Laptop & Desktop Acknowledgement Receipt System &copy; <%= DateTime.Now.Year %> | Secure Enterprise Portal</p>
                <p style="margin-top: 8px; font-size: 0.8rem; color: #94a3b8;">
                    Windows Authentication | Last updated: <%= DateTime.Now.ToString("MMMM dd, yyyy HH:mm") %>
                </p>
            </div>
        </main>
    <!-- Toast Notification Container -->
    <div id="toastContainer"></div>

    </form>

    <style>
        /* ═══════════════════════════════════════════════════════
           PHASE STEPPER
        ═══════════════════════════════════════════════════════ */
        /* ═══════════════════════════════════════════════════════
           COLOR PALETTE
           Phase 1 — Blue   (#1d4ed8)  IT Admin raises agreement
           Phase 2 — Coral  (#dc2626)  Employee signs
           Phase 3 — Green  (#047857)  IT Admin verifies
        ═══════════════════════════════════════════════════════ */

        /* ── Stepper ─────────────────────────────────────────── */
        .phase-stepper {
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 0;
            margin-bottom: 32px;
            padding: 20px 28px;
            background: var(--surface-white, #fff);
            border-radius: 12px;
            border: 1.5px solid var(--border-color, #e5e7eb);
            box-shadow: 2px 2px 0 var(--ink, #0f1117);
        }
        .stepper-step {
            display: flex;
            align-items: center;
            gap: 12px;
            flex: 1;
        }
        .stepper-circle {
            width: 48px; height: 48px;
            border-radius: 50%;
            display: flex; align-items: center; justify-content: center;
            font-weight: 800; font-size: 1.1rem;
            flex-shrink: 0;
            border: 2.5px solid;
            transition: all 0.2s;
        }
        /* Phase 1 — Blue */
        .stepper-raise .stepper-circle  { background: #dbeafe; border-color: #1d4ed8; color: #1e3a8a; }
        /* Phase 2 — Coral/Red */
        .stepper-agree  .stepper-circle { background: #fee2e2; border-color: #dc2626; color: #7f1d1d; }
        /* Phase 3 — Emerald */
        .stepper-verify .stepper-circle { background: #d1fae5; border-color: #047857; color: #064e3b; }

        .stepper-label .stepper-title { font-weight: 700; font-size: 0.95rem; color: var(--text-primary, #1e293b); }
        .stepper-label .stepper-sub   { font-size: 0.78rem; color: var(--text-secondary, #64748b); font-family: 'DM Mono', monospace; letter-spacing: 0.03em; margin-top: 1px; }
        .stepper-connector {
            flex: 2;
            height: 2px;
            background: var(--border-color, #e5e7eb);
            margin: 0 8px;
            position: relative;
        }
        .stepper-connector::after {
            content: '';
            position: absolute; top: -3px; right: -1px;
            width: 8px; height: 8px;
            border-top: 2px solid var(--border-color, #cbc5b8);
            border-right: 2px solid var(--border-color, #cbc5b8);
            transform: rotate(45deg);
        }
        @media (max-width: 600px) {
            .phase-stepper { flex-direction: column; gap: 12px; }
            .stepper-connector { width: 2px; height: 24px; flex: none; margin: 0; }
            .stepper-connector::after { top: auto; bottom: -1px; right: -3px;
                border: none; border-bottom: 2px solid var(--border-color); border-right: 2px solid var(--border-color); }
        }

        /* ── Phase containers ────────────────────────────────── */
        .phase-container {
            margin-bottom: 28px;
            border-radius: 10px;
            border: 2px solid;
            overflow: hidden;
            background: var(--surface-white, #fff);
            position: relative;
        }

        /* Phase 1 — Blue */
        .phase-raise {
            border-color: #1d4ed8;
            box-shadow: 3px 3px 0 rgba(29,78,216,0.25);
        }
        /* Phase 2 — Coral */
        .phase-agree {
            border-color: #dc2626;
            box-shadow: 3px 3px 0 rgba(220,38,38,0.25);
        }
        /* Phase 3 — Emerald */
        .phase-verify {
            border-color: #047857;
            box-shadow: 3px 3px 0 rgba(4,120,87,0.25);
        }

        /* Top accent stripe — 8px, solid color */
        .phase-raise::before,
        .phase-agree::before,
        .phase-verify::before {
            content: '';
            display: block;
            height: 8px;
        }
        .phase-raise::before  { background: linear-gradient(90deg, #1d4ed8, #60a5fa); }
        .phase-agree::before  { background: linear-gradient(90deg, #dc2626, #f87171); }
        .phase-verify::before { background: linear-gradient(90deg, #047857, #34d399); }

        /* Phase headers — each a clearly distinct tinted background */
        .phase-header {
            display: flex;
            align-items: center;
            gap: 18px;
            padding: 18px 24px 16px;
            border-bottom: 2px solid;
        }
        .phase-raise .phase-header  {
            background: linear-gradient(135deg, #dbeafe, #bfdbfe);
            border-bottom-color: #93c5fd;
        }
        .phase-agree  .phase-header {
            background: linear-gradient(135deg, #fee2e2, #fecaca);
            border-bottom-color: #fca5a5;
        }
        .phase-verify .phase-header {
            background: linear-gradient(135deg, #d1fae5, #a7f3d0);
            border-bottom-color: #6ee7b7;
        }

        /* Phase content area — subtle tint so the body also feels distinct */
        .phase-raise  .phase-content { background: #f8faff; }
        .phase-agree  .phase-content { background: #fff8f8; }
        .phase-verify .phase-content { background: #f6fdf9; }

        /* Large phase number circle */
        .phase-num {
            width: 56px; height: 56px; border-radius: 50%;
            display: flex; align-items: center; justify-content: center;
            font-weight: 800; font-size: 1.4rem;
            flex-shrink: 0; border: 3px solid;
        }
        .phase-raise .phase-num  { background: #1d4ed8; border-color: #1e3a8a; color: #ffffff; }
        .phase-agree  .phase-num { background: #dc2626; border-color: #7f1d1d; color: #ffffff; }
        .phase-verify .phase-num { background: #047857; border-color: #064e3b; color: #ffffff; }

        /* Phase badge pill */
        .phase-badge {
            display: inline-flex;
            align-items: center;
            gap: 5px;
            padding: 4px 12px;
            border-radius: 20px;
            font-size: 0.75rem;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.6px;
            color: white;
            white-space: nowrap;
            font-family: 'DM Mono', monospace;
        }
        .phase-badge i { font-size: 0.7rem; }
        .phase-badge-raise  { background: #1d4ed8; }
        .phase-badge-agree  { background: #dc2626; }
        .phase-badge-verify { background: #047857; }

        .phase-title-group { flex: 1; }
        .phase-title    { font-size: 1.1rem; font-weight: 700; color: #0f172a; margin-bottom: 4px; }
        .phase-subtitle { font-size: 0.83rem; color: #475569; }

        /* Who badge */
        .phase-who {
            font-size: 0.72rem; font-family: 'DM Mono', monospace;
            font-weight: 700; letter-spacing: 0.05em;
            padding: 4px 10px; border-radius: 4px; border: 2px solid;
            text-transform: uppercase; white-space: nowrap;
        }
        .phase-raise  .phase-who { color: #1e3a8a; border-color: #93c5fd; background: #eff6ff; }
        .phase-agree  .phase-who { color: #7f1d1d; border-color: #fca5a5; background: #fff1f2; }
        .phase-verify .phase-who { color: #064e3b; border-color: #6ee7b7; background: #ecfdf5; }

        .phase-content { padding: 24px; }
        .phase-content .form-section {
            margin-bottom: 20px;
            padding: 0;
            border: none;
            box-shadow: none;
            background: transparent;
        }
        .phase-content .form-section:last-child { margin-bottom: 0; }

        /* ── Animated phase dividers ─────────────────────────── */
        .phase-divider {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 0;
            margin: 8px 0;
            position: relative;
        }
        .phase-divider-line {
            width: 2px;
            height: 40px;
            background: repeating-linear-gradient(
                to bottom,
                var(--div-color, #93c5fd) 0px,
                var(--div-color, #93c5fd) 6px,
                transparent 6px,
                transparent 12px
            );
            animation: dashFlow 1.4s linear infinite;
        }
        @keyframes dashFlow {
            0%   { background-position: 0 0; }
            100% { background-position: 0 24px; }
        }
        .phase-divider-badge {
            width: 52px; height: 52px;
            border-radius: 50%;
            display: flex; align-items: center; justify-content: center;
            font-size: 1.1rem;
            border: 2px solid;
            position: relative;
            animation: phasePulse 2.2s ease-in-out infinite;
            z-index: 1;
        }
        @keyframes phasePulse {
            0%, 100% { transform: scale(1);    box-shadow: 0 0 0 0 rgba(0,0,0,0); }
            50%       { transform: scale(1.08); box-shadow: 0 0 0 8px rgba(0,0,0,0.06); }
        }
        .phase-divider-badge::before,
        .phase-divider-badge::after {
            content: '';
            position: absolute; inset: -6px;
            border-radius: 50%;
            border: 1.5px solid;
            opacity: 0;
            animation: rippleOut 2.2s ease-out infinite;
        }
        .phase-divider-badge::after { inset: -12px; animation-delay: 0.4s; }
        @keyframes rippleOut {
            0%   { opacity: 0.5; transform: scale(1);   }
            100% { opacity: 0;   transform: scale(1.5); }
        }

        /* Divider 1→2: blue → coral transition */
        .phase-divider-1to2 { --div-color: #93c5fd; }
        .phase-divider-1to2 .phase-divider-badge {
            background: #dbeafe; border-color: #1d4ed8; color: #1e3a8a;
        }
        .phase-divider-1to2 .phase-divider-badge::before,
        .phase-divider-1to2 .phase-divider-badge::after { border-color: #93c5fd; }

        /* Divider 2→3: coral → green transition */
        .phase-divider-2to3 { --div-color: #fca5a5; }
        .phase-divider-2to3 .phase-divider-badge {
            background: #fee2e2; border-color: #dc2626; color: #7f1d1d;
        }
        .phase-divider-2to3 .phase-divider-badge::before,
        .phase-divider-2to3 .phase-divider-badge::after { border-color: #fca5a5; }

        /* Label chips */
        .phase-divider-chip {
            font-size: 0.7rem; font-family: 'DM Mono', monospace;
            font-weight: 600; letter-spacing: 0.07em;
            text-transform: uppercase;
            padding: 3px 10px; border-radius: 20px;
            margin-top: 4px;
            border: 1.5px solid;
        }
        .phase-divider-1to2 .phase-divider-chip {
            color: #1e3a8a; border-color: #93c5fd; background: #eff6ff;
        }
        .phase-divider-2to3 .phase-divider-chip {
            color: #7f1d1d; border-color: #fca5a5; background: #fff1f2;
        }

        /* ===== Wider Two-Column Layout ===== */
        .form-sections .form-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
        }
        .form-sections .employee-info-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 16px;
        }
        @media (max-width: 768px) {
            .form-sections .form-grid,
            .form-sections .employee-info-grid {
                grid-template-columns: 1fr;
            }
            .phase-header {
                flex-direction: column;
                align-items: flex-start;
                gap: 8px;
            }
        }

        /* Searchable Dropdown Styles */
        .searchable-dropdown {
            position: relative;
        }
        .searchable-dropdown .dropdown-list {
            position: absolute;
            top: 100%;
            left: 0;
            right: 0;
            max-height: 250px;
            overflow-y: auto;
            background: white;
            border: 1px solid #d1d5db;
            border-top: none;
            border-radius: 0 0 8px 8px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            z-index: 1000;
        }
        .searchable-dropdown .dropdown-item {
            padding: 10px 14px;
            cursor: pointer;
            font-size: 0.9rem;
            border-bottom: 1px solid #f3f4f6;
            transition: background-color 0.15s;
        }
        .searchable-dropdown .dropdown-item:hover,
        .searchable-dropdown .dropdown-item.highlighted {
            background-color: #eff6ff;
            color: #1e40af;
        }
        .searchable-dropdown .dropdown-item.selected {
            background-color: #dbeafe;
            font-weight: 600;
        }
        .searchable-dropdown .dropdown-no-results {
            padding: 10px 14px;
            color: #9ca3af;
            font-style: italic;
            font-size: 0.9rem;
        }
        .searchable-dropdown .form-control.has-value {
            border-color: #3b82f6;
            background-color: #f0f9ff;
        }

        /* ===== PDF Download Overlay ===== */
        .pdf-overlay {
            position: fixed; inset: 0; z-index: 9999;
            background: rgba(15, 23, 42, 0.7);
            backdrop-filter: blur(6px);
            display: flex; align-items: center; justify-content: center;
            animation: pdfFadeIn 0.3s ease;
        }
        @keyframes pdfFadeIn { from { opacity: 0; } to { opacity: 1; } }

        .pdf-modal {
            background: white; border-radius: 20px; padding: 48px 40px;
            text-align: center; box-shadow: 0 25px 60px rgba(0,0,0,0.3);
            animation: pdfSlideUp 0.4s ease; min-width: 340px;
        }
        @keyframes pdfSlideUp { from { opacity: 0; transform: translateY(30px); } to { opacity: 1; transform: translateY(0); } }

        .pdf-spinner-container { margin-bottom: 24px; }
        .pdf-spinner {
            width: 80px; height: 80px; margin: 0 auto; position: relative;
        }
        .pdf-spinner-ring {
            position: absolute; inset: 0; border-radius: 50%;
            border: 3px solid transparent;
        }
        .pdf-spinner-ring:nth-child(1) {
            border-top-color: #ef4444;
            animation: pdfSpin 1.2s linear infinite;
        }
        .pdf-spinner-ring:nth-child(2) {
            inset: 6px; border-right-color: #f59e0b;
            animation: pdfSpin 1.6s linear infinite reverse;
        }
        .pdf-spinner-ring:nth-child(3) {
            inset: 12px; border-bottom-color: #4361ee;
            animation: pdfSpin 2s linear infinite;
        }
        @keyframes pdfSpin { to { transform: rotate(360deg); } }

        .pdf-spinner-icon {
            position: absolute; inset: 0; display: flex;
            align-items: center; justify-content: center;
            font-size: 1.6rem; color: #ef4444;
            animation: pdfPulse 1.5s ease-in-out infinite;
        }
        @keyframes pdfPulse { 0%, 100% { transform: scale(1); opacity: 1; } 50% { transform: scale(1.15); opacity: 0.7; } }

        .pdf-modal-title {
            font-size: 1.2rem; font-weight: 700; color: #1e293b; margin: 0 0 8px;
        }
        .pdf-modal-text {
            font-size: 0.9rem; color: #64748b; margin: 0 0 20px;
        }
        .pdf-progress-bar {
            width: 100%; height: 4px; background: #e5e7eb;
            border-radius: 4px; overflow: hidden;
        }
        .pdf-progress-fill {
            height: 100%; width: 0;
            background: linear-gradient(90deg, #ef4444, #f59e0b, #4361ee);
            border-radius: 4px;
            animation: pdfProgress 2.5s ease-in-out forwards;
        }
        @keyframes pdfProgress { 0% { width: 0; } 60% { width: 70%; } 100% { width: 95%; } }

        .pdf-overlay.pdf-complete .pdf-spinner-icon {
            color: #10b981; animation: none;
        }
        .pdf-overlay.pdf-complete .pdf-spinner-ring { animation-play-state: paused; }
        .pdf-overlay.pdf-complete .pdf-progress-fill { width: 100% !important; background: #10b981; animation: none; transition: width 0.3s; }
        .pdf-overlay.pdf-complete .pdf-modal-title { color: #10b981; }

        .btn-pdf-export {
            position: relative; overflow: hidden;
        }
        .btn-pdf-export::before {
            content: '\f1c1'; font-family: 'Font Awesome 6 Free'; font-weight: 900;
            margin-right: 8px;
        }
        .btn-pdf-export::after {
            content: 'Export PDF';
        }

        /* ═══════════════════════════════════════════════════════
           TOAST NOTIFICATION SYSTEM
        ═══════════════════════════════════════════════════════ */
        #toastContainer {
            position: fixed;
            top: 24px; right: 24px;
            z-index: 999999;
            display: flex; flex-direction: column; gap: 12px;
            pointer-events: none;
        }
        .toast {
            pointer-events: all;
            display: flex; align-items: flex-start; gap: 14px;
            padding: 16px 18px 18px;
            background: white;
            border-radius: 12px;
            box-shadow: 0 8px 32px rgba(0,0,0,0.14), 0 2px 8px rgba(0,0,0,0.07);
            border-left: 4px solid;
            min-width: 320px; max-width: 440px;
            animation: toastSlideIn 0.35s cubic-bezier(0.34,1.56,0.64,1) forwards;
            position: relative; overflow: hidden;
        }
        .toast.toast-leaving { animation: toastSlideOut 0.28s ease forwards; }
        @keyframes toastSlideIn {
            from { opacity: 0; transform: translateX(110px) scale(0.92); }
            to   { opacity: 1; transform: translateX(0) scale(1); }
        }
        @keyframes toastSlideOut {
            from { opacity: 1; transform: translateX(0); max-height: 120px; }
            to   { opacity: 0; transform: translateX(110px); max-height: 0; padding: 0; margin: 0; }
        }
        .toast-success { border-color: #10b981; }
        .toast-error   { border-color: #ef4444; }
        .toast-warning { border-color: #f59e0b; }
        .toast-info    { border-color: #3b82f6; }
        .toast-icon {
            width: 38px; height: 38px; border-radius: 50%;
            display: flex; align-items: center; justify-content: center;
            flex-shrink: 0; font-size: 1rem;
        }
        .toast-success .toast-icon { background: #ecfdf5; color: #10b981; }
        .toast-error   .toast-icon { background: #fef2f2; color: #ef4444; }
        .toast-warning .toast-icon { background: #fffbeb; color: #f59e0b; }
        .toast-info    .toast-icon { background: #eff6ff; color: #3b82f6; }
        .toast-body    { flex: 1; min-width: 0; }
        .toast-title   { font-weight: 700; font-size: 0.92rem; color: #111827; margin-bottom: 2px; }
        .toast-msg     { font-size: 0.84rem; color: #4b5563; line-height: 1.45; word-break: break-word; }
        .toast-close   {
            background: none; border: none; cursor: pointer;
            color: #9ca3af; font-size: 0.95rem; padding: 0 0 0 4px; line-height: 1; flex-shrink: 0;
        }
        .toast-close:hover { color: #4b5563; }
        .toast-progress {
            position: absolute; bottom: 0; left: 0; height: 3px; border-radius: 0 0 12px 0;
        }
        .toast-success .toast-progress { background: #10b981; }
        .toast-error   .toast-progress { background: #ef4444; }
        .toast-warning .toast-progress { background: #f59e0b; }
        .toast-info    .toast-progress { background: #3b82f6; }

        /* ═══════════════════════════════════════════════════════
           DARK MODE OVERRIDES — Agreement.aspx inline styles
           =================================================== */
        [data-theme="dark"] .phase-container {
            background: #1a2234;
            border-color: rgba(255,255,255,0.08);
        }
        [data-theme="dark"] .phase-header {
            border-bottom-color: rgba(255,255,255,0.07);
        }
        /* Phase dark mode — see updated overrides below */

        [data-theme="dark"] .searchable-dropdown .dropdown-list {
            background: #111827; border-color: rgba(255,255,255,0.1);
            box-shadow: 0 8px 24px rgba(0,0,0,0.5);
        }
        [data-theme="dark"] .searchable-dropdown .dropdown-item {
            color: #e2e8f0; border-bottom-color: rgba(255,255,255,0.05);
        }
        [data-theme="dark"] .searchable-dropdown .dropdown-item:hover,
        [data-theme="dark"] .searchable-dropdown .dropdown-item.highlighted {
            background: #1e3a5f; color: #93c5fd;
        }
        [data-theme="dark"] .searchable-dropdown .dropdown-item.selected {
            background: #1e3560; color: #bfdbfe; font-weight: 600;
        }
        [data-theme="dark"] .searchable-dropdown .form-control.has-value {
            border-color: #4c6ef5; background: #111827;
        }

        /* Employee details blue box */
        [data-theme="dark"] .employee-details-section {
            background: #0f1e2e !important;
            border-color: rgba(96,165,250,0.2) !important;
        }
        [data-theme="dark"] .employee-details-section h4 { color: #93c5fd !important; }

        /* Read-only inputs */
        [data-theme="dark"] input[readonly],
        [data-theme="dark"] textarea[readonly] {
            background: #1a2234 !important; color: #94a3b8 !important;
            border-color: rgba(255,255,255,0.08) !important;
        }

        /* Terms amber box */
        [data-theme="dark"] [style*="#fffbeb"],
        [data-theme="dark"] [style*="fef3c7"] {
            background: #1e1600 !important;
            border-color: rgba(251,191,36,0.25) !important;
        }
        [data-theme="dark"] [style*="b45309"] { color: #fde68a !important; }

        /* Checkbox groups */
        [data-theme="dark"] .checkbox-group {
            background: #1a2234 !important;
            border-color: rgba(255,255,255,0.08) !important;
        }
        [data-theme="dark"] .checkbox-label { color: #e2e8f0 !important; }
        [data-theme="dark"] [style*="#6b7280"] { color: #94a3b8 !important; }

        /* Green verify box */
        [data-theme="dark"] [style*="#f0fdf4"],
        [data-theme="dark"] [style*="bbf7d0"] {
            background: #001a0e !important;
            border-color: rgba(52,211,153,0.2) !important;
        }
        [data-theme="dark"] [style*="#15803d"],
        [data-theme="dark"] [style*="2e7d32"] { color: #6ee7b7 !important; }

        /* Agree label */
        [data-theme="dark"] label[for="chkAgreeTerms"] { color: #6ee7b7 !important; }

        /* Breadcrumb */
        [data-theme="dark"] .breadcrumb { color: #94a3b8; }
        [data-theme="dark"] .breadcrumb a { color: #93c5fd; }

        /* Helper text */
        [data-theme="dark"] small { color: #64748b; }

        /* PDF modal */
        [data-theme="dark"] .pdf-modal { background: #1a2234; color: #e2e8f0; }
        [data-theme="dark"] .pdf-modal h3 { color: #e2e8f0; }
        [data-theme="dark"] .pdf-modal p  { color: #94a3b8; }

        /* Dark mode: phase containers */
        [data-theme="dark"] .phase-raise  { border-color: #3b82f6; box-shadow: 3px 3px 0 rgba(59,130,246,0.2); }
        [data-theme="dark"] .phase-agree  { border-color: #f87171; box-shadow: 3px 3px 0 rgba(248,113,113,0.2); }
        [data-theme="dark"] .phase-verify { border-color: #34d399; box-shadow: 3px 3px 0 rgba(52,211,153,0.2); }

        [data-theme="dark"] .phase-raise .phase-header  { background: linear-gradient(135deg, #1e3060, #172554); border-bottom-color: #1d4ed8; }
        [data-theme="dark"] .phase-agree  .phase-header { background: linear-gradient(135deg, #3b0f0f, #450a0a); border-bottom-color: #dc2626; }
        [data-theme="dark"] .phase-verify .phase-header { background: linear-gradient(135deg, #052e16, #064e3b); border-bottom-color: #047857; }

        [data-theme="dark"] .phase-raise  .phase-content { background: #0f1e3a; }
        [data-theme="dark"] .phase-agree  .phase-content { background: #1f0a0a; }
        [data-theme="dark"] .phase-verify .phase-content { background: #031a0e; }

        [data-theme="dark"] .phase-raise  .phase-num { background: #1d4ed8; border-color: #3b82f6; color: #fff; }
        [data-theme="dark"] .phase-agree  .phase-num { background: #dc2626; border-color: #f87171; color: #fff; }
        [data-theme="dark"] .phase-verify .phase-num { background: #047857; border-color: #34d399; color: #fff; }

        [data-theme="dark"] .phase-raise  .phase-who { color: #bfdbfe; border-color: #1d4ed8; background: #172554; }
        [data-theme="dark"] .phase-agree  .phase-who { color: #fecaca; border-color: #dc2626; background: #450a0a; }
        [data-theme="dark"] .phase-verify .phase-who { color: #a7f3d0; border-color: #047857; background: #064e3b; }

        [data-theme="dark"] .phase-title   { color: #f1f5f9; }
        [data-theme="dark"] .phase-subtitle { color: #94a3b8; }

        /* Dark mode: stepper */
        [data-theme="dark"] .stepper-raise  .stepper-circle { background: #172554; border-color: #3b82f6; color: #93c5fd; }
        [data-theme="dark"] .stepper-agree  .stepper-circle { background: #450a0a; border-color: #f87171; color: #fca5a5; }
        [data-theme="dark"] .stepper-verify .stepper-circle { background: #064e3b; border-color: #34d399; color: #6ee7b7; }

        /* Dark mode: phase dividers */
        [data-theme="dark"] .phase-divider-1to2 .phase-divider-badge { background: #172554; border-color: #3b82f6; color: #93c5fd; }
        [data-theme="dark"] .phase-divider-1to2 .phase-divider-chip  { background: #172554; border-color: #1d4ed8; color: #93c5fd; }
        [data-theme="dark"] .phase-divider-2to3 .phase-divider-badge { background: #450a0a; border-color: #f87171; color: #fca5a5; }
        [data-theme="dark"] .phase-divider-2to3 .phase-divider-chip  { background: #450a0a; border-color: #dc2626; color: #fca5a5; }

        /* Dark mode: toasts */
        [data-theme="dark"] .toast { background: #1e2d40; border-left-color: inherit; }
        [data-theme="dark"] .toast-title { color: #e2e8f0; }
        [data-theme="dark"] .toast-msg   { color: #94a3b8; }
        [data-theme="dark"] .toast-close { color: #4b5563; }
        [data-theme="dark"] .toast-close:hover { color: #94a3b8; }
        [data-theme="dark"] .toast-success .toast-icon { background: rgba(16,185,129,0.15); }
        [data-theme="dark"] .toast-error   .toast-icon { background: rgba(239,68,68,0.15);  }
        [data-theme="dark"] .toast-warning .toast-icon { background: rgba(245,158,11,0.15); }
        [data-theme="dark"] .toast-info    .toast-icon { background: rgba(59,130,246,0.15); }

        /* Dark mode: stepper */
        [data-theme="dark"] .phase-stepper { background: #141e2e; border-color: rgba(255,255,255,0.08); }
        [data-theme="dark"] .stepper-connector { background: rgba(255,255,255,0.1); }
        [data-theme="dark"] .stepper-connector::after { border-color: rgba(255,255,255,0.1); }
        [data-theme="dark"] .phase-num { background: #1a2234; }

    </style>

    <script>
        // ── Theme init ─────────────────────────────────────────────────────────
        (function(){
            document.documentElement.setAttribute('data-theme',
                localStorage.getItem('portalTheme') || 'light');
        })();

        // ── Global validation functions — MUST be global for ASP.NET CustomValidator ──
        // Bug: previously defined inside DOMContentLoaded so ASP.NET couldn't find them
        function validateEmployeeEmail(sender, args) {
            var hidden = document.getElementById('<%= hdnEmployeeEmail.ClientID %>');
            args.IsValid = hidden != null && hidden.value.trim() !== '';
        }
        function validateHODEmail(sender, args) {
            var hidden = document.getElementById('<%= hdnHODEmail.ClientID %>');
            args.IsValid = hidden != null && hidden.value.trim() !== '';
        }
        function validateEmployeeAgreement(sender, args) {
            var chk = document.getElementById('<%= chkAgreeTerms.ClientID %>');
            args.IsValid = chk != null && chk.checked;
        }

        document.addEventListener('DOMContentLoaded', function () {

            function applyTheme(theme) {
                document.documentElement.setAttribute('data-theme', theme);
                localStorage.setItem('portalTheme', theme);
                var btn = document.getElementById('themeToggleBtn');
                if (btn) btn.innerHTML = theme === 'dark'
                    ? '<i class="fas fa-sun"></i><span>Light Mode</span>'
                    : '<i class="fas fa-moon"></i><span>Dark Mode</span>';
            }
            applyTheme(localStorage.getItem('portalTheme') || 'light');

            var themeBtn = document.getElementById('themeToggleBtn');
            if (themeBtn) {
                themeBtn.addEventListener('click', function () {
                    var cur = document.documentElement.getAttribute('data-theme') || 'light';
                    applyTheme(cur === 'dark' ? 'light' : 'dark');
                });
            }


            // ── Reject confirmation — shows reason box, waits for user to confirm ─
            window.confirmReject = function () {
                var container = document.getElementById('rejectReasonContainer');
                if (!container) return true; // fallback

                if (container.style.display === 'none') {
                    // First click: reveal the reason textarea, don't submit yet
                    container.style.display = 'block';
                    container.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    var txt = container.querySelector('textarea');
                    if (txt) txt.focus();

                    // Change button text to signal second click confirms
                    var btn = document.querySelector('[id$="btnReject"]');
                    if (btn) {
                        btn.value = '⚠ Confirm Rejection & Notify Employee';
                        btn.style.boxShadow = '0 0 0 3px rgba(239,68,68,.35)';
                    }
                    return false; // block postback on first click
                }

                // Second click: validate reason is filled
                var txt = container.querySelector('textarea');
                if (!txt || txt.value.trim() === '') {
                    txt.style.borderColor = '#dc2626';
                    txt.placeholder = 'Please describe the reason for rejection before confirming.';
                    txt.focus();
                    return false;
                }

                return confirm('Reject this acknowledgement receipt and send it back to the employee for correction?\n\nThe employee will receive a new signing link along with your rejection reason.');
            };


            // Both Employee and HOD dropdowns are wired identically.
            // The server renders the full email list as JSON into the hidden fields
            // hdnEmployeeEmailList and hdnHODEmailList.  We read that JSON once and
            // use it for client-side filtering — no extra round-trips needed.

            function setupEmailDropdown(cfg) {
                // cfg: { inputId, dropdownId, hiddenId, listHiddenId }
                var input    = document.getElementById(cfg.inputId);
                var dropdown = document.getElementById(cfg.dropdownId);
                var hidden   = document.getElementById(cfg.hiddenId);
                var listEl   = document.getElementById(cfg.listHiddenId);

                if (!input || !dropdown || !hidden || !listEl) return;

                // Parse the JSON list injected by the server
                var allEmails = [];
                try { allEmails = JSON.parse(listEl.value || '[]'); } catch(e) {}

                var highlighted = -1;

                function showDropdown(items) {
                    dropdown.innerHTML = '';
                    highlighted = -1;

                    if (items.length === 0) {
                        dropdown.innerHTML = '<div class="dropdown-no-results"><i class="fas fa-search"></i> No results found</div>';
                        dropdown.style.display = 'block';
                        return;
                    }

                    items.slice(0, 50).forEach(function(item, idx) {
                        var div = document.createElement('div');
                        div.className = 'dropdown-item';
                        if (item.value === hidden.value) div.classList.add('selected');

                        // Highlight the matched part
                        var q   = input.value.trim().toLowerCase();
                        var txt = item.text;
                        if (q) {
                            var lo = txt.toLowerCase().indexOf(q);
                            if (lo !== -1) {
                                txt = txt.substring(0, lo)
                                    + '<strong>' + txt.substring(lo, lo + q.length) + '</strong>'
                                    + txt.substring(lo + q.length);
                            }
                        }
                        div.innerHTML = '<i class="fas fa-envelope" style="margin-right:8px;color:var(--primary);opacity:.6;font-size:.8rem;"></i>' + txt;

                        div.addEventListener('mousedown', function(e) {
                            e.preventDefault(); // prevent input blur firing first
                            selectItem(item);
                        });
                        dropdown.appendChild(div);
                    });

                    if (items.length > 50) {
                        var hint = document.createElement('div');
                        hint.className = 'dropdown-no-results';
                        hint.innerHTML = '<i class="fas fa-info-circle"></i> ' + (items.length - 50) + ' more — keep typing to narrow down';
                        dropdown.appendChild(hint);
                    }

                    dropdown.style.display = 'block';
                }

                function hideDropdown() {
                    dropdown.style.display = 'none';
                    highlighted = -1;
                }

                function selectItem(item) {
                    input.value   = item.text;
                    hidden.value  = item.value;
                    input.classList.add('has-value');
                    hideDropdown();
                }

                function filterEmails(q) {
                    if (!q) { showDropdown(allEmails); return; }
                    var lower = q.toLowerCase();
                    var hits  = allEmails.filter(function(e) {
                        return e.text.toLowerCase().indexOf(lower) !== -1;
                    });
                    showDropdown(hits);
                }

                // Keyboard navigation
                function moveHighlight(dir) {
                    var items = dropdown.querySelectorAll('.dropdown-item');
                    if (!items.length) return;
                    items[highlighted] && items[highlighted].classList.remove('highlighted');
                    highlighted = Math.max(0, Math.min(items.length - 1, highlighted + dir));
                    var el = items[highlighted];
                    el.classList.add('highlighted');
                    el.scrollIntoView({ block: 'nearest' });
                }

                input.addEventListener('focus', function() {
                    filterEmails(this.value.trim());
                });

                input.addEventListener('input', function() {
                    // If user clears the box, also clear the hidden value
                    if (!this.value.trim()) {
                        hidden.value = '';
                        input.classList.remove('has-value');
                    }
                    filterEmails(this.value.trim());
                });

                input.addEventListener('keydown', function(e) {
                    if (dropdown.style.display === 'none') {
                        if (e.key === 'ArrowDown') { filterEmails(this.value.trim()); return; }
                    }
                    if (e.key === 'ArrowDown')  { e.preventDefault(); moveHighlight(1); }
                    if (e.key === 'ArrowUp')    { e.preventDefault(); moveHighlight(-1); }
                    if (e.key === 'Escape')     { hideDropdown(); }
                    if (e.key === 'Enter' || e.key === 'Tab') {
                        var items = dropdown.querySelectorAll('.dropdown-item');
                        if (highlighted >= 0 && items[highlighted] && !items[highlighted].classList.contains('dropdown-no-results')) {
                            e.preventDefault();
                            items[highlighted].dispatchEvent(new Event('mousedown'));
                        } else if (items.length === 1 && !items[0].classList.contains('dropdown-no-results')) {
                            e.preventDefault();
                            items[0].dispatchEvent(new Event('mousedown'));
                        }
                    }
                });

                input.addEventListener('blur', function() {
                    // Small delay to allow mousedown on dropdown item to fire first
                    setTimeout(hideDropdown, 180);
                });

                // Close on outside click
                document.addEventListener('click', function(e) {
                    if (!input.contains(e.target) && !dropdown.contains(e.target)) {
                        hideDropdown();
                    }
                });
            }

            // Wire up both dropdowns using the ASP.NET-generated client IDs
            setupEmailDropdown({
                inputId:      '<%= txtEmployeeEmailSearch.ClientID %>',
                dropdownId:   'empEmailDropdown',
                hiddenId:     '<%= hdnEmployeeEmail.ClientID %>',
                listHiddenId: '<%= hdnEmployeeEmailList.ClientID %>'
            });
            setupEmailDropdown({
                inputId:      '<%= txtHODEmailSearch.ClientID %>',
                dropdownId:   'hodEmailDropdown',
                hiddenId:     '<%= hdnHODEmail.ClientID %>',
                listHiddenId: '<%= hdnHODEmailList.ClientID %>'
            });

            // ── Validation functions moved to global scope (below) ─────────────
            // ASP.NET CustomValidator ClientValidationFunction must be global.


            // Mobile sidebar
            var sidebarToggle = document.createElement('button');
            sidebarToggle.innerHTML = '<i class="fas fa-bars"></i>';
            sidebarToggle.className = 'sidebar-toggle';
            document.body.appendChild(sidebarToggle);
            sidebarToggle.addEventListener('click', function(){
                document.querySelector('.sidebar').classList.toggle('mobile-open');
                this.style.transform = document.querySelector('.sidebar').classList.contains('mobile-open') ? 'rotate(90deg)' : '';
            });
            function handleResize(){
                var btn = document.querySelector('.sidebar-toggle');
                var sb  = document.querySelector('.sidebar');
                if (!btn) return;
                btn.style.display = window.innerWidth <= 768 ? 'flex' : 'none';
                if (window.innerWidth > 768) sb.classList.remove('mobile-open');
            }
            window.addEventListener('resize', handleResize);
            handleResize();

            // ── FEEDBACK FIX: Archive Remarks Modal ─────────────────────────
            window.showArchiveModal = function () {
                var modal = document.getElementById('archiveModal');
                modal.style.display = 'flex';
                document.getElementById('archiveRemarksInput').value = '';
                document.getElementById('archiveRemarksError').style.display = 'none';
                setTimeout(function () { document.getElementById('archiveRemarksInput').focus(); }, 100);
                return false; // prevent default postback
            };

            window.closeArchiveModal = function () {
                document.getElementById('archiveModal').style.display = 'none';
            };

            window.confirmArchive = function () {
                var remarks = document.getElementById('archiveRemarksInput').value.trim();
                var errEl = document.getElementById('archiveRemarksError');
                if (!remarks) {
                    errEl.style.display = 'block';
                    document.getElementById('archiveRemarksInput').focus();
                    return;
                }
                errEl.style.display = 'none';
                // Store remarks in the hidden field
                var hdn = document.getElementById('<%= hdnArchiveRemarks.ClientID %>');
                if (hdn) hdn.value = remarks;
                // Close modal and trigger postback
                document.getElementById('archiveModal').style.display = 'none';
                __doPostBack('<%= btnDelete.UniqueID %>', '');
            };

            // Close archive modal on backdrop click
            document.getElementById('archiveModal').addEventListener('click', function (e) {
                if (e.target === this) closeArchiveModal();
            });

            // ── Phase 3: Asset Number Required Modal ─────────────────────────
            window.closeAssetRequiredModal = function () {
                document.getElementById('assetRequiredModal').style.display = 'none';
                // Focus the asset number field so user can fill it immediately
                var assetField = document.getElementById('<%= txtAssetNumber.ClientID %>');
                if (assetField) {
                    assetField.focus();
                    assetField.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    assetField.style.borderColor = '#ef4444';
                    assetField.style.boxShadow = '0 0 0 3px rgba(239,68,68,0.2)';
                }
            };
            document.getElementById('assetRequiredModal').addEventListener('click', function (e) {
                if (e.target === this) closeAssetRequiredModal();
            });

            // Show/hide the Phase 3 required asterisk on asset number based on form state
            // (set by server-side via a flag class on the field)
            (function () {
                var assetField = document.getElementById('<%= txtAssetNumber.ClientID %>');
                var asterisk  = document.getElementById('spanAssetRequired');
                if (assetField && asterisk) {
                    // If the field is editable and we're in verify mode, show the asterisk
                    if (!assetField.readOnly && !assetField.disabled &&
                        document.getElementById('<%= btnVerify.ClientID %>') !== null) {
                        asterisk.style.display = 'inline';
                    }
                    // Remove red highlight once user starts typing
                    assetField.addEventListener('input', function () {
                        if (assetField.value.trim()) {
                            assetField.style.borderColor = '';
                            assetField.style.boxShadow  = '';
                        }
                    });
                }
            })();

            // Support email

            // Parallax
            window.addEventListener('scroll', function(){
                var s = window.pageYOffset;
                document.querySelectorAll('.floating-icon').forEach(function(ic,i){
                    ic.style.transform = 'translateY('+(-s*(0.5+i*0.1))+'px) rotate('+(s*0.1)+'deg)';
                });
            });
        });

        // ═══════════════════════════════════════════════════════════════
        // TOAST NOTIFICATION SYSTEM  (global — fires from C# RegisterStartupScript)
        // ═══════════════════════════════════════════════════════════════
        window.showToast = function(type, title, message, duration) {
            duration = duration || 5500;
            var container = document.getElementById('toastContainer');
            if (!container) return;
            var icons = { success:'fa-check-circle', error:'fa-times-circle',
                          warning:'fa-exclamation-triangle', info:'fa-info-circle' };
            var pid = 'tp_' + Date.now() + '_' + Math.floor(Math.random()*9999);
            var toast = document.createElement('div');
            toast.className = 'toast toast-' + type;
            toast.innerHTML =
                '<div class="toast-icon"><i class="fas ' + (icons[type]||'fa-bell') + '"></i></div>' +
                '<div class="toast-body">' +
                  (title   ? '<div class="toast-title">' + title   + '</div>' : '') +
                  (message ? '<div class="toast-msg">'   + message + '</div>' : '') +
                '</div>' +
                '<button class="toast-close" type="button"><i class="fas fa-times"></i></button>' +
                '<div class="toast-progress" id="' + pid + '"></div>';
            container.appendChild(toast);

            toast.querySelector('.toast-close').addEventListener('click', function() {
                toast.classList.add('toast-leaving');
                setTimeout(function() { if (toast.parentElement) toast.remove(); }, 290);
            });

            var prog = document.getElementById(pid);
            if (prog) {
                prog.style.width = '100%';
                requestAnimationFrame(function() { requestAnimationFrame(function() {
                    prog.style.transition = 'width ' + duration + 'ms linear';
                    prog.style.width = '0%';
                }); });
            }

            var timer = setTimeout(function() {
                toast.classList.add('toast-leaving');
                setTimeout(function() { if (toast.parentElement) toast.remove(); }, 290);
            }, duration);

            toast.addEventListener('mouseenter', function() {
                clearTimeout(timer);
                if (prog) { prog.style.transition = 'none'; }
            });
            toast.addEventListener('mouseleave', function() {
                var rem = prog ? (parseFloat(prog.style.width) / 100) * duration : 1500;
                if (prog) { prog.style.transition = 'width ' + rem + 'ms linear'; prog.style.width = '0%'; }
                timer = setTimeout(function() {
                    toast.classList.add('toast-leaving');
                    setTimeout(function() { if (toast.parentElement) toast.remove(); }, 290);
                }, rem);
            });
        };

        // ── PDF export overlay ─────────────────────────────────────────
        function startPdfDownload(btn) {
            var overlay = document.getElementById('pdfOverlay');
            if (overlay) overlay.style.display = 'flex';
            setTimeout(function() { if (overlay) overlay.style.display = 'none'; }, 8000);
            return true;
        }

        document.querySelectorAll('input, textarea, select').forEach(function(el) {
            el.setAttribute('autocomplete', 'off');
            el.setAttribute('autocomplete', 'one-time-code');
            el.setAttribute('data-lpignore', 'true');
            el.setAttribute('data-form-type', 'other');
        });

    </script>

    <!-- Theme toggle — floating button, always visible at any zoom -->
    <button id="themeToggleBtn" class="theme-toggle" type="button">
        <i class="fas fa-moon"></i><span>Dark Mode</span>
    </button>

</body>
</html>
