<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ReportPage.aspx.cs" Inherits="WindowsAuthDemo.ReportPage" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title>Reports & Analytics - Laptop & Desktop Acknowledgement Receipt Portal</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta http-equiv="X-Content-Type-Options" content="nosniff">
    <meta name="referrer" content="strict-origin-when-cross-origin">
    <link href="https://fonts.googleapis.com/css2?family=Sora:wght@300;400;500;600;700;800&family=DM+Mono:ital,wght@0,400;0,500;1,400&display=swap" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
    <link rel="stylesheet" href="hardware-portal-styles.css">
    <script>document.documentElement.setAttribute("data-theme",localStorage.getItem("portalTheme")||"light");</script>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
</head>
<body>
    <div class="floating-icon"><i class="fas fa-laptop"></i></div>
    <div class="floating-icon"><i class="fas fa-microchip"></i></div>
    <div class="floating-icon"><i class="fas fa-keyboard"></i></div>
    <div class="floating-icon"><i class="fas fa-server"></i></div>
    <div class="floating-icon"><i class="fas fa-hdd"></i></div>

    <form id="form1" runat="server">
        <!-- Sidebar -->
        <aside class="sidebar">
            <div class="sidebar-header"><i class="fas fa-laptop-code"></i><h2>Laptop & Desktop Acknowledgement Receipt</h2></div>
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
                    <a href="UserManagement.aspx" class="nav-link">
                        <i class="fas fa-users"></i>
                        <span>Users</span>
                    </a>
                </li>
                <li class="nav-item">
                    <a href="ReportPage.aspx" class="nav-link active">
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
                        <div class="user-name" style="font-weight:600;color:white;"><asp:Label ID="lblUserRoleSidebar" runat="server" /></div>
                        <div style="font-size:0.85rem;color:#94a3b8;"><asp:Label ID="lblUserSidebar" runat="server" /></div>
                    </div>
                </div>
            </div>
        </aside>

        <!-- Main Content -->
        <main class="main-content">
            <header class="top-header">
                <div class="page-title"><h1>Reports & Analytics</h1><p>Comprehensive insights from laptop & desktop acknowledgement receipts data</p></div>
                <div class="user-profile">
                    <i class="fas fa-user-circle"></i>
                    <div>
                        <div style="font-weight:600;"><asp:Label ID="lblUser" runat="server" /></div>
                        <div style="font-size:0.85rem;color:var(--text-secondary);"><asp:Label ID="lblStatus" runat="server" /></div>
                    </div>
                </div>
            </header>

            <!-- Access Denied -->
            <asp:Panel ID="pnlAccessDenied" runat="server" Visible="false">
                <div class="access-denied">
                    <div class="denied-icon"><i class="fas fa-ban"></i></div>
                    <h2 class="denied-title">Access Denied</h2>
                    <p class="denied-message">You don't have administrator privileges to access Reports & Analytics.</p>
                    <a href="Default.aspx" class="btn btn-primary"><i class="fas fa-arrow-left"></i> Back to Dashboard</a>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlReportManagement" runat="server" Visible="false">

                <!-- FILTERS -->
                <div class="filters-section">
                    <div class="filters-header"><i class="fas fa-filter"></i><h3>Report Filters</h3></div>
                    <div class="filters-grid">
                        <div class="filter-group date-range-group">
                            <label class="filter-label">Date Range</label>
                            <div class="date-range-inputs">
                                <asp:TextBox ID="txtStartDate" runat="server" TextMode="Date" CssClass="filter-control" />
                                <asp:TextBox ID="txtEndDate" runat="server" TextMode="Date" CssClass="filter-control" />
                            </div>
                        </div>
                        <div class="filter-group">
                            <label class="filter-label">Status</label>
                            <asp:DropDownList ID="ddlStatus" runat="server" CssClass="filter-control" />
                        </div>
                        <div class="filter-group">
                            <label class="filter-label">Hardware Type</label>
                            <asp:DropDownList ID="ddlHardwareType" runat="server" CssClass="filter-control" />
                        </div>
                        <div class="filter-group">
                            <label class="filter-label">IT Staff</label>
                            <asp:DropDownList ID="ddlITStaff" runat="server" CssClass="filter-control" />
                        </div>
                        <div class="filter-group">
                            <label class="filter-label">Department</label>
                            <asp:DropDownList ID="ddlDepartment" runat="server" CssClass="filter-control" />
                        </div>
                        <div class="filter-group">
                            <label class="filter-label">Model</label>
                            <asp:DropDownList ID="ddlModel" runat="server" CssClass="filter-control" />
                        </div>
                    </div>
                    <div class="filter-actions" style="margin-top:24px;display:flex;gap:12px;flex-wrap:wrap;">
                        <asp:Button ID="btnApplyFilters" runat="server" Text="Apply Filters" CssClass="btn btn-primary" OnClick="btnApplyFilters_Click" />
                        <asp:Button ID="btnClearFilters" runat="server" Text="Clear Filters" CssClass="btn btn-secondary" OnClick="btnClearFilters_Click" />
                        <asp:Button ID="btnExport" runat="server" Text="Export to Excel" CssClass="btn btn-export" OnClick="btnExport_Click" />
                    </div>
                </div>

                <!-- KPI CARDS -->
                <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:24px;margin-bottom:36px;">
                    <div class="kpi-card">
                        <div class="kpi-header">
                            <div style="background:linear-gradient(135deg,#4361ee,#7209b7);color:#fff;width:48px;height:48px;border-radius:12px;display:flex;align-items:center;justify-content:center;font-size:1.3rem;"><i class="fas fa-file-contract"></i></div>
                        </div>
                        <div class="kpi-title" style="margin-top:16px;font-size:0.85rem;color:var(--text-secondary);text-transform:uppercase;font-weight:700;">Total Acknowledgement Receipts</div>
                        <div class="kpi-value" style="font-size:2rem;font-weight:800;"><asp:Literal ID="litTotalAgreements" runat="server" Text="0" /></div>
                    </div>
                    <div class="kpi-card">
                        <div class="kpi-header">
                            <div style="background:linear-gradient(135deg,#10b981,#059669);color:#fff;width:48px;height:48px;border-radius:12px;display:flex;align-items:center;justify-content:center;font-size:1.3rem;"><i class="fas fa-check-circle"></i></div>
                        </div>
                        <div class="kpi-title" style="margin-top:16px;font-size:0.85rem;color:var(--text-secondary);text-transform:uppercase;font-weight:700;">Completed / Agreed</div>
                        <div class="kpi-value" style="font-size:2rem;font-weight:800;"><asp:Literal ID="litCompletedAgreements" runat="server" Text="0" /></div>
                    </div>
                    <div class="kpi-card">
                        <div class="kpi-header">
                            <div style="background:linear-gradient(135deg,#f59e0b,#d97706);color:#fff;width:48px;height:48px;border-radius:12px;display:flex;align-items:center;justify-content:center;font-size:1.3rem;"><i class="fas fa-clock"></i></div>
                        </div>
                        <div class="kpi-title" style="margin-top:16px;font-size:0.85rem;color:var(--text-secondary);text-transform:uppercase;font-weight:700;">Pending</div>
                        <div class="kpi-value" style="font-size:2rem;font-weight:800;"><asp:Literal ID="litPendingAgreements" runat="server" Text="0" /></div>
                    </div>
                    <div class="kpi-card">
                        <div class="kpi-header">
                            <div style="background:linear-gradient(135deg,#94a3b8,#64748b);color:#fff;width:48px;height:48px;border-radius:12px;display:flex;align-items:center;justify-content:center;font-size:1.3rem;"><i class="fas fa-edit"></i></div>
                        </div>
                        <div class="kpi-title" style="margin-top:16px;font-size:0.85rem;color:var(--text-secondary);text-transform:uppercase;font-weight:700;">Draft</div>
                        <div class="kpi-value" style="font-size:2rem;font-weight:800;"><asp:Literal ID="litDraftAgreements" runat="server" Text="0" /></div>
                    </div>
                    <div class="kpi-card">
                        <div class="kpi-header">
                            <div style="background:linear-gradient(135deg,#ef4444,#dc2626);color:#fff;width:48px;height:48px;border-radius:12px;display:flex;align-items:center;justify-content:center;font-size:1.3rem;"><i class="fas fa-laptop"></i></div>
                        </div>
                        <div class="kpi-title" style="margin-top:16px;font-size:0.85rem;color:var(--text-secondary);text-transform:uppercase;font-weight:700;">Laptops</div>
                        <div class="kpi-value" style="font-size:2rem;font-weight:800;"><asp:Literal ID="litLaptopCount" runat="server" Text="0" /></div>
                    </div>
                    <div class="kpi-card">
                        <div class="kpi-header">
                            <div style="background:linear-gradient(135deg,#8b5cf6,#6d28d9);color:#fff;width:48px;height:48px;border-radius:12px;display:flex;align-items:center;justify-content:center;font-size:1.3rem;"><i class="fas fa-desktop"></i></div>
                        </div>
                        <div class="kpi-title" style="margin-top:16px;font-size:0.85rem;color:var(--text-secondary);text-transform:uppercase;font-weight:700;">Desktops</div>
                        <div class="kpi-value" style="font-size:2rem;font-weight:800;"><asp:Literal ID="litDesktopCount" runat="server" Text="0" /></div>
                    </div>
                </div>

                <!-- CHARTS -->
                <div class="charts-grid">
                    <div class="chart-card">
                        <div class="chart-header"><div class="chart-title">Acknowledgement Receipts by Status</div><div class="chart-period">Filtered period</div></div>
                        <div class="chart-container"><canvas id="statusChart"></canvas></div>
                    </div>
                    <div class="chart-card">
                        <div class="chart-header"><div class="chart-title">Hardware Type Distribution</div><div class="chart-period">Filtered period</div></div>
                        <div class="chart-container"><canvas id="typeChart"></canvas></div>
                    </div>
                    <div class="chart-card">
                        <div class="chart-header"><div class="chart-title">Monthly Trend</div><div class="chart-period">Last 6 months</div></div>
                        <div class="chart-container"><canvas id="trendChart"></canvas></div>
                    </div>
                    <div class="chart-card">
                        <div class="chart-header"><div class="chart-title">Acknowledgement Receipts by Department</div><div class="chart-period">Filtered period</div></div>
                        <div class="chart-container"><canvas id="deptChart"></canvas></div>
                    </div>
                    <div class="chart-card">
                        <div class="chart-header"><div class="chart-title">IT Staff Workload</div><div class="chart-period">Filtered period</div></div>
                        <div class="chart-container"><canvas id="itStaffChart"></canvas></div>
                    </div>
                </div>

                <!-- Hidden chart data literals -->
                <asp:Literal ID="litStatusChartData" runat="server" Visible="false" />
                <asp:Literal ID="litTypeChartData" runat="server" Visible="false" />
                <asp:Literal ID="litTrendChartData" runat="server" Visible="false" />
                <asp:Literal ID="litDeptChartData" runat="server" Visible="false" />
                <asp:Literal ID="litITStaffChartData" runat="server" Visible="false" />

                <!-- DATA TABLE -->
                <div class="tables-section">
                    <div class="table-card" style="grid-column:1/-1;">
                        <div class="table-title"><i class="fas fa-list"></i> Acknowledgement Receipts (<asp:Literal ID="litRecordCount" runat="server" Text="0" /> records)</div>
                        <div class="table-responsive">
                            <asp:GridView ID="gvRecentAgreements" runat="server" AutoGenerateColumns="false"
                                CssClass="data-table" AllowPaging="true" PageSize="10"
                                OnPageIndexChanging="gvRecentAgreements_PageIndexChanging">
                                <Columns>
                                    <asp:BoundField DataField="agreement_number" HeaderText="Acknowledgement Receipt" />
                                    <asp:BoundField DataField="employee_name" HeaderText="Employee" />
                                    <asp:BoundField DataField="employee_department" HeaderText="Department" />
                                    <asp:BoundField DataField="model" HeaderText="Model" />
                                    <asp:BoundField DataField="hardware_type" HeaderText="Type" />
                                    <asp:BoundField DataField="serial_number" HeaderText="Serial No" />
                                    <asp:BoundField DataField="asset_number" HeaderText="Asset No" />
                                    <asp:BoundField DataField="issue_date" HeaderText="Issue Date" DataFormatString="{0:dd/MM/yyyy}" />
                                    <asp:BoundField DataField="it_staff_win_id" HeaderText="IT Staff" />
                                    <asp:TemplateField HeaderText="Status">
                                        <ItemTemplate>
                                            <span class='status-badge <%# "status-" + Eval("agreement_status").ToString().ToLower() %>'><%# Eval("agreement_status") %></span>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                                <EmptyDataTemplate>
                                    <div style="text-align:center;padding:40px;color:var(--text-secondary);">
                                        <i class="fas fa-inbox" style="font-size:3rem;opacity:0.3;display:block;margin-bottom:16px;"></i>
                                        <h3 style="margin:0 0 8px;">No acknowledgement receipts found</h3><p>Try adjusting your filters</p>
                                    </div>
                                </EmptyDataTemplate>
                            </asp:GridView>
                        </div>
                    </div>
                </div>

                <!-- ACCESSORIES SUMMARY -->
                <div class="tables-section" style="margin-top:36px;">
                    <div class="table-card" style="grid-column:1/-1;">
                        <div class="table-title"><i class="fas fa-briefcase"></i> Laptop Accessories Summary</div>
                        <div class="table-responsive">
                            <asp:GridView ID="gvAccessories" runat="server" AutoGenerateColumns="false" CssClass="data-table">
                                <Columns>
                                    <asp:BoundField DataField="item" HeaderText="Accessory" />
                                    <asp:BoundField DataField="item_count" HeaderText="Count" />
                                    <asp:TemplateField HeaderText="Usage %">
                                        <ItemTemplate>
                                            <div style="display:flex;align-items:center;gap:12px;">
                                                <div style="flex:1;background:#e5e7eb;border-radius:8px;height:10px;overflow:hidden;">
                                                    <div style="width:<%# Eval("percentage") %>%;height:100%;background:linear-gradient(90deg,#4361ee,#7209b7);border-radius:8px;"></div>
                                                </div>
                                                <span style="font-weight:700;min-width:40px;"><%# Eval("percentage") %>%</span>
                                            </div>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                                <EmptyDataTemplate>
                                    <div style="text-align:center;padding:20px;color:var(--text-secondary);">No laptop accessory data available</div>
                                </EmptyDataTemplate>
                            </asp:GridView>
                        </div>
                    </div>
                </div>

                <!-- INSIGHTS -->
                <div class="insights-section">
                    <div class="insights-header">
                        <div class="insights-icon"><i class="fas fa-lightbulb"></i></div>
                        <div class="insights-title"><h3>Key Insights</h3><p>Data-driven intelligence from your acknowledgement receipts</p></div>
                    </div>
                    <div class="insights-grid">
                        <div class="insight-item">
                            <div class="insight-header"><i class="fas fa-laptop"></i><h4>Laptop Share</h4></div>
                            <p class="insight-text"><span class="insight-metric">Laptops</span> account for <span class="insight-metric"><asp:Literal ID="litLaptopPercentage" runat="server" Text="0" />%</span> of all hardware acknowledgement receipts.</p>
                        </div>
                        <div class="insight-item">
                            <div class="insight-header"><i class="fas fa-user-tie"></i><h4>Top IT Staff</h4></div>
                            <p class="insight-text"><span class="insight-metric"><asp:Literal ID="litTopITStaff" runat="server" Text="N/A" /></span> has processed the most acknowledgement receipts in this period.</p>
                        </div>
                        <div class="insight-item">
                            <div class="insight-header"><i class="fas fa-clock"></i><h4>Avg. Processing Time</h4></div>
                            <p class="insight-text">Average time from submission to employee signature is <span class="insight-metric"><asp:Literal ID="litAvgProcessingTime" runat="server" Text="0" /> days</span>.</p>
                        </div>
                        <div class="insight-item">
                            <div class="insight-header"><i class="fas fa-star"></i><h4>Top Model</h4></div>
                            <p class="insight-text"><span class="insight-metric"><asp:Literal ID="litTopModel" runat="server" Text="N/A" /></span> is the most frequently issued model.</p>
                        </div>
                        <div class="insight-item">
                            <div class="insight-header"><i class="fas fa-building"></i><h4>Top Department</h4></div>
                            <p class="insight-text"><span class="insight-metric"><asp:Literal ID="litTopDepartment" runat="server" Text="N/A" /></span> has the most hardware acknowledgement receipts.</p>
                        </div>
                        <div class="insight-item">
                            <div class="insight-header"><i class="fas fa-microchip"></i><h4>Model Variety</h4></div>
                            <p class="insight-text"><span class="insight-metric"><asp:Literal ID="litUniqueModels" runat="server" Text="0" /></span> unique hardware models have been issued.</p>
                        </div>
                    </div>
                </div>

            </asp:Panel>

            <div class="footer">
                <p>Reports & Analytics &copy; <%= DateTime.Now.Year %> | Generated on: <%= DateTime.Now.ToString("MMMM dd, yyyy HH:mm:ss") %></p>
                <p style="margin-top:8px;font-size:0.8rem;color:rgba(255,255,255,0.8);">Data Source: Laptop & Desktop Acknowledgement Receipts Database</p>
            </div>
        </main>
    </form>

    <script>
        // ── Theme init (before paint) ──────────────────────────────────────────
        (function(){
            document.documentElement.setAttribute('data-theme',
                localStorage.getItem('portalTheme') || 'light');
        })();

        // ── Chart colour palettes ──────────────────────────────────────────────
        var COLORS = [
            'rgba(67,97,238,0.8)','rgba(247,37,133,0.8)','rgba(16,185,129,0.8)',
            'rgba(245,158,11,0.8)','rgba(139,92,246,0.8)','rgba(239,68,68,0.8)',
            'rgba(6,182,212,0.8)','rgba(234,88,12,0.8)','rgba(99,102,241,0.8)',
            'rgba(168,85,247,0.8)','rgba(20,184,166,0.8)','rgba(251,146,60,0.8)'
        ];
        var BORDERS = COLORS.map(function(c){ return c.replace('0.8)','1)'); });

        var STATUS_COLORS = {
            'Draft':    'rgba(148,163,184,0.8)',
            'Pending':  'rgba(245,158,11,0.8)',
            'Agreed':   'rgba(16,185,129,0.8)',
            'Completed':'rgba(67,97,238,0.8)',
            'Rejected': 'rgba(239,68,68,0.8)'
        };
        var STATUS_BORDERS = {};
        for (var k in STATUS_COLORS) STATUS_BORDERS[k] = STATUS_COLORS[k].replace('0.8)','1)');

        function getStatusColors(labels) {
            var bg = [], bd = [];
            for (var i = 0; i < labels.length; i++) {
                bg.push(STATUS_COLORS[labels[i]] || COLORS[i % COLORS.length]);
                bd.push(STATUS_BORDERS[labels[i]] || BORDERS[i % BORDERS.length]);
            }
            return { bg: bg, bd: bd };
        }

        // ── Chart.js dark-mode defaults ────────────────────────────────────────
        function applyChartTheme() {
            var dark = document.documentElement.getAttribute('data-theme') === 'dark';
            var textColor  = dark ? '#94a3b8' : '#4a5568';
            var gridColor  = dark ? 'rgba(255,255,255,0.07)' : 'rgba(0,0,0,0.07)';
            var bg         = dark ? '#1a2234'  : '#ffffff';

            Chart.defaults.color          = textColor;
            Chart.defaults.borderColor    = gridColor;
            Chart.defaults.backgroundColor = bg;

            if (Chart.defaults.plugins && Chart.defaults.plugins.legend) {
                Chart.defaults.plugins.legend.labels = Chart.defaults.plugins.legend.labels || {};
                Chart.defaults.plugins.legend.labels.color = textColor;
            }
            if (Chart.defaults.scales) {
                ['x','y'].forEach(function(axis){
                    Chart.defaults.scales[axis] = Chart.defaults.scales[axis] || {};
                    Chart.defaults.scales[axis].ticks = Chart.defaults.scales[axis].ticks || {};
                    Chart.defaults.scales[axis].ticks.color = textColor;
                    Chart.defaults.scales[axis].grid  = Chart.defaults.scales[axis].grid  || {};
                    Chart.defaults.scales[axis].grid.color  = gridColor;
                });
            }
        }

        document.addEventListener('DOMContentLoaded', function () {

            // ── Theme toggle ───────────────────────────────────────────────────
            function applyTheme(theme) {
                document.documentElement.setAttribute('data-theme', theme);
                localStorage.setItem('portalTheme', theme);
                document.dispatchEvent(new Event('themeChanged'));
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
                    // applyTheme dispatches 'themeChanged' which calls initializeCharts()
                    // initializeCharts() destroys existing instances via _chartReg before recreating
                });
            }

            // ── Active nav ─────────────────────────────────────────────────────

            // ── Mobile sidebar ─────────────────────────────────────────────────
            var sidebarToggle = document.createElement('button');
            sidebarToggle.innerHTML = '<i class="fas fa-bars"></i>';
            sidebarToggle.className = 'sidebar-toggle';
            document.body.appendChild(sidebarToggle);
            sidebarToggle.addEventListener('click', function(){
                document.querySelector('.sidebar').classList.toggle('mobile-open');
            });
            function handleResize(){
                var btn = document.querySelector('.sidebar-toggle');
                if (!btn) return;
                btn.style.display = window.innerWidth <= 768 ? 'flex' : 'none';
                if (window.innerWidth > 768) document.querySelector('.sidebar').classList.remove('mobile-open');
            }
            window.addEventListener('resize', handleResize);
            handleResize();

            // ── Support email ──────────────────────────────────────────────────

            // ── Parallax ───────────────────────────────────────────────────────
            window.addEventListener('scroll', function(){
                var s = window.pageYOffset;
                document.querySelectorAll('.floating-icon').forEach(function(ic,i){
                    ic.style.transform = 'translateY('+(-s*(0.5+i*0.1))+'px) rotate('+(s*0.1)+'deg)';
                });
            });

            // ── KPI number animation ───────────────────────────────────────────
            document.querySelectorAll('.kpi-value').forEach(function(el){
                var val = parseInt(el.textContent);
                if (!isNaN(val) && val > 0){
                    var cur = 0, inc = val / 75;
                    var t = setInterval(function(){
                        cur += inc;
                        if (cur >= val){ el.textContent = val.toLocaleString(); clearInterval(t); }
                        else el.textContent = Math.floor(cur).toLocaleString();
                    }, 16);
                }
            });

            // ── Init charts if admin panel is visible ──────────────────────────
            applyChartTheme();
            var adminPanel = document.getElementById('<%= pnlReportManagement.ClientID %>');
            if (adminPanel) { initializeCharts(); }  // style.display unreliable for server-rendered panels
        });

        // ── Chart initialisation ───────────────────────────────────────────────
        document.addEventListener('themeChanged', function() { initializeCharts(); });

        // Registry to track chart instances so they can be destroyed before re-creation
        var _chartReg = {};

        function initializeCharts() {
            // Destroy any existing Chart instances before re-creating (fixes "Canvas already in use" error)
            ['statusChart','typeChart','trendChart','deptChart','itStaffChart'].forEach(function(id) {
                if (_chartReg[id]) {
                    _chartReg[id].destroy();
                    delete _chartReg[id];
                }
            });

            var isDark    = document.documentElement.getAttribute('data-theme') === 'dark';
            var textColor = isDark ? '#94a3b8' : '#4a5568';
            var gridColor = isDark ? 'rgba(255,255,255,0.07)' : 'rgba(0,0,0,0.07)';

            var scaleOpts = {
                ticks: { color: textColor, stepSize: 1 },
                grid:  { color: gridColor }
            };

            var statusData  = <%= litStatusChartData.Text  ?? "{labels:[],values:[]}" %>;
            var typeData    = <%= litTypeChartData.Text    ?? "{labels:[],values:[]}" %>;
            var trendData   = <%= litTrendChartData.Text   ?? "{labels:[],values:[]}" %>;
            var deptData    = <%= litDeptChartData.Text    ?? "{labels:[],values:[]}" %>;
            var itStaffData = <%= litITStaffChartData.Text ?? "{labels:[],values:[]}" %>;

            function showEmpty(canvasId) {
                var canvas = document.getElementById(canvasId);
                if (!canvas) return;
                var parent = canvas.parentElement;
                parent.innerHTML = '<div style="display:flex;flex-direction:column;align-items:center;justify-content:center;height:100%;gap:12px;opacity:0.4;">'
                    + '<i class="fas fa-chart-bar" style="font-size:2.5rem;color:var(--primary);"></i>'
                    + '<span style="font-size:0.9rem;color:var(--text-secondary);">No data available</span></div>';
            }

            // 1. Status — Doughnut
            var ctx1 = document.getElementById('statusChart');
            if (ctx1 && statusData.labels && statusData.labels.length > 0) {
                var sc = getStatusColors(statusData.labels);
                _chartReg['statusChart'] = new Chart(ctx1.getContext('2d'), {
                    type: 'doughnut',
                    data: { labels: statusData.labels, datasets: [{ data: statusData.values, backgroundColor: sc.bg, borderColor: sc.bd, borderWidth: 2 }] },
                    options: { responsive: true, maintainAspectRatio: false,
                        plugins: { legend: { position: 'right', labels: { padding: 16, usePointStyle: true, color: textColor } } } }
                });
            } else { showEmpty('statusChart'); }

            // 2. Hardware Type — Bar
            var ctx2 = document.getElementById('typeChart');
            if (ctx2 && typeData.labels && typeData.labels.length > 0) {
                _chartReg['typeChart'] = new Chart(ctx2.getContext('2d'), {
                    type: 'bar',
                    data: { labels: typeData.labels, datasets: [{ label: 'Count', data: typeData.values, backgroundColor: COLORS.slice(0,typeData.labels.length), borderColor: BORDERS.slice(0,typeData.labels.length), borderWidth: 1, borderRadius: 6 }] },
                    options: { responsive: true, maintainAspectRatio: false,
                        scales: { x: scaleOpts, y: Object.assign({beginAtZero:true}, scaleOpts) },
                        plugins: { legend: { display: false } } }
                });
            } else { showEmpty('typeChart'); }

            // 3. Monthly Trend — Line
            var ctx3 = document.getElementById('trendChart');
            if (ctx3 && trendData.labels && trendData.labels.length > 0) {
                _chartReg['trendChart'] = new Chart(ctx3.getContext('2d'), {
                    type: 'line',
                    data: { labels: trendData.labels, datasets: [{ label: 'Acknowledgement Receipts', data: trendData.values,
                        borderColor: 'rgba(67,97,238,1)', backgroundColor: 'rgba(67,97,238,0.1)',
                        borderWidth: 3, fill: true, tension: 0.4,
                        pointBackgroundColor: 'rgba(67,97,238,1)', pointRadius: 5 }] },
                    options: { responsive: true, maintainAspectRatio: false,
                        scales: { x: scaleOpts, y: Object.assign({beginAtZero:true}, scaleOpts) },
                        plugins: { legend: { display: false } } }
                });
            } else { showEmpty('trendChart'); }

            // 4. Department — Horizontal Bar
            var ctx4 = document.getElementById('deptChart');
            if (ctx4 && deptData.labels && deptData.labels.length > 0) {
                _chartReg['deptChart'] = new Chart(ctx4.getContext('2d'), {
                    type: 'bar',
                    data: { labels: deptData.labels, datasets: [{ label: 'Count', data: deptData.values, backgroundColor: COLORS.slice(0,deptData.labels.length), borderColor: BORDERS.slice(0,deptData.labels.length), borderWidth: 1, borderRadius: 6 }] },
                    options: { responsive: true, maintainAspectRatio: false, indexAxis: 'y',
                        scales: { x: Object.assign({beginAtZero:true}, scaleOpts), y: scaleOpts },
                        plugins: { legend: { display: false } } }
                });
            } else { showEmpty('deptChart'); }

            // 5. IT Staff Workload — Bar
            var ctx5 = document.getElementById('itStaffChart');
            if (ctx5 && itStaffData.labels && itStaffData.labels.length > 0) {
                _chartReg['itStaffChart'] = new Chart(ctx5.getContext('2d'), {
                    type: 'bar',
                    data: { labels: itStaffData.labels, datasets: [{ label: 'Acknowledgement Receipts', data: itStaffData.values,
                        backgroundColor: 'rgba(139,92,246,0.7)', borderColor: 'rgba(139,92,246,1)',
                        borderWidth: 1, borderRadius: 6 }] },
                    options: { responsive: true, maintainAspectRatio: false,
                        scales: { x: scaleOpts, y: Object.assign({beginAtZero:true}, scaleOpts) },
                        plugins: { legend: { display: false, labels: { color: textColor } } } }
                });
            } else { showEmpty('itStaffChart'); }
        }
    </script>

    <!-- Theme toggle — floating button, always visible at any zoom -->
    <button id="themeToggleBtn" class="theme-toggle" type="button">
        <i class="fas fa-moon"></i><span>Dark Mode</span>
    </button>

</body>
</html>
