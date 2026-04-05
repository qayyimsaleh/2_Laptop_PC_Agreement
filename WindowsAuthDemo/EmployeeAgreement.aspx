<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EmployeeAgreement.aspx.cs" Inherits="WindowsAuthDemo.EmployeeAgreement" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Employee Agreement - Hardware Portal</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
    <script src="https://cdn.jsdelivr.net/npm/signature_pad@4.1.3/dist/signature_pad.umd.min.js"></script>
    
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
            font-family: 'Segoe UI', 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
        }

        :root {
            --primary: #4361ee;
            --primary-dark: #3a56d4;
            --secondary: #7209b7;
            --success: #10b981;
            --danger: #ef4444;
            --warning: #f59e0b;
            --border-color: #e2e8f0;
            --text-primary: #1e293b;
            --text-secondary: #64748b;
            --bg-color: #f8fafc;
        }

        body {
            background-color: var(--bg-color);
            color: var(--text-primary);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }

        .container {
            max-width: 900px;
            width: 100%;
            background: white;
            border-radius: 16px;
            box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }

        .header {
            background: linear-gradient(135deg, var(--primary), var(--primary-dark));
            color: white;
            padding: 30px;
            text-align: center;
        }

        .header h1 {
            font-size: 1.8rem;
            margin-bottom: 8px;
        }

        .header p {
            opacity: 0.9;
            font-size: 0.95rem;
        }

        .agreement-info {
            background: #f0f9ff;
            padding: 20px 30px;
            border-bottom: 1px solid var(--border-color);
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
        }

        .info-item {
            display: flex;
            flex-direction: column;
        }

        .info-label {
            font-size: 0.8rem;
            color: var(--text-secondary);
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 4px;
        }

        .info-value {
            font-size: 1rem;
            font-weight: 600;
            color: var(--text-primary);
        }

        .content {
            padding: 30px;
        }

        .agreement-title {
            text-align: center;
            margin-bottom: 30px;
            padding-bottom: 20px;
            border-bottom: 2px solid var(--border-color);
        }

        .agreement-title h2 {
            font-size: 1.5rem;
            color: var(--primary);
            margin-bottom: 10px;
        }

        .agreement-body {
            line-height: 1.8;
            margin-bottom: 30px;
        }

        .agreement-body h3 {
            color: var(--text-primary);
            margin: 25px 0 15px 0;
            padding-bottom: 8px;
            border-bottom: 1px solid var(--border-color);
        }

        .agreement-body p {
            margin-bottom: 15px;
            text-align: justify;
        }

        .agreement-body ul {
            margin-left: 20px;
            margin-bottom: 15px;
        }

        .agreement-body li {
            margin-bottom: 8px;
        }

        .form-section {
            margin-top: 30px;
            padding: 25px;
            background: #f8fafc;
            border-radius: 12px;
            border: 1px solid var(--border-color);
        }

        .form-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
            margin-bottom: 25px;
        }

        .form-group {
            margin-bottom: 20px;
        }

        .form-label {
            display: block;
            margin-bottom: 8px;
            font-weight: 600;
            color: var(--text-primary);
        }

        .form-control {
            width: 100%;
            padding: 12px 16px;
            border: 2px solid var(--border-color);
            border-radius: 8px;
            font-size: 1rem;
            background: white;
        }

        .form-control:focus {
            outline: none;
            border-color: var(--primary);
            box-shadow: 0 0 0 3px rgba(67, 97, 238, 0.1);
        }

        .signature-section {
            margin: 25px 0;
        }

        .signature-canvas-container {
            border: 2px dashed var(--border-color);
            border-radius: 12px;
            padding: 20px;
            margin: 20px 0;
            background: white;
        }

        .signature-canvas {
            width: 100%;
            height: 200px;
            background: white;
            border: 1px solid var(--border-color);
            border-radius: 8px;
            cursor: crosshair;
        }

        .signature-actions {
            display: flex;
            gap: 10px;
            margin-top: 15px;
            justify-content: center;
        }

        .signature-btn {
            padding: 10px 20px;
            border: none;
            border-radius: 8px;
            cursor: pointer;
            font-weight: 600;
            transition: all 0.2s;
            display: flex;
            align-items: center;
            gap: 8px;
        }

        .signature-btn-clear {
            background: #f1f5f9;
            color: var(--text-secondary);
            border: 2px solid var(--border-color);
        }

        .signature-btn-clear:hover {
            background: #e2e8f0;
        }

        .signature-btn-save {
            background: linear-gradient(135deg, var(--success), #0da271);
            color: white;
        }

        .signature-btn-save:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);
        }

        .signature-preview {
            margin-top: 20px;
            text-align: center;
        }

        .signature-display {
            max-width: 300px;
            margin: 0 auto;
            border: 1px solid var(--border-color);
            border-radius: 8px;
            padding: 10px;
            background: white;
        }

        .agreement-footer {
            background: #f0f9ff;
            padding: 30px;
            text-align: center;
            border-top: 1px solid var(--border-color);
            margin-top: 30px;
        }

        .btn-submit {
            background: linear-gradient(135deg, var(--primary), var(--primary-dark));
            color: white;
            border: none;
            padding: 15px 40px;
            border-radius: 10px;
            font-size: 1.1rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.3s;
            display: inline-flex;
            align-items: center;
            gap: 10px;
        }

        .btn-submit:hover {
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(67, 97, 238, 0.3);
        }

        .btn-submit:disabled {
            opacity: 0.6;
            cursor: not-allowed;
            transform: none;
        }

        .message {
            padding: 15px 20px;
            border-radius: 8px;
            margin: 20px 0;
            display: flex;
            align-items: center;
            gap: 12px;
            animation: fadeIn 0.5s;
        }

        .message-success {
            background: linear-gradient(135deg, #d1fae5, #a7f3d0);
            color: #065f46;
            border: 1px solid #6ee7b7;
        }

        .message-error {
            background: linear-gradient(135deg, #fee2e2, #fecaca);
            color: #991b1b;
            border: 1px solid #fca5a5;
        }

        .message-info {
            background: linear-gradient(135deg, #dbeafe, #bfdbfe);
            color: #1e40af;
            border: 1px solid #93c5fd;
        }

        .readonly-badge {
            display: inline-block;
            padding: 5px 15px;
            background: #10b981;
            color: white;
            border-radius: 20px;
            font-size: 0.85rem;
            font-weight: 600;
            margin-left: 10px;
        }

        @keyframes fadeIn {
            from {
                opacity: 0;
                transform: translateY(10px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }

        @media (max-width: 768px) {
            .container {
                margin: 10px;
            }
            
            .content, .header, .agreement-footer {
                padding: 20px;
            }
            
            .agreement-info {
                grid-template-columns: 1fr;
            }
            
            .form-grid {
                grid-template-columns: 1fr;
            }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <!-- Header -->
            <div class="header">
                <h1>Employee Laptop/PC Agreement Form</h1>
                <p>Please review and sign the agreement below</p>
            </div>

            <!-- Agreement Information -->
            <div class="agreement-info" id="agreementInfo" runat="server">
                <div class="info-item">
                    <span class="info-label">Agreement Number</span>
                    <span class="info-value" id="agreementNumber" runat="server"></span>
                </div>
                <div class="info-item">
                    <span class="info-label">Model</span>
                    <span class="info-value" id="modelInfo" runat="server"></span>
                </div>
                <div class="info-item">
                    <span class="info-label">Serial Number</span>
                    <span class="info-value" id="serialNumber" runat="server"></span>
                </div>
                <div class="info-item">
                    <span class="info-label">Status</span>
                    <span class="info-value" id="statusInfo" runat="server"></span>
                </div>
            </div>

            <!-- Messages -->
            <asp:Panel ID="messagePanel" runat="server" Visible="false" CssClass="message">
                <i class="fas fa-info-circle"></i>
                <asp:Label ID="messageText" runat="server"></asp:Label>
            </asp:Panel>

            <!-- Main Content -->
            <div class="content">
                <div class="agreement-title">
                    <h2>Hardware Usage Agreement</h2>
                    <p>Please read carefully before signing</p>
                </div>

                <!-- Agreement Body -->
                <div class="agreement-body">
                    <h3>Employee Information</h3>
                    
                    <div class="form-grid">
                        <div class="form-group">
                            <label class="form-label">Employee Name</label>
                            <asp:TextBox ID="txtEmployeeName" runat="server" CssClass="form-control" 
                                placeholder="Enter your full name"></asp:TextBox>
                        </div>
                        
                        <div class="form-group">
                            <label class="form-label">Employee ID</label>
                            <asp:TextBox ID="txtEmployeeID" runat="server" CssClass="form-control" 
                                placeholder="Enter employee ID"></asp:TextBox>
                        </div>
                        
                        <div class="form-group">
                            <label class="form-label">Position/Title</label>
                            <asp:TextBox ID="txtPosition" runat="server" CssClass="form-control" 
                                placeholder="Enter your position"></asp:TextBox>
                        </div>
                        
                        <div class="form-group">
                            <label class="form-label">Department</label>
                            <asp:TextBox ID="txtDepartment" runat="server" CssClass="form-control" 
                                placeholder="Enter department"></asp:TextBox>
                        </div>
                    </div>

                    <h3>Terms and Conditions</h3>
                    
                    <p>In acceptance of this device (Laptop/PC) for usage, I agree to the terms and conditions stated below:</p>
                    
                    <ul>
                        <li>I understand that I am responsible for the laptop/PC whilst in my possession.</li>
                        <li>I am responsible for keeping the laptop/PC in good condition while using it and until the time of return.</li>
                        <li>I understand that I should not install any program or software that is not permitted to use by the company, for privacy and security reasons.</li>
                        <li>I should be the only authorized person to have access to and use this laptop/PC. Any unauthorized access to this laptop/PC is a violation of this company's policy, employment regulation and employment contract.</li>
                        <li>I should remove all data that is not company or work-related before turning over the laptop/PC to the designated department.</li>
                        <li>In the event of loss, theft, or damage, this must be reported to the police within 24-48 hours, and a copy of a Police report or incident report must be submitted to the company for verification purposes.</li>
                        <li>I understand that any violation of these policies is a violation and I am subject to any disciplinary action by the company.</li>
                    </ul>

                    <!-- Signature Section -->
                    <div class="form-section">
                        <h3 style="margin-top: 0;">Digital Signature</h3>
                        
                        <div class="signature-section">
                            <label class="form-label">Draw your signature below:</label>
                            <div class="signature-canvas-container">
                                <canvas id="signatureCanvas" class="signature-canvas"></canvas>
                                <div class="signature-actions">
                                    <button type="button" id="clearSignature" class="signature-btn signature-btn-clear">
                                        <i class="fas fa-eraser"></i> Clear Signature
                                    </button>
                                    <button type="button" id="saveSignature" class="signature-btn signature-btn-save">
                                        <i class="fas fa-save"></i> Save Signature
                                    </button>
                                </div>
                            </div>
                            
                            <div class="signature-preview">
                                <label class="form-label">Signature Preview:</label>
                                <div class="signature-display">
                                    <img id="signaturePreview" src="" alt="Signature Preview" style="max-width: 100%; display: none;" />
                                    <p id="noSignatureText" style="color: var(--text-secondary); font-style: italic;">No signature saved yet</p>
                                </div>
                            </div>
                            
                            <!-- Hidden fields for signature data -->
                            <asp:HiddenField ID="hdnSignatureData" runat="server" />
                            <asp:HiddenField ID="hdnIsSigned" runat="server" Value="false" />
                            
                            <div class="form-group" style="margin-top: 20px;">
                                <label class="form-label">Signature Date</label>
                                <asp:TextBox ID="txtSignatureDate" runat="server" CssClass="form-control" 
                                    ReadOnly="true"></asp:TextBox>
                            </div>
                            
                            <div class="form-group">
                                <label class="form-label">Signed By (Windows ID)</label>
                                <asp:TextBox ID="txtSignedBy" runat="server" CssClass="form-control" 
                                    ReadOnly="true"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Agreement Acceptance -->
                <div class="form-section">
                    <div class="form-group" style="text-align: center;">
                        <asp:CheckBox ID="chkAgreeTerms" runat="server" />
                        <label for="chkAgreeTerms" style="font-weight: 600; margin-left: 8px;">
                            I have read, understood, and agree to all the terms and conditions stated above
                        </label>
                        <asp:CustomValidator ID="cvAgreeTerms" runat="server" 
                            ErrorMessage="You must agree to the terms and conditions"
                            ClientValidationFunction="validateAgreement"
                            Display="Dynamic" ForeColor="#ef4444">
                        </asp:CustomValidator>
                    </div>
                </div>
            </div>

            <!-- Footer with Submit Button -->
            <div class="agreement-footer">
                <asp:Button ID="btnSubmitAgreement" runat="server" Text="Submit Agreement" 
                    CssClass="btn-submit" OnClick="btnSubmitAgreement_Click" />
                <div style="margin-top: 15px; font-size: 0.9rem; color: var(--text-secondary);">
                    <i class="fas fa-info-circle"></i> Once submitted, this agreement will be finalized and cannot be modified.
                </div>
            </div>
        </div>
    </form>

    <script>
        let signaturePad = null;
        let canvas = null;

        document.addEventListener('DOMContentLoaded', function () {
            // Initialize signature pad
            canvas = document.getElementById('signatureCanvas');
            if (canvas) {
                signaturePad = new SignaturePad(canvas, {
                    backgroundColor: 'white',
                    penColor: 'rgb(0, 0, 0)',
                    throttle: 16
                });
                
                // Handle clear button
                document.getElementById('clearSignature').addEventListener('click', function () {
                    signaturePad.clear();
                    document.getElementById('signaturePreview').style.display = 'none';
                    document.getElementById('noSignatureText').style.display = 'block';
                    document.getElementById('<%= hdnSignatureData.ClientID %>').value = '';
                    document.getElementById('<%= hdnIsSigned.ClientID %>').value = 'false';
                });
                
                // Handle save button
                document.getElementById('saveSignature').addEventListener('click', function () {
                    if (signaturePad.isEmpty()) {
                        alert('Please provide your signature first.');
                        return;
                    }
                    
                    const signatureData = signaturePad.toDataURL('image/png');
                    document.getElementById('<%= hdnSignatureData.ClientID %>').value = signatureData;
                    document.getElementById('<%= hdnIsSigned.ClientID %>').value = 'true';
                    
                    // Show preview
                    const previewImg = document.getElementById('signaturePreview');
                    previewImg.src = signatureData;
                    previewImg.style.display = 'block';
                    document.getElementById('noSignatureText').style.display = 'none';
                    
                    // Show success message
                    showMessage('Signature saved successfully!', 'success');
                });
            }
            
            // Auto-fill current user info if available
            const userName = '<%= Page.User.Identity.Name %>';
            if (userName && userName !== '') {
                // Extract username from domain\username format
                const parts = userName.split('\\');
                if (parts.length > 1) {
                    document.getElementById('<%= txtSignedBy.ClientID %>').value = parts[1];
                } else {
                    document.getElementById('<%= txtSignedBy.ClientID %>').value = userName;
                }
            }
            
            // Set current date
            const now = new Date();
            const formattedDate = now.toLocaleDateString('en-GB') + ' ' + now.toLocaleTimeString('en-GB');
            document.getElementById('<%= txtSignatureDate.ClientID %>').value = formattedDate;
            
            // Check if agreement is already completed
            const statusElement = document.getElementById('<%= statusInfo.ClientID %>');
            if (statusElement && statusElement.innerText === 'Completed') {
                disableForm();
            }
            
            // Resize canvas on window resize
            window.addEventListener('resize', resizeCanvas);
            resizeCanvas();
        });
        
        function resizeCanvas() {
            if (canvas) {
                const ratio = Math.max(window.devicePixelRatio || 1, 1);
                canvas.width = canvas.offsetWidth * ratio;
                canvas.height = canvas.offsetHeight * ratio;
                canvas.getContext("2d").scale(ratio, ratio);
                if (signaturePad) {
                    signaturePad.clear(); // Clear signature pad on resize
                }
            }
        }
        
        function validateAgreement(source, args) {
            const isAgreed = document.getElementById('<%= chkAgreeTerms.ClientID %>').checked;
            const isSigned = document.getElementById('<%= hdnIsSigned.ClientID %>').value === 'true';
            
            if (!isAgreed || !isSigned) {
                args.IsValid = false;
                showMessage('Please agree to the terms and provide your signature.', 'error');
            } else {
                args.IsValid = true;
            }
        }
        
        function disableForm() {
            // Disable all form elements
            const form = document.getElementById('form1');
            const elements = form.querySelectorAll('input, textarea, button, select, canvas');
            elements.forEach(element => {
                if (element.id !== '<%= btnSubmitAgreement.ClientID %>') {
                    element.disabled = true;
                    element.style.opacity = '0.6';
                    element.style.cursor = 'not-allowed';
                }
            });
            
            // Disable signature pad
            if (signaturePad) {
                signaturePad.off();
            }
            
            // Hide submit button
            document.getElementById('<%= btnSubmitAgreement.ClientID %>').style.display = 'none';
            
            // Show completed message
            showMessage('This agreement has already been completed and signed.', 'info');
        }
        
        function showMessage(message, type) {
            // You can implement a toast notification here
            console.log(`${type}: ${message}`);
            alert(message); // Simple alert for now
        }
    </script>
</body>
</html>