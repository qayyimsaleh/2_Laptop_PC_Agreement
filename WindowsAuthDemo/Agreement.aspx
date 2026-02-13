<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Agreement.aspx.cs" Inherits="WindowsAuthDemo.Agreement" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title><asp:Literal ID="litPageTitle" runat="server"></asp:Literal></title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
    <link rel="stylesheet" href="hardware-portal-styles.css">

</head>
<body>
    <form id="form1" runat="server">
        <!-- Hidden fields for backup -->
        <asp:HiddenField ID="hdnEmpNameBackup" runat="server" />
        <asp:HiddenField ID="hdnEmpPositionBackup" runat="server" />
        <asp:HiddenField ID="hdnEmpDepartmentBackup" runat="server" />
        
        <!-- Sidebar -->
        <aside class="sidebar">
            <div class="sidebar-header">
                <i class="fas fa-laptop-code"></i>
                <h2>Laptop/PC Portal</h2>
            </div>

            <ul class="nav-links">
                <li class="nav-item">
                    <a href="Default.aspx" class="nav-link">
                        <i class="fas fa-home"></i>
                        <span>Dashboard</span>
                    </a>
                </li>
                <li class="nav-item">
                    <a href="Agreement.aspx" class="nav-link active">
                        <i class="fas fa-file-contract"></i>
                        <span>New Agreement</span>
                    </a>
                </li>
                <li class="nav-item">
                    <a href="ExistingAgreements.aspx" class="nav-link">
                        <i class="fas fa-list-alt"></i>
                        <span>Agreements</span>
                    </a>
                </li>
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
                <li class="nav-item">
                    <a href="#" class="nav-link">
                        <i class="fas fa-cog"></i>
                        <span>Settings</span>
                    </a>
                </li>
            </ul>

            <div class="nav-divider"></div>

            <div class="nav-links">
                <div class="nav-item">
                    <a href="mailto:qayyim@ioioleo.com?subject=Laptop_PC%20Agreement%20Portal%20Support&body=Hello%20Support%20Team,%0A%0AI%20need%20assistance%20with:%0A%0A%0A%0AWindows%20ID:%20[Your%20Windows%20ID]%0APage:%20[Current%20Page]" 
                       class="nav-link" 
                       onclick="return setEmailBody(this)">
                        <i class="fas fa-question-circle"></i>
                        <span>Help & Support</span>
                    </a>
                </div>
                <div class="nav-item">
                    <a href="#" class="nav-link">
                        <i class="fas fa-sign-out-alt"></i>
                        <span>Logout</span>
                    </a>
                </div>
            </div>

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
                        <span class="info-label">Agreement Number</span>
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

                <!-- Messages -->
                <div id="messageSuccess" runat="server" class="message message-success">
                    <i class="fas fa-check-circle"></i>
                    <span id="successText" runat="server"></span>
                </div>
                
                <div id="messageError" runat="server" class="message message-error">
                    <i class="fas fa-exclamation-circle"></i>
                    <span id="errorText" runat="server"></span>
                </div>

                <!-- Form Sections -->
                <div class="form-sections">

                    <!-- ========== PHASE 1: RAISE (IT Admin) ========== -->
                    <div class="phase-container phase-raise">
                        <div class="phase-header">
                            <div class="phase-badge phase-badge-raise">
                                <i class="fas fa-paper-plane"></i>
                                <span>Phase 1</span>
                            </div>
                            <div class="phase-title-group">
                                <div class="phase-title">Raise Agreement</div>
                                <div class="phase-subtitle">IT admin fills in laptop/pc details and submits to employee</div>
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
                                <div class="section-title">Laptop/PC Details</div>
                                <div class="section-subtitle">Enter Laptop/PC specifications</div>
                            </div>
                        </div>

                        <div class="form-grid">
                            <div class="form-group">
                                <label class="form-label required">Model</label>
                                <asp:DropDownList ID="ddlModel" runat="server" CssClass="form-select" 
                                    AutoPostBack="true" OnSelectedIndexChanged="ddlModel_SelectedIndexChanged">
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
                                                placeholder="Enter new model name"></asp:TextBox>
                                            <asp:RequiredFieldValidator ID="rfvOtherModel" runat="server" 
                                                ControlToValidate="txtOtherModel" ErrorMessage="Please enter model name"
                                                Display="Dynamic" ForeColor="#ef4444" Enabled="false">
                                            </asp:RequiredFieldValidator>
                                        </div>
                                        
                                        <div class="form-group">
                                            <label class="form-label required">Device Type</label>
                                            <asp:DropDownList ID="ddlDeviceType" runat="server" CssClass="form-select"
                                                AutoPostBack="true" OnSelectedIndexChanged="ddlDeviceType_SelectedIndexChanged">
                                                <asp:ListItem Value="">-- Select Type --</asp:ListItem>
                                                <asp:ListItem Value="Laptop">Laptop</asp:ListItem>
                                                <asp:ListItem Value="Desktop">PC</asp:ListItem>
                                                <asp:ListItem Value="All-in-One">All-in-One PC</asp:ListItem>
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
                                    placeholder="Enter serial number"></asp:TextBox>
                                <asp:RequiredFieldValidator ID="rfvSerialNumber" runat="server" 
                                    ControlToValidate="txtSerialNumber" ErrorMessage="Serial number is required"
                                    Display="Dynamic" ForeColor="#ef4444">
                                </asp:RequiredFieldValidator>
                            </div>

                            <div class="form-group">
                                <label class="form-label required">Asset Number</label>
                                <asp:TextBox ID="txtAssetNumber" runat="server" CssClass="form-control" 
                                    placeholder="Enter asset number"></asp:TextBox>
                                <asp:RequiredFieldValidator ID="rfvAssetNumber" runat="server" 
                                    ControlToValidate="txtAssetNumber" ErrorMessage="Asset number is required"
                                    Display="Dynamic" ForeColor="#ef4444">
                                </asp:RequiredFieldValidator>
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
                                <label for="chkVGAConverter">VGA Converter</label>
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
                                placeholder="List any other accessories..." TextMode="MultiLine" Rows="3"></asp:TextBox>
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
                                    ReadOnly="true"></asp:TextBox>
                                <div class="helper-text">Auto-filled based on your Windows ID</div>
                            </div>

                            <div class="form-group">
                                <label class="form-label">Issue Date</label>
                                <asp:TextBox ID="txtDateIssue" runat="server" CssClass="form-control auto-fill" 
                                    ReadOnly="true"></asp:TextBox>
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
                                        placeholder="Type to search employee email..." autocomplete="off"></asp:TextBox>
                                    <asp:HiddenField ID="hdnEmployeeEmail" runat="server" />
                                    <div class="dropdown-list" id="empEmailDropdown" style="display:none;">
                                    </div>
                                </div>
                                <asp:HiddenField ID="hdnEmployeeEmailList" runat="server" />
                                <asp:CustomValidator ID="rfvEmployeeEmail" runat="server" 
                                    ClientValidationFunction="validateEmployeeEmail"
                                    ErrorMessage="Please select a valid employee email" 
                                    Display="Dynamic" ForeColor="#ef4444">
                                </asp:CustomValidator>
                            </div>

                            <div class="form-group">
                                <label class="form-label required">HOD Email</label>
                                <div class="searchable-dropdown" id="hodEmailContainer">
                                    <asp:TextBox ID="txtHODEmailSearch" runat="server" CssClass="form-control" 
                                        placeholder="Type to search HOD email..." autocomplete="off"></asp:TextBox>
                                    <asp:HiddenField ID="hdnHODEmail" runat="server" />
                                    <div class="dropdown-list" id="hodEmailDropdown" style="display:none;">
                                    </div>
                                </div>
                                <asp:HiddenField ID="hdnHODEmailList" runat="server" />
                                <asp:CustomValidator ID="rfvHODEmail" runat="server" 
                                    ClientValidationFunction="validateHODEmail"
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
                                <div class="section-subtitle">Additional notes and agreement status</div>
                            </div>
                        </div>

                        <div class="form-group">
                            <label class="form-label">Remarks</label>
                            <asp:TextBox ID="txtRemarks" runat="server" CssClass="form-control remarks-box" 
                                placeholder="Enter any remarks or notes..." TextMode="MultiLine" Rows="4"></asp:TextBox>
                        </div>

                        <!-- Status Section (only for edit mode) -->
                        <asp:Panel ID="statusSection" runat="server" CssClass="status-section" Visible="false">
                            <label class="form-label required">Agreement Status</label>
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

                    <!-- ========== PHASE 2: AGREE (Employee) ========== -->
                    <div class="phase-container phase-agree" id="phaseAgree" runat="server">
                        <div class="phase-header">
                            <div class="phase-badge phase-badge-agree">
                                <i class="fas fa-handshake"></i>
                                <span>Phase 2</span>
                            </div>
                            <div class="phase-title-group">
                                <div class="phase-title">Employee Agreement</div>
                                <div class="phase-subtitle">Employee reviews terms, fills in details and agrees</div>
                            </div>
                        </div>
                        <div class="phase-content">
                    <!-- Employee Agreement Section -->
                    <asp:Panel ID="pnlEmployeeSignature" runat="server" CssClass="form-section" Visible="false">
                        <div class="section-header">
                            <div class="section-icon">
                                <i class="fas fa-file-contract"></i>
                            </div>
                            <div>
                                <div class="section-title">Employee Agreement</div>
                                <div class="section-subtitle">Fill in your details and review the Laptop/PC agreement</div>
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
                                        placeholder="Enter your full name"></asp:TextBox>
                                    <asp:RequiredFieldValidator ID="rfvEmpName" runat="server" 
                                        ControlToValidate="txtEmpName"
                                        ErrorMessage="Employee name is required"
                                        Display="Dynamic" ForeColor="#ef4444"
                                        ValidationGroup="EmployeeValidation">
                                    </asp:RequiredFieldValidator>
                                </div>

                                <div class="form-group">
                                    <label class="form-label">Employee ID (Windows ID)</label>
                                    <asp:TextBox ID="txtEmpId" runat="server" CssClass="form-control" 
                                        ReadOnly="true" style="background-color: #f1f5f9;"></asp:TextBox>
                                    <small style="color: var(--text-secondary); font-size: 0.8rem;">
                                        <i class="fas fa-info-circle"></i> Automatically captured from your Windows login
                                    </small>
                                </div>

                                <div class="form-group">
                                    <label class="form-label required">Position / Job Title</label>
                                    <asp:TextBox ID="txtEmpPosition" runat="server" CssClass="form-control" 
                                        placeholder="Enter your job title"></asp:TextBox>
                                    <asp:RequiredFieldValidator ID="rfvEmpPosition" runat="server" 
                                        ControlToValidate="txtEmpPosition"
                                        ErrorMessage="Position is required"
                                        Display="Dynamic" ForeColor="#ef4444"
                                        ValidationGroup="EmployeeValidation">
                                    </asp:RequiredFieldValidator>
                                </div>

                                <div class="form-group">
                                    <label class="form-label required">Department</label>
                                    <asp:TextBox ID="txtEmpDepartment" runat="server" CssClass="form-control" 
                                        placeholder="Enter your department"></asp:TextBox>
                                    <asp:RequiredFieldValidator ID="rfvEmpDepartment" runat="server" 
                                        ControlToValidate="txtEmpDepartment"
                                        ErrorMessage="Department is required"
                                        Display="Dynamic" ForeColor="#ef4444"
                                        ValidationGroup="EmployeeValidation">
                                    </asp:RequiredFieldValidator>
                                </div>
                            </div>
                        </div>

                        <!-- Hidden field for agreement ID -->
                        <asp:HiddenField ID="hdnAgreementId" runat="server" Value="" />

                        <div class="agreement-terms">
                            <h3 style="color: var(--primary); margin-bottom: 15px;">Laptop/PC Usage Agreement</h3>
        
                            <!-- Agreement Terms Text -->
                            <div style="line-height: 1.6; margin-bottom: 20px;">
                                <p><strong>In acceptance of this device (Laptop/PC) for usage, I agree to the terms and conditions stated below:</strong></p>
                                <ol style="margin-left: 20px; margin-bottom: 15px;">
                                    <li>I understand that I am responsible for the laptop/PC whilst in my possession.</li>
                                    <li>I am responsible for keeping the laptop/PC in good condition while using it and until the time of return.</li>
                                    <li>I understand that I should not install any program or software that is not permitted to use by the company, for privacy and security reasons.</li>
                                    <li>I should be the only authorized person to have access to and use this laptop/PC. Any unauthorized access to this laptop/PC is a violation of this company's policy, employment regulation and employment contract.</li>
                                    <li>I should remove all data that is not company or work-related before turning over the laptop/PC to the designated department.</li>
                                    <li>In the event of loss, theft, or damage, this must be reported to the police within 24-48 hours, and a copy of a Police report or incident report must be submitted to the company for verification purposes.</li>
                                    <li>I understand that any violation of these policies is a violation and I am subject to any disciplinary action by the company.</li>
                                </ol>
                            </div>

                            <!-- Submission Date (auto-filled) -->
                            <div class="employee-info-grid" style="margin-top: 20px;">
                                <div class="form-group">
                                    <label class="form-label">Submission Date</label>
                                    <asp:TextBox ID="txtEmpSignatureDate" runat="server" CssClass="form-control" 
                                        ReadOnly="true"></asp:TextBox>
                                </div>
                            </div>

                            <!-- Agreement Acceptance -->
                            <div class="terms-acceptance">
                                <div style="display: flex; align-items: flex-start; gap: 10px;">
                                    <asp:CheckBox ID="chkAgreeTerms" runat="server" />
                                    <div>
                                        <label for="chkAgreeTerms" style="font-weight: 600; color: #2e7d32;">
                                            I have read, understood, and agree to all the terms and conditions stated above
                                        </label>
                                        <asp:CustomValidator ID="cvAgreeTerms" runat="server" 
                                            ErrorMessage="You must agree to the terms and conditions"
                                            ClientValidationFunction="validateEmployeeAgreement"
                                            Display="Dynamic" ForeColor="#ef4444">
                                        </asp:CustomValidator>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </asp:Panel>
                        </div> <!-- /phase-content Phase 2 -->
                    </div> <!-- /phase-container Phase 2 -->

                    <!-- ========== PHASE 3: VERIFY (IT Admin) ========== -->
                    <div class="phase-container phase-verify" id="phaseVerify" runat="server">
                        <div class="phase-header">
                            <div class="phase-badge phase-badge-verify">
                                <i class="fas fa-clipboard-check"></i>
                                <span>Phase 3</span>
                            </div>
                            <div class="phase-title-group">
                                <div class="phase-title">IT Verification</div>
                                <div class="phase-subtitle">IT admin verifies laptop/pc and system configuration</div>
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
                                <div class="section-subtitle">Complete the verification checklist before finalizing the agreement</div>
                            </div>
                        </div>

                        <!-- Employee Summary -->
                        <div style="background: #f0fdf4; padding: 16px; border-radius: 8px; margin-bottom: 20px; border: 1px solid #bbf7d0;">
                            <h4 style="color: #15803d; margin-bottom: 10px;">
                                <i class="fas fa-user-check" style="margin-right: 8px;"></i>Employee Agreement Info
                            </h4>
                            <div class="employee-info-grid">
                                <div class="form-group">
                                    <label class="form-label">Employee Name</label>
                                    <asp:TextBox ID="txtVerifyEmpName" runat="server" CssClass="form-control auto-fill" ReadOnly="true"></asp:TextBox>
                                </div>
                                <div class="form-group">
                                    <label class="form-label">Employee ID</label>
                                    <asp:TextBox ID="txtVerifyEmpId" runat="server" CssClass="form-control auto-fill" ReadOnly="true"></asp:TextBox>
                                </div>
                                <div class="form-group">
                                    <label class="form-label">Position</label>
                                    <asp:TextBox ID="txtVerifyEmpPosition" runat="server" CssClass="form-control auto-fill" ReadOnly="true"></asp:TextBox>
                                </div>
                                <div class="form-group">
                                    <label class="form-label">Department</label>
                                    <asp:TextBox ID="txtVerifyEmpDepartment" runat="server" CssClass="form-control auto-fill" ReadOnly="true"></asp:TextBox>
                                </div>
                            </div>
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
                                        placeholder="Enter any additional verification notes..." TextMode="MultiLine" Rows="3"></asp:TextBox>
                                </div>
                            </div>
                        </div>

                        <!-- Verified By Info -->
                        <div class="form-grid">
                            <div class="form-group">
                                <label class="form-label">Verified By</label>
                                <asp:TextBox ID="txtVerifiedBy" runat="server" CssClass="form-control auto-fill" ReadOnly="true"></asp:TextBox>
                                <div class="helper-text">Auto-filled based on your Windows ID</div>
                            </div>
                            <div class="form-group">
                                <label class="form-label">Verification Date</label>
                                <asp:TextBox ID="txtVerifiedDate" runat="server" CssClass="form-control auto-fill" ReadOnly="true"></asp:TextBox>
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
                    <asp:Button ID="btnSubmit" runat="server" Text="Submit Agreement" 
                        CssClass="btn btn-primary" OnClick="btnSubmit_Click" />
                    <asp:Button ID="btnEdit" runat="server" Text="Edit Agreement" 
                        CssClass="btn btn-outline" OnClick="btnEdit_Click" Visible="false" />
                    <asp:Button ID="btnDelete" runat="server" Text="Archive" 
                        CssClass="btn btn-warning" OnClick="btnDelete_Click" Visible="false"
                        OnClientClick="return confirm('Are you sure you want to archive this agreement? It will be moved to Archived status.');"
                        style="background: linear-gradient(135deg, #f59e0b, #d97706); color: white; border: none;" />
                    <asp:Button ID="btnSubmitEmployee" runat="server" Text="Submit Employee Agreement" 
                        CssClass="btn btn-primary" OnClick="btnSubmitEmployee_Click" Visible="false" 
                        CausesValidation="true" ValidationGroup="EmployeeValidation" />
                    <asp:Button ID="btnVerify" runat="server" Text="Verify & Complete Agreement" 
                        CssClass="btn btn-primary" OnClick="btnVerify_Click" Visible="false"
                        OnClientClick="return confirm('Are you sure you want to verify and complete this agreement? This will send a final notification to the employee and HOD.');" />
                    <asp:Button ID="btnSaveUpdate" runat="server" Text="Save & Update" 
                        CssClass="btn btn-primary" OnClick="btnSaveUpdate_Click" Visible="false"
                        OnClientClick="return confirm('Save changes and resend the agreement notification email to the employee? A new signing link will be generated.');"
                        style="background: linear-gradient(135deg, #4361ee, #7209b7); color: white; border: none;" />
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
                        <p class="pdf-modal-text">Preparing your agreement document...</p>
                        <div class="pdf-progress-bar"><div class="pdf-progress-fill"></div></div>
                    </div>
                </div>
            </div>

            <!-- Footer -->
            <div class="footer">
                <p>Laptop/PC Agreement System &copy; <%= DateTime.Now.Year %> | Secure Enterprise Portal</p>
                <p style="margin-top: 8px; font-size: 0.8rem; color: #94a3b8;">
                    Windows Authentication | Last updated: <%= DateTime.Now.ToString("MMMM dd, yyyy HH:mm") %>
                </p>
            </div>
        </main>
    </form>

    <style>
        /* ===== Phase Container Styles ===== */
        .phase-container {
            margin-bottom: 32px;
            border-radius: 12px;
            border: 1px solid #e5e7eb;
            overflow: hidden;
            background: white;
            box-shadow: 0 1px 3px rgba(0,0,0,0.06);
        }
        .phase-header {
            display: flex;
            align-items: center;
            gap: 16px;
            padding: 16px 24px;
            border-bottom: 1px solid #e5e7eb;
        }
        .phase-badge {
            display: flex;
            align-items: center;
            gap: 6px;
            padding: 6px 14px;
            border-radius: 20px;
            font-size: 0.8rem;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            color: white;
            white-space: nowrap;
        }
        .phase-badge i { font-size: 0.75rem; }
        .phase-badge-raise { background: linear-gradient(135deg, #667eea, #764ba2); }
        .phase-badge-agree { background: linear-gradient(135deg, #f59e0b, #d97706); }
        .phase-badge-verify { background: linear-gradient(135deg, #10b981, #059669); }

        .phase-header .phase-raise .phase-header { background: #f5f3ff; }
        .phase-agree .phase-header { background: #fffbeb; }
        .phase-verify .phase-header { background: #ecfdf5; }

        .phase-raise .phase-header { background: linear-gradient(135deg, #f5f3ff, #ede9fe); border-bottom-color: #c4b5fd; }
        .phase-agree .phase-header { background: linear-gradient(135deg, #fffbeb, #fef3c7); border-bottom-color: #fcd34d; }
        .phase-verify .phase-header { background: linear-gradient(135deg, #ecfdf5, #d1fae5); border-bottom-color: #6ee7b7; }

        .phase-raise { border-color: #c4b5fd; }
        .phase-agree { border-color: #fcd34d; }
        .phase-verify { border-color: #6ee7b7; }

        .phase-title-group { flex: 1; }
        .phase-title { font-size: 1.1rem; font-weight: 700; color: #1e293b; }
        .phase-subtitle { font-size: 0.85rem; color: #64748b; margin-top: 2px; }
        .phase-content { padding: 24px; }
        .phase-content .form-section { 
            margin-bottom: 20px;
            padding: 0;
            border: none;
            box-shadow: none;
            background: transparent;
        }
        .phase-content .form-section:last-child { margin-bottom: 0; }

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
    </style>

    <script>
        // Add active class to current page
        document.addEventListener('DOMContentLoaded', function () {
            const currentPage = window.location.pathname.split('/').pop();
            const navLinks = document.querySelectorAll('.nav-link');

            navLinks.forEach(link => {
                if (link.getAttribute('href') === currentPage ||
                    (currentPage === '' && link.getAttribute('href') === 'Default.aspx')) {
                    link.classList.add('active');
                }
            });

            // Auto-fill current date
            const dateIssue = document.getElementById('<%= txtDateIssue.ClientID %>');
            if (dateIssue && !dateIssue.value) {
                const now = new Date();
                dateIssue.value = now.toLocaleDateString('en-GB');
            }

            // Enhanced form validation
            const form = document.getElementById('form1');
            if (form) {
                form.addEventListener('submit', function (e) {
                    const modelSelect = document.getElementById('<%= ddlModel.ClientID %>');
                    const serialNumber = document.getElementById('<%= txtSerialNumber.ClientID %>');
                    const assetNumber = document.getElementById('<%= txtAssetNumber.ClientID %>');

                    let isValid = true;

                    if (modelSelect && modelSelect.value === '') {
                        isValid = false;
                        highlightError(modelSelect);
                    }

                    if (serialNumber && !serialNumber.value.trim()) {
                        isValid = false;
                        highlightError(serialNumber);
                    }

                    if (assetNumber && !assetNumber.value.trim()) {
                        isValid = false;
                        highlightError(assetNumber);
                    }

                    if (!isValid) {
                        e.preventDefault();
                        showValidationMessage();
                    }
                });
            }

            function highlightError(element) {
                element.style.borderColor = '#ef4444';
                element.style.boxShadow = '0 0 0 3px rgba(239, 68, 68, 0.1)';

                element.addEventListener('input', function () {
                    this.style.borderColor = '';
                    this.style.boxShadow = '';
                });
            }

            function showValidationMessage() {
                console.log('Please fill in all required fields');
            }

            // Mobile sidebar toggle
            const sidebarToggle = document.createElement('button');
            sidebarToggle.innerHTML = '<i class="fas fa-bars"></i>';
            sidebarToggle.style.cssText = `
                position: fixed;
                top: 20px;
                left: 20px;
                z-index: 1001;
                background: var(--primary);
                color: white;
                border: none;
                width: 40px;
                height: 40px;
                border-radius: 8px;
                display: none;
                align-items: center;
                justify-content: center;
                cursor: pointer;
                box-shadow: var(--shadow-lg);
            `;

            sidebarToggle.classList.add('sidebar-toggle');
            document.body.appendChild(sidebarToggle);

            sidebarToggle.addEventListener('click', function () {
                document.querySelector('.sidebar').classList.toggle('mobile-open');
            });

            document.addEventListener('click', function (e) {
                const sidebar = document.querySelector('.sidebar');
                const toggleBtn = document.querySelector('.sidebar-toggle');

                if (window.innerWidth <= 768 &&
                    sidebar.classList.contains('mobile-open') &&
                    !sidebar.contains(e.target) &&
                    e.target !== toggleBtn &&
                    !toggleBtn.contains(e.target)) {
                    sidebar.classList.remove('mobile-open');
                }
            });

            function handleResize() {
                const sidebar = document.querySelector('.sidebar');
                const toggleBtn = document.querySelector('.sidebar-toggle');

                if (!toggleBtn) return;

                if (window.innerWidth <= 768) {
                    toggleBtn.style.display = 'flex';
                    sidebar.classList.remove('mobile-open');
                } else {
                    toggleBtn.style.display = 'none';
                    sidebar.classList.remove('mobile-open');
                    sidebar.style.transform = 'none';
                }
            }

            window.addEventListener('resize', handleResize);
            handleResize();

            // ===== SEARCHABLE DROPDOWN INITIALIZATION =====
            function initSearchableDropdown(textBoxId, hiddenFieldId, hiddenListId, dropdownDivId) {
                const textBox = document.getElementById(textBoxId);
                const hiddenField = document.getElementById(hiddenFieldId);
                const hiddenList = document.getElementById(hiddenListId);
                const dropdownDiv = document.getElementById(dropdownDivId);

                if (!textBox || !hiddenField || !hiddenList || !dropdownDiv) return;

                // Parse the email list from the hidden field (JSON array of {text, value})
                let items = [];
                try {
                    items = JSON.parse(hiddenList.value || '[]');
                } catch(e) {
                    items = [];
                }

                let highlightedIndex = -1;

                function renderDropdown(filter) {
                    const filterLower = (filter || '').toLowerCase();
                    const filtered = items.filter(item => 
                        item.text.toLowerCase().includes(filterLower) || 
                        item.value.toLowerCase().includes(filterLower)
                    );

                    dropdownDiv.innerHTML = '';
                    highlightedIndex = -1;

                    if (filtered.length === 0) {
                        dropdownDiv.innerHTML = '<div class="dropdown-no-results">No matching emails found</div>';
                        dropdownDiv.style.display = 'block';
                        return;
                    }

                    filtered.forEach((item, idx) => {
                        const div = document.createElement('div');
                        div.className = 'dropdown-item';
                        if (item.value === hiddenField.value) {
                            div.className += ' selected';
                        }
                        div.textContent = item.text;
                        div.dataset.value = item.value;
                        div.dataset.index = idx;

                        div.addEventListener('mousedown', function(e) {
                            e.preventDefault(); // Prevent blur
                            selectItem(item, textBox, hiddenField, dropdownDiv);
                        });

                        dropdownDiv.appendChild(div);
                    });

                    dropdownDiv.style.display = 'block';
                }

                function selectItem(item, tb, hf, dd) {
                    tb.value = item.text;
                    hf.value = item.value;
                    tb.classList.add('has-value');
                    dd.style.display = 'none';
                }

                // Show dropdown on focus
                textBox.addEventListener('focus', function() {
                    renderDropdown(this.value);
                });

                // Filter on input
                textBox.addEventListener('input', function() {
                    hiddenField.value = ''; // Clear selection while typing
                    textBox.classList.remove('has-value');
                    renderDropdown(this.value);
                });

                // Keyboard navigation
                textBox.addEventListener('keydown', function(e) {
                    const visibleItems = dropdownDiv.querySelectorAll('.dropdown-item');
                    if (!visibleItems.length) return;

                    if (e.key === 'ArrowDown') {
                        e.preventDefault();
                        highlightedIndex = Math.min(highlightedIndex + 1, visibleItems.length - 1);
                        updateHighlight(visibleItems);
                    } else if (e.key === 'ArrowUp') {
                        e.preventDefault();
                        highlightedIndex = Math.max(highlightedIndex - 1, 0);
                        updateHighlight(visibleItems);
                    } else if (e.key === 'Enter') {
                        e.preventDefault();
                        if (highlightedIndex >= 0 && highlightedIndex < visibleItems.length) {
                            const selectedValue = visibleItems[highlightedIndex].dataset.value;
                            const selectedItem = items.find(i => i.value === selectedValue);
                            if (selectedItem) {
                                selectItem(selectedItem, textBox, hiddenField, dropdownDiv);
                            }
                        }
                    } else if (e.key === 'Escape') {
                        dropdownDiv.style.display = 'none';
                    }
                });

                function updateHighlight(visibleItems) {
                    visibleItems.forEach((item, idx) => {
                        item.classList.toggle('highlighted', idx === highlightedIndex);
                        if (idx === highlightedIndex) {
                            item.scrollIntoView({ block: 'nearest' });
                        }
                    });
                }

                // Hide dropdown on blur
                textBox.addEventListener('blur', function() {
                    setTimeout(function() {
                        dropdownDiv.style.display = 'none';
                        // If no valid selection, try to match what's typed
                        if (!hiddenField.value && textBox.value) {
                            const match = items.find(i => 
                                i.text.toLowerCase() === textBox.value.toLowerCase() ||
                                i.value.toLowerCase() === textBox.value.toLowerCase()
                            );
                            if (match) {
                                selectItem(match, textBox, hiddenField, dropdownDiv);
                            }
                        }
                    }, 200);
                });

                // If there's already a value in the hidden field, set the display text
                if (hiddenField.value) {
                    const existing = items.find(i => i.value === hiddenField.value);
                    if (existing) {
                        textBox.value = existing.text;
                        textBox.classList.add('has-value');
                    }
                }
            }

            // Initialize both searchable dropdowns
            initSearchableDropdown(
                '<%= txtEmployeeEmailSearch.ClientID %>',
                '<%= hdnEmployeeEmail.ClientID %>',
                '<%= hdnEmployeeEmailList.ClientID %>',
                'empEmailDropdown'
            );

            initSearchableDropdown(
                '<%= txtHODEmailSearch.ClientID %>',
                '<%= hdnHODEmail.ClientID %>',
                '<%= hdnHODEmailList.ClientID %>',
                'hodEmailDropdown'
            );

            // Employee agreement validation (no signature required)
            window.validateEmployeeAgreement = function(source, args) {
                const isAgreed = document.getElementById('<%= chkAgreeTerms.ClientID %>').checked;
                
                if (!isAgreed) {
                    args.IsValid = false;
                    alert('Please agree to the terms and conditions.');
                } else {
                    args.IsValid = true;
                }
            }

            // Email validation functions
            window.validateEmployeeEmail = function(source, args) {
                const hdnValue = document.getElementById('<%= hdnEmployeeEmail.ClientID %>').value;
                args.IsValid = (hdnValue && hdnValue.trim() !== '');
            }

            window.validateHODEmail = function(source, args) {
                const hdnValue = document.getElementById('<%= hdnHODEmail.ClientID %>').value;
                args.IsValid = (hdnValue && hdnValue.trim() !== '');
            }
            
            // CRITICAL FIX: Remove view-mode class from employee fields - ONLY in employee mode
            setTimeout(function() {
                const isEmployeeMode = window.location.href.includes('token=');

                if (isEmployeeMode) {
                    const empName = document.getElementById('<%= txtEmpName.ClientID %>');
                    const empPosition = document.getElementById('<%= txtEmpPosition.ClientID %>');
                    const empDepartment = document.getElementById('<%= txtEmpDepartment.ClientID %>');
                    
                    if (empName) {
                        empName.classList.remove('readonly-control');
                        empName.style.cursor = 'text';
                        empName.readOnly = false;
                        empName.disabled = false;
                        empName.style.pointerEvents = 'auto';
                    }
                    if (empPosition) {
                        empPosition.classList.remove('readonly-control');
                        empPosition.style.cursor = 'text';
                        empPosition.readOnly = false;
                        empPosition.disabled = false;
                        empPosition.style.pointerEvents = 'auto';
                    }
                    if (empDepartment) {
                        empDepartment.classList.remove('readonly-control');
                        empDepartment.style.cursor = 'text';
                        empDepartment.readOnly = false;
                        empDepartment.disabled = false;
                        empDepartment.style.pointerEvents = 'auto';
                    }

                    const formContainer = document.getElementById('<%= formContainer.ClientID %>');
                    if (formContainer) {
                        formContainer.classList.remove('view-mode');
                    }
                }
            }, 100);
            
            // Backup employee field values before PostBack
            const btnSubmitEmployee = document.getElementById('<%= btnSubmitEmployee.ClientID %>');
            if (btnSubmitEmployee) {
                btnSubmitEmployee.addEventListener('click', function(e) {
                    const empName = document.getElementById('<%= txtEmpName.ClientID %>').value;
                    const empPosition = document.getElementById('<%= txtEmpPosition.ClientID %>').value;
                    const empDepartment = document.getElementById('<%= txtEmpDepartment.ClientID %>').value;
                    
                    document.getElementById('<%= hdnEmpNameBackup.ClientID %>').value = empName;
                    document.getElementById('<%= hdnEmpPositionBackup.ClientID %>').value = empPosition;
                    document.getElementById('<%= hdnEmpDepartmentBackup.ClientID %>').value = empDepartment;
                });
            }
            
            // Restore values on page load if they were backed up
            window.addEventListener('load', function() {
                const backupName = document.getElementById('<%= hdnEmpNameBackup.ClientID %>').value;
                const backupPosition = document.getElementById('<%= hdnEmpPositionBackup.ClientID %>').value;
                const backupDepartment = document.getElementById('<%= hdnEmpDepartmentBackup.ClientID %>').value;
    
                if (backupName) {
                    document.getElementById('<%= txtEmpName.ClientID %>').value = backupName;
                }
                if (backupPosition) {
                    document.getElementById('<%= txtEmpPosition.ClientID %>').value = backupPosition;
                }
                if (backupDepartment) {
                    document.getElementById('<%= txtEmpDepartment.ClientID %>').value = backupDepartment;
                }
    
                const isEmployeeMode = window.location.href.includes('token=');
    
                if (isEmployeeMode) {
                    const empNameField = document.getElementById('<%= txtEmpName.ClientID %>');
                    if (empNameField) {
                        if (empNameField.disabled) {
                            empNameField.disabled = false;
                            empNameField.readOnly = false;
                            empNameField.classList.remove('aspNetDisabled');
                            empNameField.classList.add('form-control');
                        }
                    }
        
                    const empPositionField = document.getElementById('<%= txtEmpPosition.ClientID %>');
                    if (empPositionField && empPositionField.disabled) {
                        empPositionField.disabled = false;
                        empPositionField.readOnly = false;
                        empPositionField.classList.remove('aspNetDisabled');
                        empPositionField.classList.add('form-control');
                    }
        
                    const empDepartmentField = document.getElementById('<%= txtEmpDepartment.ClientID %>');
                    if (empDepartmentField && empDepartmentField.disabled) {
                        empDepartmentField.disabled = false;
                        empDepartmentField.readOnly = false;
                        empDepartmentField.classList.remove('aspNetDisabled');
                        empDepartmentField.classList.add('form-control');
                    }
        
                    const formContainer = document.getElementById('<%= formContainer.ClientID %>');
                    if (formContainer && formContainer.classList.contains('view-mode')) {
                        formContainer.classList.remove('view-mode');
                    }
                }
            });

            // PDF Export with animation
            window.startPdfDownload = function(btn) {
                var overlay = document.getElementById('pdfOverlay');
                var title = overlay.querySelector('.pdf-modal-title');
                var text = overlay.querySelector('.pdf-modal-text');

                // Show overlay
                overlay.style.display = 'flex';
                overlay.classList.remove('pdf-complete');
                title.textContent = 'Generating PDF';
                text.textContent = 'Preparing your agreement document...';

                // Reset progress animation
                var fill = overlay.querySelector('.pdf-progress-fill');
                fill.style.animation = 'none';
                fill.offsetHeight; // trigger reflow
                fill.style.animation = '';

                // Use iframe to download without leaving page
                var agrId = '';
                try {
                    var hdnField = document.querySelector('[id$="hdnAgreementId"]');
                    if (hdnField) agrId = hdnField.value;
                } catch(e) {}

                if (!agrId) {
                    // fallback: let server-side handle it
                    setTimeout(function() { overlay.style.display = 'none'; }, 500);
                    return true;
                }

                var iframe = document.createElement('iframe');
                iframe.style.display = 'none';
                iframe.src = 'ExportPDF.ashx?id=' + agrId;
                document.body.appendChild(iframe);

                // Show success after delay, then hide
                setTimeout(function() {
                    overlay.classList.add('pdf-complete');
                    title.textContent = 'Download Complete!';
                    text.textContent = 'Your PDF has been saved.';

                    // Update icon
                    var icon = overlay.querySelector('.pdf-spinner-icon');
                    icon.className = 'fas fa-check-circle pdf-spinner-icon';

                    setTimeout(function() {
                        overlay.style.display = 'none';
                        // Reset icon for next use
                        icon.className = 'fas fa-file-pdf pdf-spinner-icon';
                    }, 1800);
                }, 2800);

                // Clean up iframe
                setTimeout(function() { if (iframe.parentNode) iframe.parentNode.removeChild(iframe); }, 10000);

                return false; // prevent postback
            };

            // Close dropdown when clicking outside
            document.addEventListener('click', function(e) {
                if (!e.target.closest('.searchable-dropdown')) {
                    document.querySelectorAll('.dropdown-list').forEach(d => d.style.display = 'none');
                }
            });
        });
    </script>
</body>
</html>