<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UserManagement.aspx.cs" Inherits="WindowsAuthDemo.UserManagement" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title>User Management — Laptop & Desktop Portal</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta http-equiv="X-Content-Type-Options" content="nosniff">
    <meta name="referrer" content="strict-origin-when-cross-origin">
    <script>document.documentElement.setAttribute("data-theme",localStorage.getItem("portalTheme")||"light");</script>
    <link href="https://fonts.googleapis.com/css2?family=Sora:wght@300;400;500;600;700;800&family=DM+Mono:ital,wght@0,400;0,500;1,400&display=swap" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
    <link rel="stylesheet" href="hardware-portal-styles.css">
    <style>
        /* ── User Management extras ──────────────────────────────────────── */
        .um-stats { display:grid; grid-template-columns:repeat(auto-fit,minmax(160px,1fr)); gap:20px; margin-bottom:32px; }
        .um-stat  {
            background:var(--card-bg);
            border:1px solid var(--border);
            border-radius:16px;
            padding:24px 20px;
            display:flex; flex-direction:column; align-items:center; gap:8px;
            transition:transform .2s, box-shadow .2s;
        }
        .um-stat:hover { transform:translateY(-3px); box-shadow:var(--shadow-lg); }
        .um-stat-icon  { width:48px;height:48px;border-radius:14px;display:flex;align-items:center;justify-content:center;font-size:1.3rem;color:#fff; }
        .um-stat-val   { font-size:2.2rem;font-weight:800;color:var(--text-primary);line-height:1; }
        .um-stat-lbl   { font-size:0.8rem;font-weight:600;text-transform:uppercase;letter-spacing:.06em;color:var(--text-secondary); }

        .um-stat.total   .um-stat-icon { background:linear-gradient(135deg,#4361ee,#7209b7); }
        .um-stat.active  .um-stat-icon { background:linear-gradient(135deg,#10b981,#059669); }
        .um-stat.admins  .um-stat-icon { background:linear-gradient(135deg,#f59e0b,#d97706); }
        .um-stat.inactive .um-stat-icon { background:linear-gradient(135deg,#94a3b8,#64748b); }

        /* ── Toolbar row ─────────────────────────────────────────────────── */
        .um-toolbar {
            display:flex; flex-wrap:wrap; gap:12px; align-items:flex-end;
            background:var(--card-bg); border:1px solid var(--border);
            border-radius:16px; padding:20px 24px; margin-bottom:24px;
        }
        .um-toolbar .filter-group { display:flex; flex-direction:column; gap:6px; }
        .um-toolbar .filter-group label { font-size:0.78rem;font-weight:700;text-transform:uppercase;letter-spacing:.06em;color:var(--text-secondary); }
        .um-toolbar select, .um-toolbar input[type=text] {
            height:40px; padding:0 14px; border:1px solid var(--border);
            border-radius:10px; background:var(--card-bg); color:var(--text-primary);
            font-size:0.9rem; min-width:160px;
        }
        .um-toolbar .spacer { flex:1; }

        /* ── Form card ───────────────────────────────────────────────────── */
        #userFormCard {
            background:var(--card-bg); border:1px solid var(--border);
            border-radius:18px; padding:32px; margin-bottom:32px;
            position:relative; overflow:hidden;
        }
        #userFormCard::before {
            content:''; position:absolute; top:0;left:0;right:0; height:4px;
            background:linear-gradient(90deg,#4361ee,#7209b7,#f72585);
        }
        .form-title { font-size:1.3rem;font-weight:800;color:var(--text-primary);margin-bottom:4px; }
        .form-subtitle { font-size:0.9rem;color:var(--text-secondary);margin-bottom:28px; }
        .form-row { display:grid; grid-template-columns:1fr 1fr; gap:20px; margin-bottom:20px; }
        .form-row.three { grid-template-columns:1fr 1fr 1fr; }
        @media(max-width:700px){ .form-row,.form-row.three { grid-template-columns:1fr; } }

        .form-field { display:flex; flex-direction:column; gap:6px; }
        .form-field label { font-size:0.8rem;font-weight:700;text-transform:uppercase;letter-spacing:.05em;color:var(--text-secondary); }
        .form-field input[type=text], .form-field input[type=email],
        .form-field select {
            padding:11px 14px; border:1.5px solid var(--border); border-radius:10px;
            background:var(--card-bg); color:var(--text-primary); font-size:0.95rem;
            transition:border-color .2s, box-shadow .2s;
        }
        .form-field input:focus, .form-field select:focus {
            outline:none; border-color:var(--primary); box-shadow:0 0 0 3px rgba(67,97,238,.15);
        }
        .form-field input[readonly] { opacity:.6; cursor:not-allowed; }
        .form-field .field-hint { font-size:0.78rem;color:var(--text-secondary);margin-top:3px; }

        .form-actions { display:flex; gap:12px; flex-wrap:wrap; margin-top:8px; }

        /* ── Users table ─────────────────────────────────────────────────── */
        .um-table-wrap {
            background:var(--card-bg); border:1px solid var(--border);
            border-radius:18px; overflow:hidden; margin-bottom:32px;
        }
        /* Inner scroll wrapper — allows horizontal scroll without breaking border-radius */
        .um-table-scroll {
            overflow-x: auto;
            -webkit-overflow-scrolling: touch;
        }
        .um-table-head {
            padding:20px 24px; display:flex; align-items:center; justify-content:space-between;
            flex-wrap:wrap; gap:12px; border-bottom:1px solid var(--border);
        }
        .um-table-title { font-size:1.1rem;font-weight:800;color:var(--text-primary); display:flex;align-items:center;gap:10px; }
        .um-count-badge {
            display:inline-flex;align-items:center;justify-content:center;
            background:linear-gradient(135deg,#4361ee,#7209b7); color:#fff;
            font-size:0.75rem;font-weight:700; min-width:28px;height:22px;
            border-radius:11px; padding:0 8px;
        }

        .users-table { width:100%; border-collapse:collapse; }
        .users-table th {
            background:var(--sidebar-bg); color:#94a3b8;
            font-size:0.72rem;font-weight:700;text-transform:uppercase;letter-spacing:.08em;
            padding:12px 20px; text-align:left; white-space:nowrap;
        }
        .users-table td { padding:14px 20px; border-bottom:1px solid var(--border); vertical-align:middle; }
        .users-table tbody tr:hover { background:var(--hover-bg,rgba(67,97,238,.04)); }
        .users-table tbody tr:last-child td { border-bottom:none; }

        .user-cell { display:flex;align-items:center;gap:12px; }
        .user-avatar-sm {
            width:36px;height:36px;border-radius:50%;
            display:flex;align-items:center;justify-content:center;
            font-size:0.85rem;font-weight:700;color:#fff;flex-shrink:0;
        }
        .user-winid { font-weight:700;font-size:0.9rem;color:var(--text-primary); }
        .user-email { font-size:0.8rem;color:var(--text-secondary);margin-top:2px; }

        .badge-role, .badge-status {
            display:inline-flex;align-items:center;gap:5px;
            font-size:0.75rem;font-weight:700;padding:4px 10px;border-radius:20px;
        }
        .badge-role.admin   { background:rgba(245,158,11,.15);  color:#d97706; }
        .badge-role.user    { background:rgba(100,116,139,.12); color:var(--text-secondary); }
        .badge-status.on    { background:rgba(16,185,129,.15);  color:#059669; }
        .badge-status.off   { background:rgba(239,68,68,.12);   color:#dc2626; }

        .acknowledgement receipts-chip {
            display:inline-flex;align-items:center;gap:5px;
            font-size:0.78rem;font-weight:600;color:var(--primary);
            background:rgba(67,97,238,.1); padding:3px 10px;border-radius:20px;
        }

        /* toggle pill button */
        .btn-toggle {
            border:none;cursor:pointer;border-radius:20px;padding:5px 14px;
            font-size:0.75rem;font-weight:700;transition:all .2s;
        }
        .btn-toggle.deactivate { background:rgba(239,68,68,.1);color:#dc2626; }
        .btn-toggle.deactivate:hover { background:#dc2626;color:#fff; }
        .btn-toggle.activate   { background:rgba(16,185,129,.1);color:#059669; }
        .btn-toggle.activate:hover   { background:#059669;color:#fff; }

        /* row actions */
        .row-actions { display:flex;gap:6px;align-items:center; }
        .btn-edit-sm, .btn-del-sm {
            width:32px;height:32px;border:none;cursor:pointer;border-radius:8px;
            display:flex;align-items:center;justify-content:center;font-size:0.85rem;
            transition:all .15s;
        }
        .btn-edit-sm { background:rgba(67,97,238,.1);color:#4361ee; }
        .btn-edit-sm:hover { background:#4361ee;color:#fff; }
        .btn-del-sm  { background:rgba(239,68,68,.1);color:#dc2626; }
        .btn-del-sm:hover  { background:#dc2626;color:#fff; }

        /* ── Audit logs ──────────────────────────────────────────────────── */
        .audit-wrap {
            background:var(--card-bg); border:1px solid var(--border);
            border-radius:18px; overflow:hidden;
        }
        .audit-scroll {
            overflow-x: auto;
            -webkit-overflow-scrolling: touch;
        }
        .audit-head {
            padding:20px 24px; display:flex; align-items:center;
            justify-content:space-between; flex-wrap:wrap; gap:12px;
            border-bottom:1px solid var(--border);
        }
        .audit-title { font-size:1.1rem;font-weight:800;color:var(--text-primary);display:flex;align-items:center;gap:10px; }

        .audit-table { width:100%; min-width:600px; border-collapse:collapse; }
        .audit-table th { background:var(--sidebar-bg);color:#94a3b8;font-size:0.72rem;font-weight:700;text-transform:uppercase;letter-spacing:.08em;padding:12px 20px;text-align:left; }
        .audit-table td { padding:12px 20px;border-bottom:1px solid var(--border);font-size:0.85rem;vertical-align:middle; }
        .audit-table tbody tr:last-child td { border-bottom:none; }
        .audit-table .ts { color:var(--text-secondary);font-size:0.78rem;white-space:nowrap; }
        .audit-table .desc { max-width:360px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap; }

        .audit-badge {
            font-size:0.7rem;font-weight:700;padding:3px 9px;border-radius:12px;
            display:inline-block;text-transform:uppercase;letter-spacing:.05em;
        }
        .audit-badge.CREATE   { background:rgba(16,185,129,.15);color:#059669; }
        .audit-badge.UPDATE   { background:rgba(67,97,238,.15); color:#4361ee; }
        .audit-badge.DELETE   { background:rgba(239,68,68,.15); color:#dc2626; }
        .audit-badge.ERROR    { background:rgba(245,158,11,.15);color:#d97706; }

        /* ── Misc ────────────────────────────────────────────────────────── */
        .section-gap { height:12px; }
        .no-data-um { text-align:center;padding:60px;color:var(--text-secondary); }
        .no-data-um i { font-size:3rem;opacity:.25;display:block;margin-bottom:16px; }
    </style>
</head>
<body>
    <div class="floating-icon"><i class="fas fa-laptop"></i></div>
    <div class="floating-icon"><i class="fas fa-microchip"></i></div>
    <div class="floating-icon"><i class="fas fa-server"></i></div>
    <div class="floating-icon"><i class="fas fa-keyboard"></i></div>
    <div class="floating-icon"><i class="fas fa-mouse"></i></div>

    <form id="form1" runat="server">
        <!-- ── Sidebar ─────────────────────────────────────────────────── -->
        <aside class="sidebar">
            <div class="sidebar-header">
                <i class="fas fa-laptop-code"></i>
                <h2>Laptop & Desktop Acknowledgement Receipt</h2>
            </div>
            <ul class="nav-links">
                <% if (Session["IsAdmin"] != null && (bool)Session["IsAdmin"]) { %>
                <li class="nav-item">
                    <a href="Default.aspx" class="nav-link">
                        <i class="fas fa-home"></i>
                        <span>Dashboard</span>
                    </a>
                </li>
                <li class="nav-item">
                    <a href="Agreement.aspx" class="nav-link">
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
                <% if (Session["IsAdmin"] != null && (bool)Session["IsAdmin"]) { %>
                <li class="nav-item">
                    <a href="UserManagement.aspx" class="nav-link active">
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
                <div style="display:flex;align-items:center;gap:12px;">
                    <div class="user-avatar"><i class="fas fa-user"></i></div>
                    <div>
                        <div style="font-weight:700;color:white;"><asp:Label ID="lblUserRoleSidebar" runat="server"/></div>
                        <div style="font-size:0.85rem;color:#94a3b8;"><asp:Label ID="lblUserSidebar" runat="server"/></div>
                    </div>
                </div>
            </div>
        </aside>

        <!-- ── Main ───────────────────────────────────────────────────── -->
        <main class="main-content">
            <header class="top-header">
                <div class="page-title">
                    <h1>User Management</h1>
                    <p>Manage portal access, roles and permissions</p>
                </div>
                <div class="user-profile">
                    <i class="fas fa-user-circle"></i>
                    <div>
                        <div style="font-weight:700;"><asp:Label ID="lblUser" runat="server"/></div>
                        <div style="font-size:0.85rem;color:var(--text-secondary);"><asp:Label ID="lblStatus" runat="server"/></div>
                    </div>
                </div>
            </header>

            <!-- Access denied -->
            <asp:Panel ID="pnlAccessDenied" runat="server" Visible="false">
                <div class="access-denied">
                    <div class="denied-icon"><i class="fas fa-ban"></i></div>
                    <h2 class="denied-title">Access Denied</h2>
                    <p class="denied-message">Administrator privileges are required to access User Management.</p>
                    <a href="Default.aspx" class="btn btn-primary"><i class="fas fa-arrow-left"></i> Back to Dashboard</a>
                </div>
            </asp:Panel>

            <!-- ══ Admin content ════════════════════════════════════════ -->
            <asp:Panel ID="pnlUserManagement" runat="server" Visible="false">

                <!-- Alert -->
                <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="alert" style="margin-bottom:24px;">
                    <i class="fas fa-info-circle"></i>
                    <asp:Literal ID="litMessage" runat="server"/>
                </asp:Panel>

                <!-- ── KPI stats ──────────────────────────────────────── -->
                <div class="um-stats">
                    <div class="um-stat total">
                        <div class="um-stat-icon"><i class="fas fa-users"></i></div>
                        <div class="um-stat-val"><asp:Literal ID="litStatTotal" runat="server" Text="0"/></div>
                        <div class="um-stat-lbl">Total Users</div>
                    </div>
                    <div class="um-stat active">
                        <div class="um-stat-icon"><i class="fas fa-user-check"></i></div>
                        <div class="um-stat-val"><asp:Literal ID="litStatActive" runat="server" Text="0"/></div>
                        <div class="um-stat-lbl">Active</div>
                    </div>
                    <div class="um-stat admins">
                        <div class="um-stat-icon"><i class="fas fa-user-shield"></i></div>
                        <div class="um-stat-val"><asp:Literal ID="litStatAdmins" runat="server" Text="0"/></div>
                        <div class="um-stat-lbl">Administrators</div>
                    </div>
                    <div class="um-stat inactive">
                        <div class="um-stat-icon"><i class="fas fa-user-slash"></i></div>
                        <div class="um-stat-val"><asp:Literal ID="litStatInactive" runat="server" Text="0"/></div>
                        <div class="um-stat-lbl">Inactive</div>
                    </div>
                </div>

                <!-- ── Add/Edit form ──────────────────────────────────── -->
                <div id="userFormCard">
                    <asp:HiddenField ID="hdnUserId" runat="server"/>
                    <div class="form-title"><asp:Literal ID="litFormTitle" runat="server" Text="Add New User"/></div>
                    <div class="form-subtitle"><asp:Literal ID="litFormSubtitle" runat="server" Text="Fill in the details below to register a new portal user"/></div>

                    <div class="form-row">
                        <div class="form-field">
                            <label>Windows ID *</label>
                            <asp:TextBox ID="txtWinId" runat="server" CssClass="form-control" placeholder="DOMAIN\username"/>
                            <span class="field-hint"><i class="fas fa-info-circle"></i> e.g. COMPANY\john.doe</span>
                        </div>
                        <div class="form-field">
                            <label>Email Address *</label>
                            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="form-control" placeholder="user@company.com"/>
                        </div>
                    </div>

                    <div class="form-row three">
                        <div class="form-field">
                            <label>Account Status</label>
                            <asp:DropDownList ID="ddlActive" runat="server" CssClass="form-select">
                                <asp:ListItem Value="1" Text="✅  Active"   Selected="True"/>
                                <asp:ListItem Value="0" Text="⛔  Inactive"/>
                            </asp:DropDownList>
                        </div>
                        <div class="form-field">
                            <label>Role</label>
                            <asp:DropDownList ID="ddlAdmin" runat="server" CssClass="form-select">
                                <asp:ListItem Value="0" Text="👤  Normal User"     Selected="True"/>
                                <asp:ListItem Value="1" Text="🛡️  Administrator"/>
                            </asp:DropDownList>
                            <span class="field-hint"><i class="fas fa-shield-alt"></i> Admins have full system access</span>
                        </div>
                        <div class="form-field">
                            <label>Email Notification</label>
                            <asp:DropDownList ID="ddlNotify" runat="server" CssClass="form-select">
                                <asp:ListItem Value="1" Text="📧  Receive Emails"  Selected="True"/>
                                <asp:ListItem Value="0" Text="🔕  No Emails"/>
                            </asp:DropDownList>
                            <span class="field-hint"><i class="fas fa-bell"></i> Admin CC on acknowledgement receipt emails</span>
                        </div>
                    </div>
                    <div class="form-row" style="justify-content:flex-end;margin-top:10px;">
                        <div class="form-field" style="justify-content:flex-end;">
                            <div class="form-actions">
                                <asp:Button ID="btnSave"   runat="server" Text="Save User"   CssClass="btn btn-primary"   OnClick="btnSave_Click"/>
                                <asp:Button ID="btnClear"  runat="server" Text="Clear"       CssClass="btn btn-secondary" OnClick="btnClear_Click"/>
                                <asp:Button ID="btnCancel" runat="server" Text="Cancel"      CssClass="btn btn-secondary" OnClick="btnCancel_Click" Visible="false"/>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- ── Users table ────────────────────────────────────── -->
                <div class="um-table-wrap">
                    <div class="um-table-head">
                        <div class="um-table-title">
                            <i class="fas fa-users" style="color:var(--primary);"></i>
                            All Users
                            <span class="um-count-badge"><asp:Literal ID="litUserCount" runat="server" Text="0"/></span>
                        </div>
                        <div style="display:flex;gap:10px;flex-wrap:wrap;align-items:center;">
                            <!-- search -->
                            <div style="position:relative;">
                                <i class="fas fa-search" style="position:absolute;left:12px;top:50%;transform:translateY(-50%);color:var(--text-secondary);font-size:0.85rem;"></i>
                                <asp:TextBox ID="txtSearch" runat="server" CssClass="search-input"
                                    placeholder="Search users…"
                                    AutoPostBack="true" OnTextChanged="txtSearch_TextChanged"
                                    style="padding-left:36px;height:36px;border-radius:10px;"/>
                            </div>
                            <!-- role filter -->
                            <asp:DropDownList ID="ddlRoleFilter" runat="server"
                                AutoPostBack="true" OnSelectedIndexChanged="ddlRoleFilter_SelectedIndexChanged"
                                style="height:36px;padding:0 12px;border:1px solid var(--border);border-radius:10px;background:var(--card-bg);color:var(--text-primary);font-size:0.85rem;">
                                <asp:ListItem Value="">All Roles</asp:ListItem>
                                <asp:ListItem Value="admin">Admins</asp:ListItem>
                                <asp:ListItem Value="user">Normal Users</asp:ListItem>
                            </asp:DropDownList>
                            <!-- status filter -->
                            <asp:DropDownList ID="ddlStatusFilter" runat="server"
                                AutoPostBack="true" OnSelectedIndexChanged="ddlStatusFilter_SelectedIndexChanged"
                                style="height:36px;padding:0 12px;border:1px solid var(--border);border-radius:10px;background:var(--card-bg);color:var(--text-primary);font-size:0.85rem;">
                                <asp:ListItem Value="">All Status</asp:ListItem>
                                <asp:ListItem Value="active">Active</asp:ListItem>
                                <asp:ListItem Value="inactive">Inactive</asp:ListItem>
                            </asp:DropDownList>
                            <!-- export -->
                            <asp:Button ID="btnExportCsv" runat="server" Text="⬇ Export CSV"
                                CssClass="btn btn-secondary"
                                style="height:36px;padding:0 16px;font-size:0.85rem;"
                                OnClick="btnExportCsv_Click"/>
                        </div>
                    </div>

                    <div class="um-table-scroll">
                    <asp:GridView ID="gvUsers" runat="server"
                        AutoGenerateColumns="false"
                        CssClass="users-table"
                        DataKeyNames="win_id"
                        OnRowEditing="gvUsers_RowEditing"
                        OnRowDeleting="gvUsers_RowDeleting"
                        OnRowCommand="gvUsers_RowCommand"
                        AllowPaging="true" PageSize="10"
                        OnPageIndexChanging="gvUsers_PageIndexChanging"
                        GridLines="None">
                        <Columns>

                            <asp:TemplateField HeaderText="User">
                                <ItemTemplate>
                                    <div class="user-cell">
                                        <div class="user-avatar-sm" style='background:<%# GetAvatarColor(Eval("win_id").ToString()) %>'>
                                            <%# GetInitials(Eval("win_id").ToString()) %>
                                        </div>
                                        <div>
                                            <div class="user-winid"><%# Eval("win_id") %></div>
                                            <div class="user-email"><%# Eval("email") %></div>
                                        </div>
                                    </div>
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Role">
                                <ItemTemplate>
                                    <span class='badge-role <%# Convert.ToInt32(Eval("admin"))==1 ? "admin" : "user" %>'>
                                        <i class='fas <%# Convert.ToInt32(Eval("admin"))==1 ? "fa-shield-alt" : "fa-user" %>'></i>
                                        <%# Convert.ToInt32(Eval("admin"))==1 ? "Administrator" : "Normal User" %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Status">
                                <ItemTemplate>
                                    <span class='badge-status <%# Convert.ToInt32(Eval("active"))==1 ? "on" : "off" %>'>
                                        <i class='fas fa-circle' style="font-size:.45rem;"></i>
                                        <%# Convert.ToInt32(Eval("active"))==1 ? "Active" : "Inactive" %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Notify">
                                <ItemTemplate>
                                    <span style='font-size:0.82rem; color:<%# Convert.ToInt32(Eval("receive_notification"))==1 ? "#10b981" : "#94a3b8" %>'>
                                        <i class='fas <%# Convert.ToInt32(Eval("receive_notification"))==1 ? "fa-bell" : "fa-bell-slash" %>'></i>
                                        <%# Convert.ToInt32(Eval("receive_notification"))==1 ? "On" : "Off" %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Acknowledgement Receipts">
                                <ItemTemplate>
                                    <span class="agreements-chip">
                                        <i class="fas fa-file-contract"></i>
                                        <%# Eval("agreement_count") %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Actions" ItemStyle-Width="220px">
                                <ItemTemplate>
                                    <div class="row-actions">
                                        <!-- Quick toggle active/inactive -->
                                        <asp:LinkButton runat="server"
                                            CommandName="ToggleActive"
                                            CommandArgument='<%# Eval("win_id") %>'
                                            CssClass='<%# "btn-toggle " + (Convert.ToInt32(Eval("active"))==1 ? "deactivate" : "activate") %>'
                                            OnClientClick="return confirm('Toggle this user\'s active status?');"
                                            ToolTip='<%# Convert.ToInt32(Eval("active"))==1 ? "Deactivate user" : "Activate user" %>'>
                                            <i class='fas <%# Convert.ToInt32(Eval("active"))==1 ? "fa-user-slash" : "fa-user-check" %>'></i>
                                            <%# Convert.ToInt32(Eval("active"))==1 ? "Deactivate" : "Activate" %>
                                        </asp:LinkButton>
                                        <!-- Edit -->
                                        <asp:LinkButton runat="server" CommandName="Edit"
                                            CssClass="btn-edit-sm" ToolTip="Edit user">
                                            <i class="fas fa-pencil-alt"></i>
                                        </asp:LinkButton>
                                        <!-- Delete -->
                                        <asp:LinkButton runat="server" CommandName="Delete"
                                            CssClass="btn-del-sm" ToolTip="Delete user"
                                            OnClientClick="return confirm('Permanently delete this user? This cannot be undone.');">
                                            <i class="fas fa-trash"></i>
                                        </asp:LinkButton>
                                    </div>
                                </ItemTemplate>
                            </asp:TemplateField>

                        </Columns>
                        <EmptyDataTemplate>
                            <div class="no-data-um">
                                <i class="fas fa-users"></i>
                                <h3 style="margin-bottom:8px;">No users found</h3>
                                <p>Try adjusting your filters or add a new user above.</p>
                            </div>
                        </EmptyDataTemplate>
                        <PagerStyle CssClass="pagination"/>
                    </asp:GridView>
                    </div><!-- /.um-table-scroll -->
                </div>

                <!-- ── Audit Logs ──────────────────────────────────────── -->
                <div class="audit-wrap">
                    <div class="audit-head">
                        <div class="audit-title">
                            <i class="fas fa-history" style="color:var(--primary);"></i>
                            Audit Logs
                            <span class="um-count-badge"><asp:Literal ID="litLogCount" runat="server" Text="0"/></span>
                        </div>
                        <div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">
                            <asp:DropDownList ID="ddlLogFilter" runat="server"
                                AutoPostBack="true" OnSelectedIndexChanged="ddlLogFilter_SelectedIndexChanged"
                                style="height:36px;padding:0 12px;border:1px solid var(--border);border-radius:10px;background:var(--card-bg);color:var(--text-primary);font-size:0.85rem;">
                                <asp:ListItem Value="">All Actions</asp:ListItem>
                                <asp:ListItem Value="CREATE">Create</asp:ListItem>
                                <asp:ListItem Value="UPDATE">Update</asp:ListItem>
                                <asp:ListItem Value="DELETE">Delete</asp:ListItem>
                                <asp:ListItem Value="ERROR">Error</asp:ListItem>
                            </asp:DropDownList>
                            <asp:Button ID="btnRefreshLogs" runat="server" Text="↻ Refresh"
                                CssClass="btn btn-secondary"
                                style="height:36px;padding:0 16px;font-size:0.85rem;"
                                OnClick="btnRefreshLogs_Click"/>
                        </div>
                    </div>

                    <div class="audit-scroll">
                    <asp:GridView ID="gvAuditLogs" runat="server"
                        AutoGenerateColumns="false"
                        CssClass="audit-table"
                        AllowPaging="true" PageSize="10"
                        OnPageIndexChanging="gvAuditLogs_PageIndexChanging"
                        GridLines="None">
                        <Columns>
                            <asp:TemplateField HeaderText="Time">
                                <ItemTemplate>
                                    <span class="ts"><%# Convert.ToDateTime(Eval("timestamp")).ToString("dd/MM/yyyy HH:mm:ss") %></span>
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:BoundField DataField="admin_user" HeaderText="Admin"/>

                            <asp:TemplateField HeaderText="Action">
                                <ItemTemplate>
                                    <span class='audit-badge <%# Eval("action_type") %>'><%# Eval("action_type") %></span>
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:BoundField DataField="target_user" HeaderText="Target"/>

                            <asp:TemplateField HeaderText="Description">
                                <ItemTemplate>
                                    <span class="desc" title='<%# Eval("description") %>'><%# Eval("description") %></span>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <EmptyDataTemplate>
                            <div class="no-data-um">
                                <i class="fas fa-history"></i>
                                <h3 style="margin-bottom:8px;">No logs yet</h3>
                                <p>Actions taken on users will appear here.</p>
                            </div>
                        </EmptyDataTemplate>
                        <PagerStyle CssClass="pagination"/>
                    </asp:GridView>
                    </div><!-- /.audit-scroll -->
                </div>

            </asp:Panel>
            <!-- ── end admin panel ──────────────────────────────────── -->

            <div class="footer">
                <p>User Management &copy; <%= DateTime.Now.Year %> | <%= DateTime.Now.ToString("MMMM dd, yyyy HH:mm:ss") %></p>
                <p style="margin-top:8px;font-size:0.8rem;color:rgba(255,255,255,0.8);">
                    <i class="fas fa-lock"></i> Windows Authentication | <i class="fas fa-shield-alt"></i> Secure Enterprise Portal
                </p>
            </div>
        </main>
    </form>

    <script>
        // ── Theme ─────────────────────────────────────────────────────────
        (function(){ document.documentElement.setAttribute('data-theme', localStorage.getItem('portalTheme')||'light'); })();

        document.addEventListener('DOMContentLoaded', function () {
            function applyTheme(t) {
                document.documentElement.setAttribute('data-theme', t);
                localStorage.setItem('portalTheme', t);
                var btn = document.getElementById('themeToggleBtn');
                if (btn) btn.innerHTML = t === 'dark'
                    ? '<i class="fas fa-sun"></i><span>Light Mode</span>'
                    : '<i class="fas fa-moon"></i><span>Dark Mode</span>';
            }
            applyTheme(localStorage.getItem('portalTheme') || 'light');
            var tb = document.getElementById('themeToggleBtn');
            if (tb) tb.addEventListener('click', function () {
                applyTheme(document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark');
            });

            // ── Stat counter animation ────────────────────────────────────
            document.querySelectorAll('.um-stat-val').forEach(function (el) {
                var target = parseInt(el.textContent);
                if (isNaN(target) || target <= 0) return;
                var cur = 0, steps = 40, inc = target / steps;
                var t = setInterval(function () {
                    cur += inc;
                    if (cur >= target) { el.textContent = target.toLocaleString(); clearInterval(t); }
                    else el.textContent = Math.floor(cur).toLocaleString();
                }, 18);
            });

            // ── Mobile sidebar ────────────────────────────────────────────
            var btn = document.createElement('button');
            btn.innerHTML = '<i class="fas fa-bars"></i>';
            btn.className = 'sidebar-toggle';
            document.body.appendChild(btn);
            btn.addEventListener('click', function () {
                document.querySelector('.sidebar').classList.toggle('mobile-open');
            });
            function handleResize() {
                btn.style.display = window.innerWidth <= 768 ? 'flex' : 'none';
                if (window.innerWidth > 768) document.querySelector('.sidebar').classList.remove('mobile-open');
            }
            window.addEventListener('resize', handleResize);
            handleResize();

            // ── Parallax ──────────────────────────────────────────────────
            window.addEventListener('scroll', function () {
                var s = window.pageYOffset;
                document.querySelectorAll('.floating-icon').forEach(function (ic, i) {
                    ic.style.transform = 'translateY(' + (-(s * (0.5 + i * 0.1))) + 'px) rotate(' + (s * 0.1) + 'deg)';
                });
            });
        });
    </script>
    <!-- Theme toggle — floating button, always visible at any zoom -->
    <button id="themeToggleBtn" class="theme-toggle" type="button">
        <i class="fas fa-moon"></i><span>Dark Mode</span>
    </button>

</body>
</html>
