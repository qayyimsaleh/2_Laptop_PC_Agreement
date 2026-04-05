<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WindowsAuthDemo.Default" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Laptop & Desktop Acknowledgement Receipt Portal</title>
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
                <h2>Laptop & Desktop Acknowledgement Receipt</h2>
            </div>

            <ul class="nav-links">
                <% if (Session["IsAdmin"] != null && (bool)Session["IsAdmin"]) { %>
                <li class="nav-item">
                    <a href="Default.aspx" class="nav-link active">
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
                        <div class="user-name" style="font-weight: 600; color: white;">
                            <asp:Label ID="lblUserRoleSidebar" runat="server"></asp:Label>
                        </div>
                        <div style="font-size: 0.85rem; color: #94a3b8;">
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
                    <h1>Welcome to Laptop & Desktop Acknowledgement Receipt Portal</h1>
                    <p>Manage your laptop & desktop acknowledgement receipts and user permissions</p>
                </div>
                <div class="user-profile">
                    <i class="fas fa-user-circle"></i>
                    <div>
                        <div style="font-weight: 600;">
                            <asp:Label ID="lblUser" runat="server"></asp:Label>
                        </div>
                        <div style="font-size: 0.85rem; color: var(--text-secondary);">
                            <asp:Label ID="lblStatus" runat="server"></asp:Label>
                        </div>
                    </div>
                </div>
            </header>

            <!-- User Info Card -->
            <div class="dashboard-grid">
                <div class="dashboard-card">
                    <div class="card-header">
                        <div class="card-icon">
                            <i class="fas fa-user-shield"></i>
                        </div>
                        <asp:Label ID="lblUserRole" runat="server" CssClass="role-badge"></asp:Label>
                    </div>
                    
                    <h3 style="margin-bottom: 20px; color: var(--text-primary); font-size: 1.4rem; font-weight: 800;">User Information</h3>
                    
                    <div class="user-details">
                        <div class="detail-item">
                            <div class="detail-label">Windows Identity</div>
                            <div class="detail-value">
                                <i class="fas fa-user" style="color: var(--primary);"></i>
                                <asp:Label ID="lblUserName" runat="server"></asp:Label>
                            </div>
                        </div>
                        
                        <div class="detail-item">
                            <div class="detail-label">Authentication Type</div>
                            <div class="detail-value">
                                <i class="fas fa-lock" style="color: var(--primary);"></i>
                                <div id="infoAuthType" runat="server">Windows Integrated</div>
                            </div>
                        </div>
                        
                        <div class="detail-item">
                            <div class="detail-label">Session Status</div>
                            <div class="detail-value" style="color: var(--success);">
                                <i class="fas fa-check-circle"></i>
                                Active Session
                            </div>
                        </div>
                    </div>

                    <!-- Messages -->
                    <asp:Label ID="lblFirstAccess" runat="server" CssClass="first-access-notice" Visible="false">
                        <i class="fas fa-info-circle notice-icon"></i>
                        First time access detected. You have been registered as a normal user.
                    </asp:Label>
                    
                    <asp:Label ID="lblError" runat="server" CssClass="error-notice" Visible="false">
                        <i class="fas fa-exclamation-triangle notice-icon"></i>
                        <asp:Literal ID="litError" runat="server"></asp:Literal>
                    </asp:Label>
                </div>

                <!-- Stats Panel -->
                <div class="stats-grid">
                    <div class="stat-card">
                        <div class="stat-icon">
                            <i class="fas fa-users"></i>
                        </div>
                        <div class="stat-title">Total Users</div>
                        <div class="stat-value">
                            <asp:Label ID="lblTotalUsers" runat="server" Text="0"></asp:Label>
                        </div>
                    </div>
    
                    <div class="stat-card">
                        <div class="stat-icon">
                            <i class="fas fa-file-contract"></i>
                        </div>
                        <div class="stat-title">Acknowledgement Receipts</div>
                        <div class="stat-value">
                            <asp:Label ID="lblTotalAgreements" runat="server" Text="0"></asp:Label>
                        </div>
                    </div>
    
                    <div class="stat-card">
                        <div class="stat-icon">
                            <i class="fas fa-laptop"></i>
                        </div>
                        <div class="stat-title">Devices</div>
                        <div class="stat-value">
                            <asp:Label ID="lblTotalDevices" runat="server" Text="0"></asp:Label>
                        </div>
                    </div>
    
                    <div class="stat-card">
                        <div class="stat-icon">
                            <i class="fas fa-check-circle"></i>
                        </div>
                        <div class="stat-title">Active</div>
                        <div class="stat-value">
                            <asp:Label ID="lblActiveAgreements" runat="server" Text="0"></asp:Label>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Admin Panel -->
            <div id="adminPanel" runat="server" class="admin-panel" visible="false">
                <div class="admin-header">
                    <div class="admin-icon">
                        <i class="fas fa-user-cog"></i>
                    </div>
                    <div class="admin-title">
                        <h3>Administrator Controls</h3>
                        <p>Manage system settings and acknowledgement receipts</p>
                    </div>
                </div>
                
                <div class="admin-controls">
                    <a href="Agreement.aspx" class="admin-control">
                        <div class="control-icon">
                            <i class="fas fa-file-contract"></i>
                        </div>
                        <div class="control-title">Create New Acknowledgement Receipt</div>
                        <div class="control-desc">Generate new laptop & desktop acknowledgement receipts for employees</div>
                    </a>

                    <a href="ExistingAgreements.aspx" class="admin-control">
                        <div class="control-icon">
                            <i class="fas fa-list-alt"></i>
                        </div>
                        <div class="control-title">View Existing Acknowledgement Receipts</div>
                        <div class="control-desc">Manage and review all laptop & desktop acknowledgement receipts</div>
                    </a>

                    <a href="UserManagement.aspx" class="admin-control">
                        <div class="control-icon">
                            <i class="fas fa-user-plus"></i>
                        </div>
                        <div class="control-title">User Management</div>
                        <div class="control-desc">Add, remove, or modify user permissions</div>
                    </a>

                    <a href="ReportPage.aspx" class="admin-control">
                        <div class="control-icon">
                            <i class="fas fa-chart-pie"></i>
                        </div>
                        <div class="control-title">Analytics Dashboard</div>
                        <div class="control-desc">View system usage and acknowledgement statistics</div>
                    </a>
                </div>
            </div>

            <!-- Normal User Content -->
            <div id="normalUserContent" runat="server">
                <div class="dashboard-card">
                    <div class="card-header">
                        <div class="card-icon">
                            <i class="fas fa-info-circle"></i>
                        </div>
                        <div style="font-size: 0.9rem; color: var(--text-secondary);">Quick Actions</div>
                    </div>
                    
                    <h3 style="margin-bottom: 20px; color: var(--text-primary); font-size: 1.4rem; font-weight: 800;">Available Actions</h3>
                    
                    <div class="admin-controls">
                        <a href="ExistingAgreements.aspx" class="admin-control">
                            <div class="control-icon">
                                <i class="fas fa-file-contract"></i>
                            </div>
                            <div class="control-title">View Acknowledgement Receipt</div>
                            <div class="control-desc">Check your laptop & desktop acknowledgement receipt status</div>
                        </a>

                        <a href="mailto:qayyim@ioioleo.com?subject=Laptop/Desktop%20Agreement%20Support" class="admin-control">
                            <div class="control-icon">
                                <i class="fas fa-headset"></i>
                            </div>
                            <div class="control-title">Request Support</div>
                            <div class="control-desc">Get help with Laptop/Desktop issues</div>
                        </a>

                        <a href="#" class="admin-control">
                            <div class="control-icon">
                                <i class="fas fa-download"></i>
                            </div>
                            <div class="control-title">Download Documents</div>
                            <div class="control-desc">Access acknowledgement documents</div>
                        </a>

                        <a href="#" class="admin-control">
                            <div class="control-icon">
                                <i class="fas fa-history"></i>
                            </div>
                            <div class="control-title">View History</div>
                            <div class="control-desc">Check your Laptop/Desktop request history</div>
                        </a>
                    </div>
                </div>
            </div>

            <div class="footer">
                <p>Laptop & Desktop Acknowledgement Receipt Portal &copy; <%= DateTime.Now.Year %> | Last updated: <%= DateTime.Now.ToString("MMMM dd, yyyy HH:mm:ss") %></p>
                <p style="margin-top: 8px; font-size: 0.8rem; color: rgba(255, 255, 255, 0.8);">
                    Windows Authentication | Secure Enterprise Portal
                </p>
            </div>
        </main>
    </form>

    <script>
        // ── Theme toggle (persisted in localStorage) ──────────────────
        (function () {
            const saved = localStorage.getItem('portalTheme') || 'light';
            document.documentElement.setAttribute('data-theme', saved);
        })();

        document.addEventListener('DOMContentLoaded', function () {

            function applyTheme(theme) {
                document.documentElement.setAttribute('data-theme', theme);
                localStorage.setItem('portalTheme', theme);
                const btn = document.getElementById('themeToggleBtn');
                if (!btn) return;
                if (theme === 'dark') {
                    btn.innerHTML = '<i class="fas fa-sun"></i><span>Light Mode</span>';
                } else {
                    btn.innerHTML = '<i class="fas fa-moon"></i><span>Dark Mode</span>';
                }
            }

            applyTheme(localStorage.getItem('portalTheme') || 'light');

            const themeBtn = document.getElementById('themeToggleBtn');
            if (themeBtn) {
                themeBtn.addEventListener('click', function () {
                    const current = document.documentElement.getAttribute('data-theme') || 'light';
                    applyTheme(current === 'dark' ? 'light' : 'dark');
                });
            }



            // ── Mobile sidebar toggle ──────────────────────────────────
            const sidebarToggle = document.createElement('button');
            sidebarToggle.innerHTML = '<i class="fas fa-bars"></i>';
            sidebarToggle.className = 'sidebar-toggle';
            document.body.appendChild(sidebarToggle);

            sidebarToggle.addEventListener('click', function () {
                const sidebar = document.querySelector('.sidebar');
                sidebar.classList.toggle('mobile-open');
                this.style.transform = sidebar.classList.contains('mobile-open') ? 'rotate(90deg)' : '';
            });

            document.addEventListener('click', function (e) {
                const sidebar   = document.querySelector('.sidebar');
                const toggleBtn = document.querySelector('.sidebar-toggle');
                if (window.innerWidth <= 768 &&
                    sidebar.classList.contains('mobile-open') &&
                    !sidebar.contains(e.target) &&
                    !toggleBtn.contains(e.target)) {
                    sidebar.classList.remove('mobile-open');
                    toggleBtn.style.transform = '';
                }
            });

            function handleResize() {
                const toggleBtn = document.querySelector('.sidebar-toggle');
                const sidebar   = document.querySelector('.sidebar');
                if (!toggleBtn) return;
                if (window.innerWidth <= 768) {
                    toggleBtn.style.display = 'flex';
                    sidebar.classList.remove('mobile-open');
                } else {
                    toggleBtn.style.display = 'none';
                    sidebar.classList.remove('mobile-open');
                }
            }
            window.addEventListener('resize', handleResize);
            handleResize();

            // ── Support email helper ───────────────────────────────────

            // ── Parallax floating icons ────────────────────────────────
            window.addEventListener('scroll', function () {
                const scrolled = window.pageYOffset;
                document.querySelectorAll('.floating-icon').forEach(function (icon, i) {
                    const speed = 0.5 + i * 0.1;
                    icon.style.transform = 'translateY(' + (-(scrolled * speed)) + 'px) rotate(' + (scrolled * 0.1) + 'deg)';
                });
            });

            // ── Animate stat numbers ───────────────────────────────────
            document.querySelectorAll('.stat-value').forEach(function (stat) {
                const final = parseInt(stat.textContent);
                if (!isNaN(final) && final > 0) {
                    let val = 0;
                    const inc = final / (1500 / 16);
                    const timer = setInterval(function () {
                        val += inc;
                        if (val >= final) { stat.textContent = final.toLocaleString(); clearInterval(timer); }
                        else              { stat.textContent = Math.floor(val).toLocaleString(); }
                    }, 16);
                }
            });
        });
    </script>
    <!-- Theme toggle — floating button, always visible at any zoom -->
    <button id="themeToggleBtn" class="theme-toggle" type="button">
        <i class="fas fa-moon"></i><span>Dark Mode</span>
    </button>

</body>
