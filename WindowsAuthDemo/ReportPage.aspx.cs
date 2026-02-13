using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WindowsAuthDemo
{
    public partial class ReportPage : System.Web.UI.Page
    {
        private string connectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            connectionString = System.Configuration.ConfigurationManager
                .ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            if (!IsPostBack)
            {
                string windowsUser = User.Identity.Name;
                lblUser.Text = windowsUser;
                lblUserSidebar.Text = windowsUser;

                bool isAdmin = CheckUserIsAdmin(windowsUser);

                if (isAdmin)
                {
                    pnlReportManagement.Visible = true;
                    pnlAccessDenied.Visible = false;
                    lblStatus.Text = "Administrator Access";
                    lblUserRoleSidebar.Text = "Administrator";

                    txtEndDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
                    txtStartDate.Text = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");

                    LoadITStaffDropdown();
                    LoadDepartmentDropdown();
                    LoadModelDropdown();
                    LoadStatusDropdown();
                    LoadHardwareTypeDropdown();
                    LoadReportData();
                }
                else
                {
                    pnlReportManagement.Visible = false;
                    pnlAccessDenied.Visible = true;
                    lblStatus.Text = "Standard User Access";
                    lblUserRoleSidebar.Text = "Normal User";
                }
            }
        }

        private bool CheckUserIsAdmin(string windowsUser)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT admin FROM hardware_users WHERE win_id = @win_id", conn))
                    {
                        cmd.Parameters.AddWithValue("@win_id", windowsUser);
                        object result = cmd.ExecuteScalar();
                        return result != null && Convert.ToInt32(result) == 1;
                    }
                }
                catch { return false; }
            }
        }

        // ================================================================
        // FILTER DROPDOWNS - All from database
        // ================================================================

        private void LoadITStaffDropdown()
        {
            LoadDropdown(ddlITStaff, "All IT Staff",
                "SELECT DISTINCT it_staff_win_id FROM hardware_agreements WHERE it_staff_win_id IS NOT NULL AND it_staff_win_id != '' ORDER BY it_staff_win_id",
                "it_staff_win_id");
        }

        private void LoadDepartmentDropdown()
        {
            LoadDropdown(ddlDepartment, "All Departments",
                "SELECT DISTINCT employee_department FROM hardware_agreements WHERE employee_department IS NOT NULL AND employee_department != '' ORDER BY employee_department",
                "employee_department");
        }

        private void LoadModelDropdown()
        {
            LoadDropdown(ddlModel, "All Models",
                "SELECT DISTINCT model FROM hardware_model ORDER BY model", "model");
        }

        private void LoadStatusDropdown()
        {
            LoadDropdown(ddlStatus, "All Status",
                "SELECT DISTINCT agreement_status FROM hardware_agreements ORDER BY agreement_status",
                "agreement_status");
        }

        private void LoadHardwareTypeDropdown()
        {
            LoadDropdown(ddlHardwareType, "All Types",
                "SELECT DISTINCT type FROM hardware_model ORDER BY type", "type");
        }

        private void LoadDropdown(DropDownList ddl, string allText, string query, string column)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    ddl.Items.Clear();
                    ddl.Items.Add(new ListItem(allText, ""));
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string val = r[column].ToString();
                            ddl.Items.Add(new ListItem(val, val));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("Dropdown error: " + ex.Message);
                }
            }
        }

        // ================================================================
        // FILTER EVENTS
        // ================================================================

        protected void btnApplyFilters_Click(object sender, EventArgs e)
        {
            LoadReportData();
        }

        protected void btnClearFilters_Click(object sender, EventArgs e)
        {
            txtStartDate.Text = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
            txtEndDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
            ddlStatus.SelectedValue = "";
            ddlHardwareType.SelectedValue = "";
            ddlITStaff.SelectedValue = "";
            ddlDepartment.SelectedValue = "";
            ddlModel.SelectedValue = "";
            LoadReportData();
        }

        // ================================================================
        // MAIN LOAD
        // ================================================================

        private void LoadReportData()
        {
            LoadKPIData();
            LoadChartData();
            LoadRecentAgreements();
            LoadAccessoriesSummary();
            LoadInsights();
        }

        private void AddDateParams(SqlCommand cmd)
        {
            DateTime d;
            cmd.Parameters.AddWithValue("@startDate",
                !string.IsNullOrEmpty(txtStartDate.Text) && DateTime.TryParse(txtStartDate.Text, out d)
                    ? (object)d : DBNull.Value);
            cmd.Parameters.AddWithValue("@endDate",
                !string.IsNullOrEmpty(txtEndDate.Text) && DateTime.TryParse(txtEndDate.Text, out d)
                    ? (object)d : DBNull.Value);
        }

        private void AddAllFilterParams(SqlCommand cmd)
        {
            AddDateParams(cmd);
            cmd.Parameters.AddWithValue("@status", ddlStatus.SelectedValue ?? "");
            cmd.Parameters.AddWithValue("@hardwareType", ddlHardwareType.SelectedValue ?? "");
            cmd.Parameters.AddWithValue("@itStaff", ddlITStaff.SelectedValue ?? "");
            cmd.Parameters.AddWithValue("@department", ddlDepartment.SelectedValue ?? "");
            cmd.Parameters.AddWithValue("@model", ddlModel.SelectedValue ?? "");
        }

        // ================================================================
        // KPI DATA
        // ================================================================

        private void LoadKPIData()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    string q = @"
                        SELECT 
                            COUNT(*) AS Total,
                            SUM(CASE WHEN agreement_status IN ('Completed','Agreed') THEN 1 ELSE 0 END) AS Completed,
                            SUM(CASE WHEN agreement_status = 'Pending' THEN 1 ELSE 0 END) AS Pending,
                            SUM(CASE WHEN agreement_status = 'Draft' THEN 1 ELSE 0 END) AS Draft
                        FROM hardware_agreements
                        WHERE (@startDate IS NULL OR issue_date >= @startDate)
                          AND (@endDate IS NULL OR issue_date <= @endDate)";

                    using (SqlCommand cmd = new SqlCommand(q, conn))
                    {
                        AddDateParams(cmd);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                litTotalAgreements.Text = r["Total"].ToString();
                                litCompletedAgreements.Text = r["Completed"].ToString();
                                litPendingAgreements.Text = r["Pending"].ToString();
                                litDraftAgreements.Text = r["Draft"].ToString();
                            }
                        }
                    }

                    // Laptop count
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM hardware_agreements ha
                        INNER JOIN hardware_model hm ON ha.model_id = hm.id
                        WHERE hm.type = 'Laptop'
                        AND (@startDate IS NULL OR ha.issue_date >= @startDate)
                        AND (@endDate IS NULL OR ha.issue_date <= @endDate)", conn))
                    {
                        AddDateParams(cmd);
                        litLaptopCount.Text = cmd.ExecuteScalar().ToString();
                    }

                    // Desktop count
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM hardware_agreements ha
                        INNER JOIN hardware_model hm ON ha.model_id = hm.id
                        WHERE hm.type = 'Desktop'
                        AND (@startDate IS NULL OR ha.issue_date >= @startDate)
                        AND (@endDate IS NULL OR ha.issue_date <= @endDate)", conn))
                    {
                        AddDateParams(cmd);
                        litDesktopCount.Text = cmd.ExecuteScalar().ToString();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("KPI error: " + ex.Message);
                    litTotalAgreements.Text = "0"; litCompletedAgreements.Text = "0";
                    litPendingAgreements.Text = "0"; litDraftAgreements.Text = "0";
                    litLaptopCount.Text = "0"; litDesktopCount.Text = "0";
                }
            }
        }

        // ================================================================
        // CHART DATA - Uses sp_GetChartData, output as JSON for Chart.js
        // ================================================================

        private void LoadChartData()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    litStatusChartData.Text = GetChartJson(conn, "Status");
                    litTypeChartData.Text = GetChartJson(conn, "Type");
                    litTrendChartData.Text = GetChartJson(conn, "Trend");
                    litDeptChartData.Text = GetChartJson(conn, "Department");
                    litITStaffChartData.Text = GetChartJson(conn, "ITStaff");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("Chart error: " + ex.Message);
                    string empty = "{labels:[],values:[]}";
                    litStatusChartData.Text = empty;
                    litTypeChartData.Text = empty;
                    litTrendChartData.Text = empty;
                    litDeptChartData.Text = empty;
                    litITStaffChartData.Text = empty;
                }
            }
        }

        private string GetChartJson(SqlConnection conn, string chartType)
        {
            StringBuilder labels = new StringBuilder();
            StringBuilder values = new StringBuilder();

            using (SqlCommand cmd = new SqlCommand("sp_GetChartData", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@chartType", chartType);

                DateTime d;
                cmd.Parameters.AddWithValue("@startDate",
                    !string.IsNullOrEmpty(txtStartDate.Text) && DateTime.TryParse(txtStartDate.Text, out d)
                        ? (object)d : DBNull.Value);
                cmd.Parameters.AddWithValue("@endDate",
                    !string.IsNullOrEmpty(txtEndDate.Text) && DateTime.TryParse(txtEndDate.Text, out d)
                        ? (object)d : DBNull.Value);

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    bool first = true;
                    while (r.Read())
                    {
                        if (!first) { labels.Append(","); values.Append(","); }
                        string lbl = r["label"] != DBNull.Value ? r["label"].ToString() : "Unknown";
                        labels.Append("\"" + HttpUtility.JavaScriptStringEncode(lbl) + "\"");
                        values.Append(r["value"].ToString());
                        first = false;
                    }
                }
            }
            return "{labels:[" + labels + "],values:[" + values + "]}";
        }

        // ================================================================
        // AGREEMENTS TABLE
        // ================================================================

        private void LoadRecentAgreements()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string q = @"
                        SELECT ha.agreement_number, ISNULL(ha.employee_name,'N/A') AS employee_name,
                               ISNULL(ha.employee_department,'N/A') AS employee_department,
                               hm.model, hm.type AS hardware_type, ha.serial_number, ha.asset_number,
                               ha.issue_date, ha.agreement_status, ha.it_staff_win_id
                        FROM hardware_agreements ha
                        INNER JOIN hardware_model hm ON ha.model_id = hm.id
                        WHERE (@startDate IS NULL OR ha.issue_date >= @startDate)
                          AND (@endDate IS NULL OR ha.issue_date <= @endDate)
                          AND (@status = '' OR ha.agreement_status = @status)
                          AND (@hardwareType = '' OR hm.type = @hardwareType)
                          AND (@itStaff = '' OR ha.it_staff_win_id = @itStaff)
                          AND (@department = '' OR ha.employee_department = @department)
                          AND (@model = '' OR hm.model = @model)
                        ORDER BY ha.created_date DESC";

                    using (SqlCommand cmd = new SqlCommand(q, conn))
                    {
                        AddAllFilterParams(cmd);
                        DataTable dt = new DataTable();
                        new SqlDataAdapter(cmd).Fill(dt);
                        gvRecentAgreements.DataSource = dt;
                        gvRecentAgreements.DataBind();
                        litRecordCount.Text = dt.Rows.Count.ToString();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("Table error: " + ex.Message);
                    litRecordCount.Text = "0";
                }
            }
        }

        // ================================================================
        // ACCESSORIES SUMMARY - Real data from database
        // ================================================================

        private void LoadAccessoriesSummary()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string q = @"
                        DECLARE @total FLOAT = (SELECT COUNT(*) FROM hardware_agreements ha
                            INNER JOIN hardware_model hm ON ha.model_id = hm.id WHERE hm.type = 'Laptop'
                            AND (@startDate IS NULL OR ha.issue_date >= @startDate)
                            AND (@endDate IS NULL OR ha.issue_date <= @endDate));
                        SELECT item, item_count, 
                               CASE WHEN @total > 0 THEN CAST(ROUND(item_count*100.0/@total,0) AS INT) ELSE 0 END AS percentage
                        FROM (
                            SELECT 'Carry Bag' AS item, SUM(CAST(ISNULL(has_carry_bag,0) AS INT)) AS item_count
                            FROM hardware_agreements ha INNER JOIN hardware_model hm ON ha.model_id=hm.id
                            WHERE hm.type='Laptop' AND (@startDate IS NULL OR ha.issue_date>=@startDate) AND (@endDate IS NULL OR ha.issue_date<=@endDate)
                            UNION ALL
                            SELECT 'Power Adapter', SUM(CAST(ISNULL(has_power_adapter,0) AS INT))
                            FROM hardware_agreements ha INNER JOIN hardware_model hm ON ha.model_id=hm.id
                            WHERE hm.type='Laptop' AND (@startDate IS NULL OR ha.issue_date>=@startDate) AND (@endDate IS NULL OR ha.issue_date<=@endDate)
                            UNION ALL
                            SELECT 'Mouse', SUM(CAST(ISNULL(has_mouse,0) AS INT))
                            FROM hardware_agreements ha INNER JOIN hardware_model hm ON ha.model_id=hm.id
                            WHERE hm.type='Laptop' AND (@startDate IS NULL OR ha.issue_date>=@startDate) AND (@endDate IS NULL OR ha.issue_date<=@endDate)
                            UNION ALL
                            SELECT 'VGA Converter', SUM(CAST(ISNULL(has_vga_converter,0) AS INT))
                            FROM hardware_agreements ha INNER JOIN hardware_model hm ON ha.model_id=hm.id
                            WHERE hm.type='Laptop' AND (@startDate IS NULL OR ha.issue_date>=@startDate) AND (@endDate IS NULL OR ha.issue_date<=@endDate)
                        ) x ORDER BY item_count DESC";

                    using (SqlCommand cmd = new SqlCommand(q, conn))
                    {
                        AddDateParams(cmd);
                        DataTable dt = new DataTable();
                        new SqlDataAdapter(cmd).Fill(dt);
                        gvAccessories.DataSource = dt;
                        gvAccessories.DataBind();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("Accessories error: " + ex.Message);
                }
            }
        }

        // ================================================================
        // INSIGHTS - All from database
        // ================================================================

        private void LoadInsights()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    litLaptopPercentage.Text = ScalarStr(conn, @"
                        SELECT ISNULL(CAST(COUNT(CASE WHEN hm.type='Laptop' THEN 1 END)*100.0/NULLIF(COUNT(*),0) AS INT),0)
                        FROM hardware_agreements ha INNER JOIN hardware_model hm ON ha.model_id=hm.id
                        WHERE (@startDate IS NULL OR ha.issue_date>=@startDate) AND (@endDate IS NULL OR ha.issue_date<=@endDate)", "0");

                    litTopITStaff.Text = ScalarStr(conn, @"
                        SELECT TOP 1 it_staff_win_id FROM hardware_agreements
                        WHERE it_staff_win_id IS NOT NULL AND it_staff_win_id != ''
                        AND (@startDate IS NULL OR issue_date>=@startDate) AND (@endDate IS NULL OR issue_date<=@endDate)
                        GROUP BY it_staff_win_id ORDER BY COUNT(*) DESC", "N/A");

                    litAvgProcessingTime.Text = ScalarStr(conn, @"
                        SELECT ISNULL(CAST(AVG(CAST(DATEDIFF(DAY,submitted_date,employee_agreed_date) AS FLOAT)) AS DECIMAL(5,1)),0)
                        FROM hardware_agreements
                        WHERE submitted_date IS NOT NULL AND employee_agreed_date IS NOT NULL
                        AND (@startDate IS NULL OR issue_date>=@startDate) AND (@endDate IS NULL OR issue_date<=@endDate)", "0");

                    litTopModel.Text = ScalarStr(conn, @"
                        SELECT TOP 1 hm.model FROM hardware_agreements ha
                        INNER JOIN hardware_model hm ON ha.model_id=hm.id
                        WHERE (@startDate IS NULL OR ha.issue_date>=@startDate) AND (@endDate IS NULL OR ha.issue_date<=@endDate)
                        GROUP BY hm.model ORDER BY COUNT(*) DESC", "N/A");

                    litTopDepartment.Text = ScalarStr(conn, @"
                        SELECT TOP 1 ISNULL(employee_department,'Unknown') FROM hardware_agreements
                        WHERE employee_department IS NOT NULL AND employee_department != ''
                        AND (@startDate IS NULL OR issue_date>=@startDate) AND (@endDate IS NULL OR issue_date<=@endDate)
                        GROUP BY employee_department ORDER BY COUNT(*) DESC", "N/A");

                    litUniqueModels.Text = ScalarStr(conn, @"
                        SELECT COUNT(DISTINCT model_id) FROM hardware_agreements
                        WHERE (@startDate IS NULL OR issue_date>=@startDate) AND (@endDate IS NULL OR issue_date<=@endDate)", "0");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("Insights error: " + ex.Message);
                    litLaptopPercentage.Text = "0"; litTopITStaff.Text = "N/A";
                    litAvgProcessingTime.Text = "0"; litTopModel.Text = "N/A";
                    litTopDepartment.Text = "N/A"; litUniqueModels.Text = "0";
                }
            }
        }

        private string ScalarStr(SqlConnection conn, string query, string fallback)
        {
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                AddDateParams(cmd);
                object r = cmd.ExecuteScalar();
                return (r != null && r != DBNull.Value) ? r.ToString() : fallback;
            }
        }

        // ================================================================
        // GRIDVIEW PAGING
        // ================================================================

        protected void gvRecentAgreements_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvRecentAgreements.PageIndex = e.NewPageIndex;
            LoadRecentAgreements();
        }

        // ================================================================
        // EXPORT
        // ================================================================

        protected void btnExport_Click(object sender, EventArgs e) { ExportToExcel(); }

        private void ExportToExcel()
        {
            try
            {
                Response.Clear(); Response.Buffer = true;
                Response.AddHeader("content-disposition",
                    "attachment;filename=Hardware_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xls");
                Response.Charset = "";
                Response.ContentType = "application/vnd.ms-excel";

                DataTable dt = GetExportData();
                StringBuilder sb = new StringBuilder();
                sb.Append("<html><head><meta charset='utf-8'></head><body>");
                sb.Append("<table border='1' cellpadding='5' cellspacing='0' style='font-family:Arial;font-size:11px'><tr>");
                foreach (DataColumn c in dt.Columns)
                    sb.Append("<th style='background:#667eea;color:white;font-weight:bold;padding:8px'>" + c.ColumnName + "</th>");
                sb.Append("</tr>");
                foreach (DataRow row in dt.Rows)
                {
                    sb.Append("<tr>");
                    foreach (DataColumn c in dt.Columns)
                        sb.Append("<td style='padding:6px'>" + HttpUtility.HtmlEncode(row[c].ToString()) + "</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table></body></html>");
                Response.Output.Write(sb.ToString());
                Response.Flush(); Response.End();
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine("Export error: " + ex.Message); }
        }

        private DataTable GetExportData()
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string q = @"SELECT ha.agreement_number AS [Agreement No],
                    ISNULL(ha.employee_name,'N/A') AS [Employee],
                    ISNULL(ha.employee_department,'N/A') AS [Department],
                    hm.model AS [Model], hm.type AS [Type],
                    ha.serial_number AS [Serial No], ha.asset_number AS [Asset No],
                    CONVERT(VARCHAR,ha.issue_date,103) AS [Issue Date],
                    ha.agreement_status AS [Status], ha.it_staff_win_id AS [IT Staff],
                    ISNULL(ha.employee_email,'N/A') AS [Email],
                    CASE WHEN has_carry_bag=1 THEN 'Yes' ELSE 'No' END AS [Carry Bag],
                    CASE WHEN has_power_adapter=1 THEN 'Yes' ELSE 'No' END AS [Power Adapter],
                    CASE WHEN has_mouse=1 THEN 'Yes' ELSE 'No' END AS [Mouse],
                    ISNULL(mouse_type,'N/A') AS [Mouse Type],
                    CASE WHEN has_vga_converter=1 THEN 'Yes' ELSE 'No' END AS [VGA Converter]
                FROM hardware_agreements ha INNER JOIN hardware_model hm ON ha.model_id=hm.id
                WHERE (@startDate IS NULL OR ha.issue_date>=@startDate) AND (@endDate IS NULL OR ha.issue_date<=@endDate)
                  AND (@status='' OR ha.agreement_status=@status) AND (@hardwareType='' OR hm.type=@hardwareType)
                  AND (@itStaff='' OR ha.it_staff_win_id=@itStaff) AND (@department='' OR ha.employee_department=@department)
                  AND (@model='' OR hm.model=@model) ORDER BY ha.issue_date DESC";
                using (SqlCommand cmd = new SqlCommand(q, conn))
                {
                    AddAllFilterParams(cmd);
                    new SqlDataAdapter(cmd).Fill(dt);
                }
            }
            return dt;
        }
    }
}
