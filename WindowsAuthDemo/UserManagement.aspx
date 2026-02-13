<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UserManagement.aspx.cs" Inherits="WindowsAuthDemo.UserManagement" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>User Management - Laptop/PC Agreement Portal</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
    <link rel="stylesheet" href="hardware-portal-styles.css">
</head>
<body>
    <!-- Floating Icons -->
    <i class="fas fa-laptop floating-icon"></i>
    <i class="fas fa-desktop floating-icon"></i>
    <i class="fas fa-server floating-icon"></i>
    <i class="fas fa-keyboard floating-icon"></i>
    <i class="fas fa-mouse floating-icon"></i>

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
                    <a href="ExistingAgreements.aspx" class="nav-link">
                        <i class="fas fa-list-alt"></i>
                        <span>Agreements</span>
                    </a>
                </li>
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
                <div style="display: flex; align-items: center; gap: 16px;">
                    <div class="user-avatar">
                        <i class="fas fa-user"></i>
                    </div>
                    <div>
                        <div class="user-name" style="font-weight: 700; font-size: 1.1rem;">
                            <asp:Label ID="lblUserRoleSidebar" runat="server"></asp:Label>
                        </div>
                        <div style="font-size: 0.95rem; color: #94a3b8;">
                            <asp:Label ID="lblUserSidebar" runat="server"></asp:Label>
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
                    <h1>User Management</h1>
                    <p>Add, remove, or modify user permissions</p>
                </div>
                <div class="user-profile">
                    <i class="fas fa-user-circle"></i>
                    <div>
                        <div style="font-weight: 700; font-size: 1.2rem;">
                            <asp:Label ID="lblUser" runat="server"></asp:Label>
                        </div>
                        <div style="font-size: 0.95rem; color: var(--text-secondary);">
                            <asp:Label ID="lblStatus" runat="server"></asp:Label>
                        </div>
                    </div>
                </div>
            </header>

            <!-- Access Check -->
            <asp:Panel ID="pnlAccessDenied" runat="server" Visible="false">
                <div class="access-denied">
                    <div class="denied-icon">
                        <i class="fas fa-ban"></i>
                    </div>
                    <h2 class="denied-title">Access Denied</h2>
                    <p class="denied-message">
                        You don't have administrator privileges to access the User Management page.
                        Only users with administrator role can manage user permissions.
                    </p>
                    <a href="Default.aspx" class="btn btn-primary">
                        <i class="fas fa-arrow-left"></i>
                        Back to Dashboard
                    </a>
                </div>
            </asp:Panel>

            <!-- User Management Content (Visible only for admins) -->
            <asp:Panel ID="pnlUserManagement" runat="server" Visible="false">
                <div class="user-management-container">
                    <!-- Messages -->
                    <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="alert">
                        <i class="fas fa-info-circle"></i>
                        <asp:Literal ID="litMessage" runat="server"></asp:Literal>
                    </asp:Panel>

                    <!-- Add/Edit User Form -->
                    <div class="form-container">
                        <div class="section-header">
                            <div class="section-icon">
                                <i class="fas fa-user-edit"></i>
                            </div>
                            <div class="section-title">
                                <h2>
                                    <asp:Literal ID="litFormTitle" runat="server" Text="Add New User"></asp:Literal>
                                </h2>
                                <p>Enter user details below</p>
                            </div>
                        </div>

                        <asp:HiddenField ID="hdnUserId" runat="server" />

                        <div class="form-grid">
                            <div class="form-group">
                                <label class="form-label">Windows ID *</label>
                                <asp:TextBox ID="txtWinId" runat="server" CssClass="form-control" 
                                    placeholder="DOMAIN\username" required="true"></asp:TextBox>
                                <small style="color: var(--text-secondary); font-size: 0.95rem; margin-top: 8px; display: block; padding-left: 12px;">
                                    <i class="fas fa-info-circle"></i> Format: DOMAIN\username (e.g., COMPANY\john.doe)
                                </small>
                            </div>

                            <div class="form-group">
                                <label class="form-label">Email Address *</label>
                                <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" 
                                    placeholder="user@company.com" TextMode="Email" required="true"></asp:TextBox>
                            </div>
                        </div>

                        <div class="form-grid">
                            <div class="form-group">
                                <label class="form-label">User Status</label>
                                <asp:DropDownList ID="ddlActive" runat="server" CssClass="form-select">
                                    <asp:ListItem Value="1" Text="Active" Selected="True"></asp:ListItem>
                                    <asp:ListItem Value="0" Text="Inactive"></asp:ListItem>
                                </asp:DropDownList>
                            </div>

                            <div class="form-group">
                                <label class="form-label">User Role</label>
                                <asp:DropDownList ID="ddlAdmin" runat="server" CssClass="form-select">
                                    <asp:ListItem Value="0" Text="Normal User" Selected="True"></asp:ListItem>
                                    <asp:ListItem Value="1" Text="Administrator"></asp:ListItem>
                                </asp:DropDownList>
                                <small style="color: var(--text-secondary); font-size: 0.95rem; margin-top: 8px; display: block; padding-left: 12px;">
                                    <i class="fas fa-shield-alt"></i> Administrators can access all system features
                                </small>
                            </div>
                        </div>

                        <div class="form-actions">
                            <asp:Button ID="btnCancel" runat="server" Text="Cancel" CssClass="btn btn-secondary"
                                OnClick="btnCancel_Click" Visible="false" />
                            <asp:Button ID="btnSave" runat="server" Text="Save User" CssClass="btn btn-success"
                                OnClick="btnSave_Click" />
                            <asp:Button ID="btnClear" runat="server" Text="Clear Form" CssClass="btn btn-secondary"
                                OnClick="btnClear_Click" />
                        </div>
                    </div>

                    <!-- Users List -->
                    <div class="table-container">
                        <div class="table-header">
                            <h3 class="table-title">All Users</h3>
                            <div class="search-box">
                                <i class="fas fa-search search-icon"></i>
                                <asp:TextBox ID="txtSearch" runat="server" CssClass="search-input" 
                                    placeholder="Search users..." AutoPostBack="true" OnTextChanged="txtSearch_TextChanged"></asp:TextBox>
                            </div>
                        </div>

                        <div class="table-responsive">
                            <asp:GridView ID="gvUsers" runat="server" AutoGenerateColumns="false" 
                                CssClass="users-table" DataKeyNames="win_id"
                                OnRowEditing="gvUsers_RowEditing" OnRowDeleting="gvUsers_RowDeleting"
                                AllowPaging="true" PageSize="10" OnPageIndexChanging="gvUsers_PageIndexChanging">
                                <Columns>
                                    <asp:BoundField DataField="win_id" HeaderText="Windows ID" SortExpression="win_id" />
                                    <asp:BoundField DataField="email" HeaderText="Email" SortExpression="email" />
                                    <asp:TemplateField HeaderText="Status" SortExpression="active">
                                        <ItemTemplate>
                                            <span class='status-badge <%# Convert.ToInt32(Eval("active")) == 1 ? "status-active" : "status-inactive" %>'>
                                                <%# Convert.ToInt32(Eval("active")) == 1 ? "Active" : "Inactive" %>
                                            </span>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Role" SortExpression="admin">
                                        <ItemTemplate>
                                            <span class='role-badge <%# Convert.ToInt32(Eval("admin")) == 1 ? "role-admin" : "role-user" %>'>
                                                <%# Convert.ToInt32(Eval("admin")) == 1 ? "Administrator" : "Normal User" %>
                                            </span>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Actions">
                                        <ItemTemplate>
                                            <div class="action-buttons">
                                                <asp:LinkButton ID="btnEdit" runat="server" CommandName="Edit" 
                                                    CssClass="btn btn-sm btn-edit" ToolTip="Edit User">
                                                    <i class="fas fa-edit"></i> Edit
                                                </asp:LinkButton>
                                                <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" 
                                                    CssClass="btn btn-sm btn-delete" ToolTip="Delete User"
                                                    OnClientClick="return confirm('Are you sure you want to delete this user?');">
                                                    <i class="fas fa-trash"></i> Delete
                                                </asp:LinkButton>
                                            </div>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                                <EmptyDataTemplate>
                                    <div style="text-align: center; padding: 60px; color: var(--text-secondary);">
                                        <i class="fas fa-users" style="font-size: 4rem; margin-bottom: 24px; opacity: 0.3;"></i>
                                        <h3 style="font-size: 1.6rem; margin-bottom: 16px; color: var(--text-primary);">No users found</h3>
                                        <p style="font-size: 1.1rem;">Start by adding a new user using the form above.</p>
                                    </div>
                                </EmptyDataTemplate>
                                <PagerStyle CssClass="pagination" />
                            </asp:GridView>
                        </div>
                    </div>

                    <!-- Audit Logs Section -->
                    <div class="table-container" style="margin-top: 40px;">
                        <div class="table-header">
                            <h3 class="table-title">
                                <i class="fas fa-history" style="margin-right: 12px;"></i>
                                Audit Logs
                            </h3>
                            <div style="display: flex; gap: 16px; align-items: center;">
                                <div style="font-size: 0.95rem; color: var(--text-secondary);">
                                    Last 50 actions
                                </div>
                                <asp:Button ID="btnViewLogs" runat="server" Text="Refresh Logs" 
                                    CssClass="btn btn-sm btn-secondary" OnClick="btnViewLogs_Click" />
                            </div>
                        </div>
    
                        <div class="table-responsive">
                            <asp:GridView ID="gvAuditLogs" runat="server" AutoGenerateColumns="false" 
                                CssClass="users-table" AllowPaging="true" PageSize="10">
                                <Columns>
                                    <asp:BoundField DataField="timestamp" HeaderText="Timestamp" 
                                        DataFormatString="{0:dd/MM/yyyy HH:mm:ss}" SortExpression="timestamp" />
                                    <asp:BoundField DataField="admin_user" HeaderText="Admin User" SortExpression="admin_user" />
                                    <asp:TemplateField HeaderText="Action Type" SortExpression="action_type">
                                        <ItemTemplate>
                                            <span class='status-badge 
                                                <%# Eval("action_type").ToString() == "CREATE" ? "status-active" : 
                                                   Eval("action_type").ToString() == "UPDATE" ? "status-inactive" : 
                                                   Eval("action_type").ToString() == "DELETE" ? "role-user" : "" %>'>
                                                <%# Eval("action_type") %>
                                            </span>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField DataField="target_user" HeaderText="Target User" SortExpression="target_user" />
                                    <asp:BoundField DataField="description" HeaderText="Description" 
                                        ItemStyle-Width="300px" ItemStyle-CssClass="text-truncate" />
                                </Columns>
                                <EmptyDataTemplate>
                                    <div style="text-align: center; padding: 60px; color: var(--text-secondary);">
                                        <i class="fas fa-history" style="font-size: 4rem; margin-bottom: 24px; opacity: 0.3;"></i>
                                        <h3 style="font-size: 1.6rem; margin-bottom: 16px; color: var(--text-primary);">No audit logs found</h3>
                                        <p style="font-size: 1.1rem;">Audit logs will appear here when actions are performed.</p>
                                    </div>
                                </EmptyDataTemplate>
                                <PagerStyle CssClass="pagination" />
                            </asp:GridView>
                        </div>
                    </div>
                </div>
            </asp:Panel>

            <div class="footer">
                <p>User Management &copy; <%= DateTime.Now.Year %> | Last updated: <%= DateTime.Now.ToString("MMMM dd, yyyy HH:mm:ss") %></p>
                <p style="margin-top: 16px; font-size: 0.9rem; color: rgba(255, 255, 255, 0.8);">
                    <i class="fas fa-lock"></i> Windows Authentication | <i class="fas fa-shield-alt"></i> Secure Enterprise Portal
                </p>
            </div>
        </main>
    </form>

    <script>
        // Add active class to current page
        document.addEventListener('DOMContentLoaded', function () {
            const currentPage = window.location.pathname.split('/').pop();
            const navLinks = document.querySelectorAll('.nav-link');

            navLinks.forEach(link => {
                if (link.getAttribute('href') === currentPage) {
                    link.classList.add('active');
                }
            });

            // Add sidebar toggle for mobile
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
                width: 50px;
                height: 50px;
                border-radius: 12px;
                display: none;
                align-items: center;
                justify-content: center;
                cursor: pointer;
                box-shadow: var(--shadow-lg);
                font-size: 1.4rem;
                transition: all 0.3s ease;
            `;

            document.body.appendChild(sidebarToggle);

            sidebarToggle.addEventListener('click', function () {
                document.querySelector('.sidebar').classList.toggle('mobile-open');
                this.style.transform = 'rotate(90deg)';
                setTimeout(() => {
                    this.style.transform = 'rotate(0)';
                }, 300);
            });

            // Responsive adjustments
            function handleResize() {
                if (window.innerWidth <= 768) {
                    sidebarToggle.style.display = 'flex';
                } else {
                    sidebarToggle.style.display = 'none';
                    document.querySelector('.sidebar').classList.remove('mobile-open');
                }
            }

            window.addEventListener('resize', handleResize);
            handleResize();

            // Add floating effect to cards on hover
            const cards = document.querySelectorAll('.form-container, .table-container, .kpi-card, .chart-card');
            cards.forEach(card => {
                card.addEventListener('mouseenter', function() {
                    this.style.transform = 'translateY(-10px)';
                });
                
                card.addEventListener('mouseleave', function() {
                    this.style.transform = 'translateY(0)';
                });
            });

            // Add typing animation to form placeholders
            const inputs = document.querySelectorAll('.form-control[placeholder]');
            inputs.forEach(input => {
                const originalPlaceholder = input.placeholder;
                let i = 0;
                
                input.addEventListener('focus', function() {
                    if (this.placeholder === originalPlaceholder) {
                        this.placeholder = '';
                        const typingInterval = setInterval(() => {
                            if (i < originalPlaceholder.length) {
                                this.placeholder += originalPlaceholder.charAt(i);
                                i++;
                            } else {
                                clearInterval(typingInterval);
                                i = 0;
                            }
                        }, 50);
                    }
                });

                input.addEventListener('blur', function() {
                    this.placeholder = originalPlaceholder;
                    i = 0;
                });
            });

            // Add ripple effect to buttons
            const buttons = document.querySelectorAll('.btn');
            buttons.forEach(button => {
                button.addEventListener('click', function(e) {
                    const ripple = document.createElement('span');
                    const rect = this.getBoundingClientRect();
                    const size = Math.max(rect.width, rect.height);
                    const x = e.clientX - rect.left - size / 2;
                    const y = e.clientY - rect.top - size / 2;
                    
                    ripple.style.cssText = `
                        position: absolute;
                        border-radius: 50%;
                        background: rgba(255, 255, 255, 0.5);
                        transform: scale(0);
                        animation: ripple 0.6s linear;
                        width: ${size}px;
                        height: ${size}px;
                        left: ${x}px;
                        top: ${y}px;
                    `;
                    
                    this.appendChild(ripple);
                    
                    setTimeout(() => {
                        ripple.remove();
                    }, 600);
                });
            });

            // Add keyframe for ripple animation
            const style = document.createElement('style');
            style.textContent = `
                @keyframes ripple {
                    to {
                        transform: scale(4);
                        opacity: 0;
                    }
                }
            `;
            document.head.appendChild(style);

            // Add scroll animation
            const observerOptions = {
                threshold: 0.1,
                rootMargin: '0px 0px -50px 0px'
            };

            const observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        entry.target.style.animationPlayState = 'running';
                    }
                });
            }, observerOptions);

            // Observe all animated elements
            document.querySelectorAll('.form-container, .table-container, .section-header').forEach(el => {
                observer.observe(el);
            });

            // Add keyboard shortcut for search
            document.addEventListener('keydown', function(e) {
                if (e.ctrlKey && e.key === 'k') {
                    e.preventDefault();
                    const searchInput = document.querySelector('.search-input');
                    if (searchInput) {
                        searchInput.focus();
                    }
                }
            });

            // Add hover effect to table rows
            const tableRows = document.querySelectorAll('.users-table tr');
            tableRows.forEach(row => {
                row.addEventListener('mouseenter', function() {
                    this.style.transition = 'all 0.3s ease';
                });
            });

            // Add loading animation to form submission (disable after postback starts)
            const saveButton = document.getElementById('<%= btnSave.ClientID %>');
            if (saveButton) {
                saveButton.addEventListener('click', function() {
                    const btn = this;
                    const originalText = btn.innerHTML;
                    // Use setTimeout so the postback fires BEFORE we disable
                    setTimeout(function() {
                        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving...';
                        btn.disabled = true;
                    }, 50);
                });
            }
        });
    </script>
</body>
</html>