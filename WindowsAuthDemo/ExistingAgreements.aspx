<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ExistingAgreements.aspx.cs" Inherits="WindowsAuthDemo.ExistingAgreements" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Agreements Management</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
    <link rel="stylesheet" href="hardware-portal-styles.css">
</head>
<body>
    <!-- Floating Icons -->
    <div class="floating-icon"><i class="fas fa-laptop"></i></div>
    <div class="floating-icon"><i class="fas fa-microchip"></i></div>
    <div class="floating-icon"><i class="fas fa-keyboard"></i></div>
    <div class="floating-icon"><i class="fas fa-server"></i></div>
    <div class="floating-icon"><i class="fas fa-hdd"></i></div>

    <form id="form1" runat="server">
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
                    <a href="Agreement.aspx" class="nav-link">
                        <i class="fas fa-file-contract"></i>
                        <span>New Agreement</span>
                    </a>
                </li>
                <li class="nav-item">
                    <a href="ExistingAgreements.aspx" class="nav-link active">
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
                    <h1>Agreements Management</h1>
                    <p>View, search, and manage all Laptop/PC agreements in the system</p>
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
                <div class="stat-card">
                    <div class="stat-icon">
                        <i class="fas fa-file-alt"></i>
                    </div>
                    <div class="stat-value">
                        <asp:Literal ID="litTotal" runat="server" Text="0"></asp:Literal>
                    </div>
                    <div class="stat-title">Total Agreements</div>
                </div>

                <div class="stat-card">
                    <div class="stat-icon">
                        <i class="fas fa-edit"></i>
                    </div>
                    <div class="stat-value">
                        <asp:Literal ID="litDrafts" runat="server" Text="0"></asp:Literal>
                    </div>
                    <div class="stat-title">Drafts</div>
                </div>

                <div class="stat-card">
                    <div class="stat-icon">
                        <i class="fas fa-clock"></i>
                    </div>
                    <div class="stat-value">
                        <asp:Literal ID="litPending" runat="server" Text="0"></asp:Literal>
                    </div>
                    <div class="stat-title">Pending</div>
                </div>

                <div class="stat-card">
                    <div class="stat-icon">
                        <i class="fas fa-handshake"></i>
                    </div>
                    <div class="stat-value">
                        <asp:Literal ID="litAgreed" runat="server" Text="0"></asp:Literal>
                    </div>
                    <div class="stat-title">Agreed</div>
                </div>

                <div class="stat-card">
                    <div class="stat-icon">
                        <i class="fas fa-check-circle"></i>
                    </div>
                    <div class="stat-value">
                        <asp:Literal ID="litCompleted" runat="server" Text="0"></asp:Literal>
                    </div>
                    <div class="stat-title">Completed</div>
                </div>

                <div class="stat-card">
                    <div class="stat-icon">
                        <i class="fas fa-archive"></i>
                    </div>
                    <div class="stat-value">
                        <asp:Literal ID="litArchived" runat="server" Text="0"></asp:Literal>
                    </div>
                    <div class="stat-title">Archived</div>
                </div>
            </div>

            <!-- Filters Section -->
            <div class="filters-section">
                <div class="filters-header">
                    <i class="fas fa-filter"></i>
                    <h3>Filter Agreements</h3>
                </div>
                <div class="filters-grid">
                    <div class="filter-group">
                        <label class="filter-label">Status</label>
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
                        <label class="filter-label">Sort By</label>
                        <asp:DropDownList ID="ddlSortBy" runat="server" CssClass="filter-select" 
                            AutoPostBack="true" OnSelectedIndexChanged="ddlSortBy_SelectedIndexChanged">
                            <asp:ListItem Value="created_date">Newest First</asp:ListItem>
                            <asp:ListItem Value="created_date ASC">Oldest First</asp:ListItem>
                            <asp:ListItem Value="agreement_number">Agreement Number</asp:ListItem>
                            <asp:ListItem Value="agreement_status">Status</asp:ListItem>
                        </asp:DropDownList>
                    </div>

                    <div class="filter-group">
                        <label class="filter-label">Search</label>
                        <asp:TextBox ID="txtSearch" runat="server" CssClass="search-input" 
                            placeholder="Search by agreement no, serial, asset, employee, model..." 
                            AutoPostBack="true" OnTextChanged="txtSearch_TextChanged"></asp:TextBox>
                    </div>
                </div>
            </div>

            <!-- Agreements Table -->
            <div class="table-container">
                <div class="table-header">
                    <div class="table-title">
                        <i class="fas fa-list"></i>
                        All Agreements
                    </div>
                    <div class="table-controls">
                        <span class="filter-label" style="font-size: 0.85rem;">
                            Showing <strong><asp:Literal ID="litShowingCount" runat="server" Text="0"></asp:Literal></strong> of <strong><asp:Literal ID="litTotalCount" runat="server" Text="0"></asp:Literal></strong> agreements
                        </span>
                    </div>
                </div>

                <!-- Agreements Grid -->
                <asp:GridView ID="gvAgreements" runat="server" CssClass="table" 
                    AutoGenerateColumns="false" OnRowCommand="gvAgreements_RowCommand"
                    OnRowDataBound="gvAgreements_RowDataBound"
                    ShowHeaderWhenEmpty="true">
                    <Columns>
                        <asp:TemplateField HeaderText="Agreement Number">
                            <ItemTemplate>
                                <div style="font-weight: 600; color: var(--text-primary);">
                                    <%# Eval("agreement_number") %>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:BoundField DataField="model" HeaderText="Model" 
                            ItemStyle-CssClass="text-secondary" />

                        <asp:TemplateField HeaderText="Serial / Asset">
                            <ItemTemplate>
                                <div style="font-size: 0.85rem;">
                                    <div><strong>S/N:</strong> <%# Eval("serial_number") %></div>
                                    <div><strong>A/N:</strong> <%# Eval("asset_number") %></div>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Status">
                            <ItemTemplate>
                                <span class='status-badge status-<%# Eval("agreement_status").ToString().ToLower() %>'>
                                    <i class="fas fa-circle" style="font-size: 0.5rem;"></i>
                                    <%# Eval("agreement_status") %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Employee">
                            <ItemTemplate>
                                <div style="font-size: 0.85rem;">
                                    <div style="font-weight: 600;"><%# Eval("employee_name") != null && Eval("employee_name") != DBNull.Value && Eval("employee_name").ToString() != "" ? Eval("employee_name") : "<span style='color:#9ca3af;'>—</span>" %></div>
                                    <div style="font-size: 0.75rem; color: var(--text-secondary);">
                                        <i class="fas fa-envelope" style="margin-right: 4px;"></i>
                                        <%# Eval("employee_email") != null && Eval("employee_email") != DBNull.Value && Eval("employee_email").ToString() != "" ? Eval("employee_email") : "—" %>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="IT Staff">
                            <ItemTemplate>
                                <div style="font-size: 0.85rem;">
                                    <i class="fas fa-user" style="color: var(--text-secondary); margin-right: 6px;"></i>
                                    <%# Eval("it_staff_win_id") %>
                                </div>
                                <div style="font-size: 0.75rem; color: var(--text-secondary);">
                                    <i class="fas fa-calendar" style="margin-right: 4px;"></i>
                                    <%# Convert.ToDateTime(Eval("issue_date")).ToString("dd MMM yyyy") %>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Actions">
                            <ItemTemplate>
                                <div class="action-buttons">
                                    <!-- Edit button - only for Draft status -->
                                    <asp:LinkButton ID="btnEdit" runat="server" CssClass="btn-action btn-edit"
                                        CommandName="EditAgreement" CommandArgument='<%# Eval("id") %>'
                                        ToolTip="Edit Agreement" Visible="false">
                                        <i class="fas fa-edit"></i>
                                    </asp:LinkButton>
                    
                                    <!-- View button - for all statuses -->
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
                            <div class="no-data-icon">
                                <i class="fas fa-file-contract"></i>
                            </div>
                            <h3>No Agreements Found</h3>
                            <p>Try adjusting your filters or create a new agreement</p>
                        </div>
                    </EmptyDataTemplate>
                </asp:GridView>

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

            <!-- Footer -->
            <div class="footer">
                <p>Laptop/PC Agreement Portal &copy; <%= DateTime.Now.Year %> | Last updated: <%= DateTime.Now.ToString("MMMM dd, yyyy HH:mm:ss") %></p>
                <p style="margin-top: 8px; font-size: 0.8rem; color: rgba(255, 255, 255, 0.8);">
                    Windows Authentication | Secure Enterprise Portal
                </p>
            </div>
        </main>
    </form>

    <script>
        // Add active class to current page
        document.addEventListener('DOMContentLoaded', function() {
            const currentPage = window.location.pathname.split('/').pop();
            const navLinks = document.querySelectorAll('.nav-link');
            
            navLinks.forEach(link => {
                if (link.getAttribute('href') === currentPage) {
                    link.classList.add('active');
                }
            });

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

            // Add hover effects to all interactive elements
            const interactiveElements = document.querySelectorAll('.stat-card, .btn-action, .nav-link, .page-link');
            interactiveElements.forEach(element => {
                element.addEventListener('mouseenter', () => {
                    element.style.zIndex = '10';
                });
                element.addEventListener('mouseleave', () => {
                    element.style.zIndex = '';
                });
            });

            // Add email body function for support link
            window.setEmailBody = function(link) {
                const user = document.getElementById('<%= lblUser.ClientID %>')?.textContent || '[Your Windows ID]';
                const page = document.title || '[Current Page]';
                const body = `Hello Support Team,\n\nI need assistance with:\n\n\n\nWindows ID: ${user}\nPage: ${page}`;
                link.href = link.href.replace(/body=.*/, 'body=' + encodeURIComponent(body));
                return true;
            }

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
</body>
</html>