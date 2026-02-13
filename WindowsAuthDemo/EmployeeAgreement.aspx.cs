using System;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WindowsAuthDemo
{
    public partial class EmployeeAgreement : System.Web.UI.Page
    {
        private int agreementId = 0;
        private bool isTokenValid = false;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Check for token in query string
                string token = Request.QueryString["token"];

                if (string.IsNullOrEmpty(token))
                {
                    ShowError("Invalid or missing access token. Please use the link provided in your email.");
                    DisableForm();
                    return;
                }

                // Validate token and load agreement
                if (ValidateToken(token))
                {
                    LoadAgreementDetails();
                    CheckAgreementStatus();
                }
                else
                {
                    ShowError("Invalid or expired access token. Please request a new link.");
                    DisableForm();
                }
            }
        }

        private bool ValidateToken(string token)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                SELECT id, agreement_status, token_expiry_date, agreement_view_token
                FROM hardware_agreements 
                WHERE agreement_view_token = @token 
                AND (token_expiry_date IS NULL OR token_expiry_date > GETDATE())
                AND agreement_status IN ('Pending', 'Active')";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@token", token);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                agreementId = Convert.ToInt32(reader["id"]);
                                string storedToken = reader["agreement_view_token"].ToString();

                                // Double-check token match
                                if (storedToken == token)
                                {
                                    isTokenValid = true;
                                    return true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Token validation error: " + ex.Message);
                }
            }

            return false;
        }

        private void LoadAgreementDetails()
        {
            if (agreementId == 0) return;

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                        SELECT a.*, m.model 
                        FROM hardware_agreements a
                        LEFT JOIN hardware_model m ON a.model_id = m.id
                        WHERE a.id = @id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", agreementId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Display agreement info
                                agreementNumber.InnerText = reader["agreement_number"].ToString();
                                modelInfo.InnerText = reader["model"].ToString();
                                serialNumber.InnerText = reader["serial_number"].ToString();
                                statusInfo.InnerText = reader["agreement_status"].ToString();

                                // Load employee info if already saved
                                if (!string.IsNullOrEmpty(reader["employee_name"]?.ToString()))
                                {
                                    txtEmployeeName.Text = reader["employee_name"].ToString();
                                }

                                if (!string.IsNullOrEmpty(reader["employee_id"]?.ToString()))
                                {
                                    txtEmployeeID.Text = reader["employee_id"].ToString();
                                }

                                if (!string.IsNullOrEmpty(reader["employee_position"]?.ToString()))
                                {
                                    txtPosition.Text = reader["employee_position"].ToString();
                                }

                                if (!string.IsNullOrEmpty(reader["employee_department"]?.ToString()))
                                {
                                    txtDepartment.Text = reader["employee_department"].ToString();
                                }

                                // Load signature if exists
                                if (!string.IsNullOrEmpty(reader["employee_signature_data"]?.ToString()))
                                {
                                    hdnSignatureData.Value = reader["employee_signature_data"].ToString();
                                    hdnIsSigned.Value = "true";

                                    // You might want to display the saved signature in preview
                                }

                                if (reader["employee_signature_date"] != DBNull.Value)
                                {
                                    txtSignatureDate.Text = Convert.ToDateTime(reader["employee_signature_date"]).ToString("dd/MM/yyyy HH:mm");
                                }

                                if (!string.IsNullOrEmpty(reader["employee_signed_by"]?.ToString()))
                                {
                                    txtSignedBy.Text = reader["employee_signed_by"].ToString();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Error loading agreement details: " + ex.Message);
                }
            }
        }

        private void CheckAgreementStatus()
        {
            if (agreementId == 0) return;

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT agreement_status FROM hardware_agreements WHERE id = @id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", agreementId);
                        object result = command.ExecuteScalar();

                        if (result != null && result.ToString() == "Completed")
                        {
                            ShowMessage("This agreement has already been completed and signed.", "info");
                            DisableForm();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Status check error: " + ex.Message);
                }
            }
        }

        protected void btnSubmitAgreement_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            if (agreementId == 0 || !isTokenValid)
            {
                ShowError("Invalid agreement access.");
                return;
            }

            if (!chkAgreeTerms.Checked)
            {
                ShowError("You must agree to the terms and conditions.");
                return;
            }

            if (hdnIsSigned.Value != "true" || string.IsNullOrEmpty(hdnSignatureData.Value))
            {
                ShowError("Please provide your signature.");
                return;
            }

            // Save agreement
            SaveAgreement();
        }

        private void SaveAgreement()
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Update agreement with employee signature
                    string query = @"
                        UPDATE hardware_agreements SET
                        employee_name = @employeeName,
                        employee_id = @employeeId,
                        employee_position = @position,
                        employee_department = @department,
                        employee_signature_data = @signatureData,
                        employee_signature_date = GETDATE(),
                        employee_signed_by = @signedBy,
                        agreement_status = 'Completed',
                        agreement_completed_date = GETDATE(),
                        agreement_view_token = NULL, -- Invalidate token after completion
                        token_expiry_date = NULL
                        WHERE id = @id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@employeeName", txtEmployeeName.Text.Trim());
                        command.Parameters.AddWithValue("@employeeId", txtEmployeeID.Text.Trim());
                        command.Parameters.AddWithValue("@position", txtPosition.Text.Trim());
                        command.Parameters.AddWithValue("@department", txtDepartment.Text.Trim());
                        command.Parameters.AddWithValue("@signatureData", hdnSignatureData.Value);

                        // Get Windows ID from current user
                        string windowsId = User.Identity.Name;
                        if (windowsId.Contains("\\"))
                        {
                            windowsId = windowsId.Split('\\')[1];
                        }
                        command.Parameters.AddWithValue("@signedBy", windowsId);

                        command.Parameters.AddWithValue("@id", agreementId);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            ShowSuccess("Agreement submitted successfully! Thank you for completing the form.");
                            DisableForm();

                            // Send notification email
                            SendConfirmationEmail();
                        }
                        else
                        {
                            ShowError("Failed to submit agreement. Please try again.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Error saving agreement: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("Save error: " + ex.Message);
                }
            }
        }

        private void SendConfirmationEmail()
        {
            try
            {
                // Get agreement details for email
                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HardwareAgreementConnection"].ConnectionString;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT agreement_number, employee_email, hod_email, it_staff_win_id
                        FROM hardware_agreements WHERE id = @id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", agreementId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string agreementNumber = reader["agreement_number"].ToString();
                                string employeeEmail = reader["employee_email"].ToString();
                                string hodEmail = reader["hod_email"].ToString();
                                string itStaff = reader["it_staff_win_id"].ToString();

                                // Create email message
                                MailMessage mail = new MailMessage();
                                mail.From = new MailAddress("hardware_agreement@pancentury.com", "Hardware Agreement System");
                                mail.To.Add(employeeEmail);
                                mail.CC.Add(hodEmail);
                                mail.CC.Add(itStaff + "@pancentury.com"); // Assuming domain

                                mail.Subject = $"Hardware Agreement {agreementNumber} - Completed";

                                string body = $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #10b981; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #ddd; }}
        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Agreement Completed</h1>
        </div>
        <div class='content'>
            <h2>Hardware Agreement {agreementNumber} has been signed</h2>
            <p><strong>Employee:</strong> {txtEmployeeName.Text}</p>
            <p><strong>Signed Date:</strong> {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}</p>
            <p><strong>Signed By (Windows ID):</strong> {User.Identity.Name}</p>
            
            <p>The hardware agreement has been successfully completed and signed by the employee.</p>
            
            <div class='footer'>
                <p>This is an automated notification from the Hardware Agreement System.</p>
            </div>
        </div>
    </div>
</body>
</html>";

                                mail.Body = body;
                                mail.IsBodyHtml = true;

                                // Send email
                                SmtpClient smtpClient = new SmtpClient();
                                smtpClient.Send(mail);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Confirmation email error: " + ex.Message);
                // Don't show error to user - email failure shouldn't prevent form submission
            }
        }

        private void ShowMessage(string message, string type)
        {
            messagePanel.Visible = true;
            messageText.Text = message;

            switch (type)
            {
                case "success":
                    messagePanel.CssClass = "message message-success";
                    break;
                case "error":
                    messagePanel.CssClass = "message message-error";
                    break;
                case "info":
                    messagePanel.CssClass = "message message-info";
                    break;
            }
        }

        private void ShowSuccess(string message)
        {
            ShowMessage(message, "success");
        }

        private void ShowError(string message)
        {
            ShowMessage(message, "error");
        }

        private void DisableForm()
        {
            txtEmployeeName.Enabled = false;
            txtEmployeeID.Enabled = false;
            txtPosition.Enabled = false;
            txtDepartment.Enabled = false;
            txtSignatureDate.Enabled = false;
            txtSignedBy.Enabled = false;
            chkAgreeTerms.Enabled = false;
            btnSubmitAgreement.Enabled = false;
        }
    }
}