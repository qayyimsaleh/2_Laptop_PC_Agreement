<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ExistingAgreements.aspx.cs" Inherits="WindowsAuthDemo.ExistingAgreements" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Acknowledgement Receipts Management</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta http-equiv="X-Content-Type-Options" content="nosniff">
    <meta name="referrer" content="strict-origin-when-cross-origin">
    <script>document.documentElement.setAttribute("data-theme",localStorage.getItem("portalTheme")||"light");</script>
    <link href="https://fonts.googleapis.com/css2?family=Sora:wght@300;400;500;600;700;800&family=DM+Mono:ital,wght@0,400;0,500;1,400&display=swap" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
    <link rel="stylesheet" href="hardware-portal-styles.css">
    <style>
        /* ── Column sort headers ─────────────────────────────────── */

        /* The th cell itself when its column is sorted */
        .sort-active-th {
            background: rgba(99, 102, 241, 0.08) !important;
        }

        /* The clickable label inside each header */
        .sort-header {
            cursor: pointer;
            user-select: none;
            white-space: nowrap;
            display: inline-flex;
            align-items: center;
            gap: 7px;
            padding: 4px 6px;
            border-radius: 6px;
            transition: background .15s, color .15s;
            font-weight: 600;
            color: inherit;
            text-decoration: none;
        }
        .sort-header:hover {
            background: rgba(99,102,241,.12);
            color: var(--primary);
        }
        .sort-header.sort-active {
            color: var(--primary);
        }

        /* Stack of arrows for unsorted column */
        .sort-icon-wrap {
            display: inline-flex;
            flex-direction: column;
            gap: 1px;
            line-height: 1;
        }
        .sort-icon-wrap i { font-size: .6rem; color: #cbd5e1; transition: color .15s; }

        /* Active arrow: full size, bold primary, other arrow fades */
        .sort-header.sort-active .sort-icon-wrap i.active-arrow {
            color: var(--primary);
            font-size: .78rem;
        }
        .sort-header.sort-active .sort-icon-wrap i.inactive-arrow {
            color: #e2e8f0;
        }
        .sort-header:hover .sort-icon-wrap i { color: #94a3b8; }

        /* ── Filter active badge ────────────────────── */
        #activeFilterCount { display: none; }

        /* ── Date inputs match select height ────────── */
        input[type="date"].filter-select { padding: 8px 12px; }

        /* ── UpdatePanel loading indicator ─────────── */
        #ajaxLoader {
            display: none;
            position: fixed;
            top: 16px;
            right: 20px;
            background: var(--primary);
            color: white;
            padding: 6px 14px;
            border-radius: 20px;
            font-size: .78rem;
            font-weight: 600;
            z-index: 9999;
            box-shadow: 0 2px 10px rgba(0,0,0,.2);
            animation: fadeInOut .3s ease;
        }
        @keyframes fadeInOut { from { opacity:0; transform:translateY(-6px); } to { opacity:1; transform:translateY(0); } }
    </style>
</head>
<body>
    <!-- Floating Icons -->
    <div class="floating-icon"><i class="fas fa-laptop"></i></div>
    <div class="floating-icon"><i class="fas fa-microchip"></i></div>
    <div class="floating-icon"><i class="fas fa-keyboard"></i></div>
    <div class="floating-icon"><i class="fas fa-server"></i></div>
    <div class="floating-icon"><i class="fas fa-hdd"></i></div>

    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePartialRendering="true" />
        <!-- Sidebar -->
        <aside class="sidebar">
            <div class="sidebar-header">
                <i class="fas fa-laptop-code"></i>
                <h2>Laptop & Desktop Acknowledgement Receipt</h2>
            </div>

            <ul class="nav-links">
                <% if (IsAdmin) { %>
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
                    <a href="ExistingAgreements.aspx" class="nav-link active">
                        <i class="fas fa-list-alt"></i>
                        <span>Acknowledgement Receipts</span>
                    </a>
                </li>
                <% if (IsAdmin) { %>
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
                    <h1>Acknowledgement Receipts Management</h1>
                    <p>View, search, and manage all laptop & desktop acknowledgement receipts in the system</p>
                </div>
                <div class="user-profile">
                    <i class="fas fa-user-circle"></i>
                    <div class="user-profile-info">
                        <div class="user-profile-name">
                            <asp:Label ID="lblUser" runat="server"></asp:Label>
                        </div>
                        <div class="user-profile-status">
                            <asp:Label ID="lblStatus" runat="server"></asp:Label>
                        </div>
                    </div>
                </div>
            </header>

            <!-- Statistics Cards -->
            <div class="stats-grid">
                <div class="stat-card stat-card-filter" data-filter="" style="cursor:pointer;" title="Show all">
                    <div class="stat-icon">
                        <i class="fas fa-file-alt"></i>
                    </div>
                    <div class="stat-value">
                        <asp:Literal ID="litTotal" runat="server" Text="0"></asp:Literal>
                    </div>
                    <div class="stat-title">Total Acknowledgement Receipts</div>
                </div>

                <div class="stat-card stat-card-filter" data-filter="Draft" style="cursor:pointer;" title="Filter: Draft">
                    <div class="stat-icon">
                        <i class="fas fa-edit"></i>
                    </div>
                    <div class="stat-value">
                        <asp:Literal ID="litDrafts" runat="server" Text="0"></asp:Literal>
                    </div>
                    <div class="stat-title">Drafts</div>
                </div>

                <div class="stat-card stat-card-filter" data-filter="Pending" style="cursor:pointer;" title="Filter: Pending">
                    <div class="stat-icon">
                        <i class="fas fa-clock"></i>
                    </div>
                    <div class="stat-value">
                        <asp:Literal ID="litPending" runat="server" Text="0"></asp:Literal>
                    </div>
                    <div class="stat-title">Pending</div>
                </div>

                <div class="stat-card stat-card-filter" data-filter="Agreed" style="cursor:pointer;" title="Filter: Agreed">
                    <div class="stat-icon">
                        <i class="fas fa-handshake"></i>
                    </div>
                    <div class="stat-value">
                        <asp:Literal ID="litAgreed" runat="server" Text="0"></asp:Literal>
                    </div>
                    <div class="stat-title">Agreed</div>
                </div>

                <div class="stat-card stat-card-filter" data-filter="Completed" style="cursor:pointer;" title="Filter: Completed">
                    <div class="stat-icon">
                        <i class="fas fa-check-circle"></i>
                    </div>
                    <div class="stat-value">
                        <asp:Literal ID="litCompleted" runat="server" Text="0"></asp:Literal>
                    </div>
                    <div class="stat-title">Completed</div>
                </div>

                <div class="stat-card stat-card-filter" data-filter="Archived" style="cursor:pointer;" title="Filter: Archived">
                    <div class="stat-icon">
                        <i class="fas fa-archive"></i>
                    </div>
                    <div class="stat-value">
                        <asp:Literal ID="litArchived" runat="server" Text="0"></asp:Literal>
                    </div>
                    <div class="stat-title">Archived</div>
                </div>
            </div>

            <asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional">
            <ContentTemplate>

            <!-- Filters Section -->
            <div class="filters-section">
                <div class="filters-header" style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:10px;">
                    <div style="display:flex;align-items:center;gap:10px;">
                        <i class="fas fa-filter"></i>
                        <h3 style="margin:0;">Filter Acknowledgement Receipts</h3>
                        <span id="activeFilterCount" style="display:none;background:var(--primary);color:#fff;font-size:.72rem;font-weight:700;padding:2px 8px;border-radius:12px;"></span>
                    </div>
                    <asp:Button ID="btnClearFilters" runat="server" Text="✕ Clear All Filters"
                        CssClass="btn btn-secondary" OnClick="btnClearFilters_Click"
                        style="height:34px;padding:0 14px;font-size:.82rem;" />
                </div>

                <!-- Row 1: Search + Status + Hardware Type + IT Staff -->
                <div class="filters-grid" style="margin-bottom:12px;">
                    <div class="filter-group" style="flex:2;min-width:220px;">
                        <label class="filter-label"><i class="fas fa-search" style="margin-right:4px;"></i>Search</label>
                        <asp:TextBox ID="txtSearch" runat="server" CssClass="search-input"
                            placeholder="Acknowledgement no, serial, asset, employee, model..."
                            AutoPostBack="true" OnTextChanged="txtSearch_TextChanged"></asp:TextBox>
                    </div>

                    <div class="filter-group">
                        <label class="filter-label"><i class="fas fa-circle-dot" style="margin-right:4px;"></i>Status</label>
                        <asp:DropDownList ID="ddlStatusFilter" runat="server" CssClass="filter-select"
                            AutoPostBack="true" OnSelectedIndexChanged="ddlStatusFilter_SelectedIndexChanged">
                            <asp:ListItem Value="">All Status</asp:ListItem>
                            <asp:ListItem Value="Draft">Draft</asp:ListItem>
                            <asp:ListItem Value="Pending">Pending</asp:ListItem>
                            <asp:ListItem Value="Agreed">Agreed</asp:ListItem>
                            <asp:ListItem Value="Completed">Completed</asp:ListItem>
                            <asp:ListItem Value="Archived">Archived</asp:ListItem>
                        </asp:DropDownList>
                    </div>

                    <div class="filter-group">
                        <label class="filter-label"><i class="fas fa-laptop" style="margin-right:4px;"></i>Hardware Type</label>
                        <asp:DropDownList ID="ddlHardwareType" runat="server" CssClass="filter-select"
                            AutoPostBack="true" OnSelectedIndexChanged="ddlHardwareType_SelectedIndexChanged">
                            <asp:ListItem Value="">All Types</asp:ListItem>
                        </asp:DropDownList>
                    </div>

                    <asp:Panel ID="pnlITStaffFilter" runat="server">
                        <div class="filter-group">
                            <label class="filter-label"><i class="fas fa-user-tie" style="margin-right:4px;"></i>IT Staff</label>
                            <asp:DropDownList ID="ddlITStaff" runat="server" CssClass="filter-select"
                                AutoPostBack="true" OnSelectedIndexChanged="ddlITStaff_SelectedIndexChanged">
                                <asp:ListItem Value="">All Staff</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </asp:Panel>
                </div>

                <!-- Row 2: Date range + page size -->
                <div class="filters-grid">
                    <div class="filter-group">
                        <label class="filter-label"><i class="fas fa-calendar-day" style="margin-right:4px;"></i>Issue Date From</label>
                        <asp:TextBox ID="txtDateFrom" runat="server" CssClass="filter-select"
                            TextMode="Date" AutoPostBack="true" OnTextChanged="txtDateFrom_TextChanged"></asp:TextBox>
                    </div>

                    <div class="filter-group">
                        <label class="filter-label"><i class="fas fa-calendar-day" style="margin-right:4px;"></i>Issue Date To</label>
                        <asp:TextBox ID="txtDateTo" runat="server" CssClass="filter-select"
                            TextMode="Date" AutoPostBack="true" OnTextChanged="txtDateTo_TextChanged"></asp:TextBox>
                    </div>

                    <div class="filter-group">
                        <label class="filter-label"><i class="fas fa-table-list" style="margin-right:4px;"></i>Rows per Page</label>
                        <asp:DropDownList ID="ddlPageSize" runat="server" CssClass="filter-select"
                            AutoPostBack="true" OnSelectedIndexChanged="ddlPageSize_SelectedIndexChanged">
                            <asp:ListItem Value="10" Selected="True">10 per page</asp:ListItem>
                            <asp:ListItem Value="25">25 per page</asp:ListItem>
                            <asp:ListItem Value="50">50 per page</asp:ListItem>
                            <asp:ListItem Value="100">100 per page</asp:ListItem>
                        </asp:DropDownList>
                    </div>

                    <div class="filter-group" style="justify-content:flex-end;align-items:flex-end;">
                        <label class="filter-label">&nbsp;</label>
                        <div style="font-size:.82rem;color:var(--text-secondary);padding:8px 0;">
                            Showing <strong><asp:Literal ID="litShowingCount" runat="server" Text="0"/></strong>
                            of <strong><asp:Literal ID="litTotalCount" runat="server" Text="0"/></strong> acknowledgement receipts
                        </div>
                    </div>
                </div>
            </div>

            <!-- Acknowledgement Receipts Table -->
            <div class="table-container">
                <div class="table-header">
                    <div class="table-title">
                        <i class="fas fa-list"></i>
                        All Acknowledgement Receipts
                    </div>
                    <div class="table-controls" style="display:flex;align-items:center;gap:12px;">
                        <asp:Literal ID="litSortInfo" runat="server"/>
                    </div>
                </div>

                <!-- Sortable Acknowledgement Receipts Grid -->
                <div style="overflow-x:auto; -webkit-overflow-scrolling:touch;">
                <asp:GridView ID="gvAgreements" runat="server" CssClass="table"
                    AutoGenerateColumns="false"
                    AllowSorting="true"
                    OnSorting="gvAgreements_Sorting"
                    OnRowCommand="gvAgreements_RowCommand"
                    OnRowDataBound="gvAgreements_RowDataBound"
                    ShowHeaderWhenEmpty="true">
                    <Columns>

                        <asp:TemplateField SortExpression="a.agreement_number">
                            <HeaderTemplate>
                                <asp:LinkButton runat="server"
                                    CommandName="Sort" CommandArgument="a.agreement_number"
                                    CssClass="sort-header <%# GetSortClass(&quot;a.agreement_number&quot;) %>"
                                    style="background:none;border:none;padding:0;color:inherit;text-decoration:none;">
                                    Agreement # <%# GetSortIcon("a.agreement_number") %>
                                </asp:LinkButton>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <div style="font-weight:600;color:var(--text-primary);"><%# Eval("agreement_number") %></div>
                                <div style="font-size:.75rem;color:var(--text-secondary);margin-top:2px;">
                                    <i class="fas fa-calendar-alt" style="margin-right:3px;"></i>
                                    <%# Eval("created_date") != DBNull.Value ? Convert.ToDateTime(Eval("created_date")).ToString("dd MMM yyyy") : "—" %>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField SortExpression="m.model">
                            <HeaderTemplate>
                                <asp:LinkButton runat="server"
                                    CommandName="Sort" CommandArgument="m.model"
                                    CssClass="sort-header <%# GetSortClass(&quot;m.model&quot;) %>"
                                    style="background:none;border:none;padding:0;color:inherit;text-decoration:none;">
                                    Model / Type <%# GetSortIcon("m.model") %>
                                </asp:LinkButton>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <div style="font-weight:600;font-size:.88rem;"><%# Eval("model") %></div>
                                <div style="font-size:.75rem;color:var(--text-secondary);margin-top:2px;">
                                    <i class="fas fa-tag" style="margin-right:3px;"></i><%# Eval("hardware_type") %>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Serial / Asset">
                            <ItemTemplate>
                                <div style="font-size:.83rem;">
                                    <div><span style="color:var(--text-secondary);">S/N</span> <%# Eval("serial_number") %></div>
                                    <div><span style="color:var(--text-secondary);">A/N</span> <%# Eval("asset_number") %></div>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField SortExpression="a.agreement_status">
                            <HeaderTemplate>
                                <asp:LinkButton runat="server"
                                    CommandName="Sort" CommandArgument="a.agreement_status"
                                    CssClass="sort-header <%# GetSortClass(&quot;a.agreement_status&quot;) %>"
                                    style="background:none;border:none;padding:0;color:inherit;text-decoration:none;">
                                    Status <%# GetSortIcon("a.agreement_status") %>
                                </asp:LinkButton>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <span class='status-badge status-<%# Eval("agreement_status").ToString().ToLower() %>'>
                                    <i class="fas fa-circle" style="font-size:.45rem;"></i>
                                    <%# Eval("agreement_status") %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField SortExpression="a.employee_name">
                            <HeaderTemplate>
                                <asp:LinkButton runat="server"
                                    CommandName="Sort" CommandArgument="a.employee_name"
                                    CssClass="sort-header <%# GetSortClass(&quot;a.employee_name&quot;) %>"
                                    style="background:none;border:none;padding:0;color:inherit;text-decoration:none;">
                                    Employee <%# GetSortIcon("a.employee_name") %>
                                </asp:LinkButton>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <div style="font-size:.85rem;">
                                    <div style="font-weight:600;"><%# (Eval("employee_name") == null || Eval("employee_name") == DBNull.Value || string.IsNullOrEmpty(Eval("employee_name").ToString())) ? "<span style='color:#9ca3af'>—</span>" : Eval("employee_name").ToString() %></div>
                                    <div style="font-size:.75rem;color:var(--text-secondary);">
                                        <i class="fas fa-envelope" style="margin-right:3px;"></i>
                                        <%# (Eval("employee_email") == null || Eval("employee_email") == DBNull.Value || string.IsNullOrEmpty(Eval("employee_email").ToString())) ? "—" : Eval("employee_email").ToString() %>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField SortExpression="a.it_staff_win_id">
                            <HeaderTemplate>
                                <asp:LinkButton runat="server"
                                    CommandName="Sort" CommandArgument="a.it_staff_win_id"
                                    CssClass="sort-header <%# GetSortClass(&quot;a.it_staff_win_id&quot;) %>"
                                    style="background:none;border:none;padding:0;color:inherit;text-decoration:none;">
                                    IT Staff <%# GetSortIcon("a.it_staff_win_id") %>
                                </asp:LinkButton>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <div style="font-size:.85rem;">
                                    <i class="fas fa-user-tie" style="color:var(--text-secondary);margin-right:5px;"></i>
                                    <%# Eval("it_staff_win_id") %>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField SortExpression="a.issue_date">
                            <HeaderTemplate>
                                <asp:LinkButton runat="server"
                                    CommandName="Sort" CommandArgument="a.issue_date"
                                    CssClass="sort-header <%# GetSortClass(&quot;a.issue_date&quot;) %>"
                                    style="background:none;border:none;padding:0;color:inherit;text-decoration:none;">
                                    Issue Date <%# GetSortIcon("a.issue_date") %>
                                </asp:LinkButton>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <div style="font-size:.83rem;white-space:nowrap;">
                                    <i class="fas fa-calendar" style="color:var(--text-secondary);margin-right:4px;"></i>
                                    <%# Convert.ToDateTime(Eval("issue_date")).ToString("dd MMM yyyy") %>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Actions" ItemStyle-Width="80px">
                            <ItemTemplate>
                                <div class="action-buttons">
                                    <asp:LinkButton ID="btnEdit" runat="server" CssClass="btn-action btn-edit"
                                        CommandName="EditAgreement" CommandArgument='<%# Eval("id") %>'
                                        ToolTip="Edit Acknowledgement Receipt" Visible="false">
                                        <i class="fas fa-edit"></i>
                                    </asp:LinkButton>
                                    <asp:LinkButton ID="btnView" runat="server" CssClass="btn-action btn-view"
                                        CommandName="ViewAgreement" CommandArgument='<%# Eval("id") %>'
                                        ToolTip="View Details">
                                        <i class="fas fa-eye"></i>
                                    </asp:LinkButton>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>

                    </Columns>
                    <EmptyDataTemplate>
                        <div class="no-data">
                            <div class="no-data-icon"><i class="fas fa-file-contract"></i></div>
                            <h3>No Acknowledgement Receipts Found</h3>
                            <p>Try adjusting your filters or create a new acknowledgement</p>
                        </div>
                    </EmptyDataTemplate>
                </asp:GridView>
                </div><!-- /scroll wrapper -->

                <!-- Pagination -->
                <div class="pagination">
                    <asp:Repeater ID="rptPagination" runat="server" OnItemCommand="rptPagination_ItemCommand">
                        <ItemTemplate>
                            <asp:LinkButton ID="lnkPage" runat="server" 
                                CommandName="Page" 
                                CommandArgument='<%# Eval("PageNumber") %>'
                                CssClass='<%# Container.DataItem != null && Convert.ToInt32(((System.Data.DataRowView)Container.DataItem)["PageNumber"]) == CurrentPage ? "page-link active" : "page-link" %>'
                                Text='<%# Eval("PageNumber") %>'>
                            </asp:LinkButton>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </div>

            </ContentTemplate>
            </asp:UpdatePanel>

            <!-- Footer -->
            <div class="footer">
                <p>Laptop & Desktop Acknowledgement Receipt Portal &copy; <%= DateTime.Now.Year %> | Last updated: <%= DateTime.Now.ToString("MMMM dd, yyyy HH:mm:ss") %></p>
                <p style="margin-top: 8px; font-size: 0.8rem; color: rgba(255, 255, 255, 0.8);">
                    Windows Authentication | Secure Enterprise Portal
                </p>
            </div>
        </main>
    </form>

    <script>
        // ── Theme init & toggle ────────────────────────────────────────────────
        (function(){ document.documentElement.setAttribute('data-theme', localStorage.getItem('portalTheme')||'light'); })();

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
        });
    </script>

    <!-- AJAX loading indicator -->
    <div id="ajaxLoader"><i class="fas fa-spinner fa-spin" style="margin-right:6px;"></i>Loading...</div>

    <script>
        // ── UpdatePanel: show/hide loader on partial postbacks ──────────────
        if (typeof Sys !== 'undefined') {
            Sys.WebForms.PageRequestManager.getInstance().add_beginRequest(function() {
                document.getElementById('ajaxLoader').style.display = 'block';
            });
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function() {
                document.getElementById('ajaxLoader').style.display = 'none';
                // Re-apply theme after panel refresh
                var theme = localStorage.getItem('portalTheme') || 'light';
                document.documentElement.setAttribute('data-theme', theme);
                // Re-highlight the active stat card (ddlStatusFilter was just re-rendered)
                highlightActiveCard();
            });
        }
    </script>

    <script>
        // ── Global helpers (must be outside DOMContentLoaded so endRequest can call them) ──
        function getStatusDdl() {
            return document.getElementById('<%= ddlStatusFilter.ClientID %>');
        }

        function highlightActiveCard() {
            var ddl = getStatusDdl();
            var cur = ddl ? ddl.value : '';
            document.querySelectorAll('.stat-card-filter').forEach(function(c) {
                c.classList.toggle('stat-card-active', c.getAttribute('data-filter') === cur);
            });
        }

        document.addEventListener('DOMContentLoaded', function () {

            // Run once on initial load to mark the active card
            highlightActiveCard();

            document.querySelectorAll('.stat-card-filter').forEach(function(card) {
                card.addEventListener('click', function() {
                    var ddl = getStatusDdl(); // fresh lookup — not the stale cached reference
                    if (!ddl) return;
                    ddl.value = this.getAttribute('data-filter');
                    if (typeof __doPostBack !== 'undefined')
                        __doPostBack('<%= ddlStatusFilter.UniqueID %>', '');
                    else ddl.form.submit();
                });
            });

            // ── Active filter badge counter ─────────────────────────────────
            function updateFilterBadge() {
                var active = 0;
                var ids = [
                    '<%= ddlStatusFilter.ClientID %>',
                    '<%= ddlHardwareType.ClientID %>',
                    '<% if (IsAdmin) { %><%= ddlITStaff.ClientID %><% } %>',
                    '<%= txtDateFrom.ClientID %>',
                    '<%= txtDateTo.ClientID %>',
                    '<%= txtSearch.ClientID %>'
                ];
                ids.forEach(function(id) {
                    if (!id) return;
                    var el = document.getElementById(id);
                    if (el && el.value) active++;
                });
                var badge = document.getElementById('activeFilterCount');
                if (badge) {
                    badge.style.display = active > 0 ? 'inline-block' : 'none';
                    badge.textContent   = active + ' active';
                }
            }
            updateFilterBadge();
        });
    </script>

    <script>
        // Add active class to current page
        document.addEventListener('DOMContentLoaded', function() {

            // Enhanced search with debounce
            const searchInput = document.getElementById('<%= txtSearch.ClientID %>');
            if (searchInput) {
                let searchTimeout;
                searchInput.addEventListener('input', function () {
                    clearTimeout(searchTimeout);
                    searchTimeout = setTimeout(() => {
                        if (this.value.length >= 3 || this.value.length === 0) {
                            this.form.submit();
                        }
                    }, 500);
                });
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
                transition: all 0.3s ease;
            `;

            sidebarToggle.classList.add('sidebar-toggle');
            document.body.appendChild(sidebarToggle);

            sidebarToggle.addEventListener('click', function () {
                const sidebar = document.querySelector('.sidebar');
                sidebar.classList.toggle('mobile-open');
                this.style.transform = sidebar.classList.contains('mobile-open') ? 'rotate(90deg)' : 'rotate(0)';
            });

            // Close sidebar when clicking outside on mobile
            document.addEventListener('click', function (e) {
                const sidebar = document.querySelector('.sidebar');
                const toggleBtn = document.querySelector('.sidebar-toggle');

                if (window.innerWidth <= 768 &&
                    sidebar.classList.contains('mobile-open') &&
                    !sidebar.contains(e.target) &&
                    e.target !== toggleBtn &&
                    !toggleBtn.contains(e.target)) {
                    sidebar.classList.remove('mobile-open');
                    toggleBtn.style.transform = 'rotate(0)';
                }
            });

            // Handle responsive behavior
            function handleResize() {
                const sidebar = document.querySelector('.sidebar');
                const toggleBtn = document.querySelector('.sidebar-toggle');

                if (!toggleBtn) return;

                if (window.innerWidth <= 768) {
                    toggleBtn.style.display = 'flex';
                    sidebar.classList.remove('mobile-open');
                    toggleBtn.style.transform = 'rotate(0)';
                } else {
                    toggleBtn.style.display = 'none';
                    sidebar.classList.remove('mobile-open');
                    sidebar.style.transform = 'none';
                }
            }

            window.addEventListener('resize', handleResize);
            handleResize();

            // Parallax effect for floating icons
            window.addEventListener('scroll', () => {
                const scrolled = window.pageYOffset;
                const floatingIcons = document.querySelectorAll('.floating-icon');
                
                floatingIcons.forEach((icon, index) => {
                    const speed = 0.5 + (index * 0.1);
                    const yPos = -(scrolled * speed);
                    icon.style.transform = `translateY(${yPos}px) rotate(${scrolled * 0.1}deg)`;
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