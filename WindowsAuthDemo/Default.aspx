<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WindowsAuthDemo.Default" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Laptop/PC Agreement Portal</title>
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
                <h2>Laptop/PC Agreement Portal</h2>
            </div>

            <ul class="nav-links">
                <li class="nav-item">
                    <a href="#" class="nav-link active">
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
                    <a href="mailto:qayyim@ioioleo.com?subject=Laptop/PC%20Agreement%20Portal%20Support&body=Hello%20Support%20Team,%0A%0AI%20need%20assistance%20with:%0A%0A%0A%0AWindows%20ID:%20[Your%20Windows%20ID]%0APage:%20[Current%20Page]" 
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
                    <h1>Welcome to Laptop/PC Agreement Portal</h1>
                    <p>Manage your Laptop/PC agreements and user permissions</p>
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
                        <div class="stat-title">Agreements</div>
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
                        <p>Manage system settings and agreements</p>
                    </div>
                </div>
                
                <div class="admin-controls">
                    <a href="Agreement.aspx" class="admin-control">
                        <div class="control-icon">
                            <i class="fas fa-file-contract"></i>
                        </div>
                        <div class="control-title">Create New Agreement</div>
                        <div class="control-desc">Generate new Laptop/PC agreements for employees</div>
                    </a>

                    <a href="ExistingAgreements.aspx" class="admin-control">
                        <div class="control-icon">
                            <i class="fas fa-list-alt"></i>
                        </div>
                        <div class="control-title">View Existing Agreements</div>
                        <div class="control-desc">Manage and review all Laptop/PC agreements</div>
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
                        <div class="control-desc">View system usage and agreement statistics</div>
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
                            <div class="control-title">View Agreement</div>
                            <div class="control-desc">Check your Laptop/PC agreement status</div>
                        </a>

                        <a href="mailto:qayyim@ioioleo.com?subject=Laptop/PC%20Agreement%20Support" class="admin-control">
                            <div class="control-icon">
                                <i class="fas fa-headset"></i>
                            </div>
                            <div class="control-title">Request Support</div>
                            <div class="control-desc">Get help with Laptop/PC issues</div>
                        </a>

                        <a href="#" class="admin-control">
                            <div class="control-icon">
                                <i class="fas fa-download"></i>
                            </div>
                            <div class="control-title">Download Documents</div>
                            <div class="control-desc">Access agreement documents</div>
                        </a>

                        <a href="#" class="admin-control">
                            <div class="control-icon">
                                <i class="fas fa-history"></i>
                            </div>
                            <div class="control-title">View History</div>
                            <div class="control-desc">Check your Laptop/PC request history</div>
                        </a>
                    </div>
                </div>
            </div>

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
        document.addEventListener('DOMContentLoaded', function () {
            const currentPage = window.location.pathname.split('/').pop();
            const navLinks = document.querySelectorAll('.nav-link');

            navLinks.forEach(link => {
                if (link.getAttribute('href') === currentPage ||
                    (currentPage === '' && link.getAttribute('href') === 'Default.aspx')) {
                    link.classList.add('active');
                }
            });

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
            const interactiveElements = document.querySelectorAll('.dashboard-card, .stat-card, .admin-control, .nav-link');
            interactiveElements.forEach(element => {
                element.addEventListener('mouseenter', () => {
                    element.style.zIndex = '10';
                });
                element.addEventListener('mouseleave', () => {
                    element.style.zIndex = '';
                });
            });

            // Animate numbers in stats
            const statValues = document.querySelectorAll('.stat-value');
            statValues.forEach(stat => {
                const finalValue = parseInt(stat.textContent);
                if (!isNaN(finalValue) && finalValue > 0) {
                    let startValue = 0;
                    const duration = 1500;
                    const increment = finalValue / (duration / 16);
                    
                    const timer = setInterval(() => {
                        startValue += increment;
                        if (startValue >= finalValue) {
                            stat.textContent = finalValue.toLocaleString();
                            clearInterval(timer);
                        } else {
                            stat.textContent = Math.floor(startValue).toLocaleString();
                        }
                    }, 16);
                }
            });

            // Add email body function for support link
            window.setEmailBody = function(link) {
                const user = document.getElementById('<%= lblUser.ClientID %>')?.textContent || '[Your Windows ID]';
                const page = document.title || '[Current Page]';
                const body = `Hello Support Team,\n\nI need assistance with:\n\n\n\nWindows ID: ${user}\nPage: ${page}`;
                link.href = link.href.replace(/body=.*/, 'body=' + encodeURIComponent(body));
                return true;
            }
        });

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
    </script>
</body>
</html>