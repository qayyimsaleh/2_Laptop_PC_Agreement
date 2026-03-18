Laptop/PC Hardware Agreement Portal
An ASP.NET Web Forms application for managing hardware loan agreements between IT departments and employees. Built on .NET Framework 4.8 with Windows Authentication and SQL Server.
Features

Windows Authentication — SSO via Active Directory, no separate login required. Role-based access (Admin vs Employee) resolved from the database.
Agreement lifecycle — Create, edit, send for signing, verify, complete, and archive hardware agreements through a multi-phase workflow (Draft → Pending → Agreed → Completed → Archived).
Token-based employee signing — Employees receive a time-limited email link (7-day expiry) to review and digitally sign their agreement without needing admin access.
PDF export — Generate branded, multi-section PDF documents for any agreement using iTextSharp. Export is authorized — only admins or the agreement's employee can download.
Reporting dashboard — Admin-only analytics with filterable charts covering agreement status, hardware types, departments, and IT staff workload.
User management — Admins can add, activate/deactivate, and promote users. First-time visitors are auto-registered as standard users.
Email notifications — Automated emails to employees, HODs, and IT admins at each workflow stage. Sender address and display name are configurable via Web.config.
Dark/light theme — Client-side theme toggle persisted in localStorage.
Responsive design — Single CSS stylesheet (hardware-portal-styles.css) with mobile breakpoints.

Tech Stack

Backend: ASP.NET Web Forms, C#, .NET Framework 4.8
Database: Microsoft SQL Server (via System.Data.SqlClient)
Authentication: Windows Integrated Authentication (IIS)
PDF generation: iTextSharp
Frontend: HTML, CSS (custom design system), JavaScript, Font Awesome, Google Fonts (Sora, DM Mono)

Prerequisites

Windows Server or Windows desktop with IIS enabled
.NET Framework 4.8
SQL Server instance
IIS configured for Windows Authentication

Project Structure
HardwareAgreementPortal/
├── Web.config                      # Connection strings, auth, SMTP, app settings
├── Default.aspx / .cs              # Admin dashboard with statistics
├── Agreement.aspx / .cs            # Create/edit/sign agreement (multi-phase form)
├── ExistingAgreements.aspx / .cs   # Agreement list with filters, pagination, sorting
├── ReportPage.aspx / .cs           # Admin reporting & analytics
├── UserManagement.aspx             # Admin user CRUD
├── ExportPDF.ashx / .cs            # PDF generation handler (iTextSharp, authorized)
├── hardware-portal-styles.css      # Design system stylesheet
└── Guideline_IT_Portal.html        # User guide
Configuration
All sensitive settings belong in Web.config. The repository ships with placeholder values — never commit real credentials.
Connection string:
xml<connectionStrings>
  <add name="HardwareAgreementConnection"
       connectionString="Server=YOUR-SERVER;Database=YOUR-DB;Integrated Security=True;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
Application settings (in <appSettings>):
KeyDescriptionDefaultEmailDomainDomain appended to Windows usernames for auto-registrationyourcompany.comSmtpSenderAddressFrom address on all outbound emailsnoreply@yourcompany.comSmtpSenderDisplayNameDisplay name on outbound emailsLaptop/PC Agreement System
SMTP: Configure your relay in <system.net><mailSettings> per your environment.
Database Setup
Create a SQL Server database with the following core tables:
sqlCREATE TABLE hardware_users (
    id          INT IDENTITY PRIMARY KEY,
    win_id      NVARCHAR(200) NOT NULL,
    email       NVARCHAR(200),
    admin       BIT DEFAULT 0,
    active      BIT DEFAULT 1
);

CREATE TABLE hardware_model (
    id    INT IDENTITY PRIMARY KEY,
    model NVARCHAR(200),
    type  NVARCHAR(100)
);

CREATE TABLE hardware_agreements (
    id                  INT IDENTITY PRIMARY KEY,
    agreement_number    NVARCHAR(50),
    agreement_status    NVARCHAR(50) DEFAULT 'Draft',
    model_id            INT REFERENCES hardware_model(id),
    serial_number       NVARCHAR(100),
    asset_number        NVARCHAR(100),
    employee_email      NVARCHAR(200),
    employee_name       NVARCHAR(200),
    hod_email           NVARCHAR(200),
    it_staff_win_id     NVARCHAR(200),
    access_token        NVARCHAR(500),
    token_expiry_date   DATETIME,
    issue_date          DATETIME,
    created_date        DATETIME DEFAULT GETDATE(),
    archive_remarks     NVARCHAR(MAX)
);

Note: The actual schema may include additional columns for accessories, signatures, and verification fields. Refer to the code-behind files for the complete column list.

Security Model
The application implements multiple layers of security:

Windows Authentication + IIS authorization — anonymous users are denied at the IIS level (<deny users="?" />).
CSRF protection — every page binds ViewStateUserKey to the authenticated Windows identity.
Security headers — X-Frame-Options, X-Content-Type-Options, X-XSS-Protection, and Referrer-Policy are set on every response, including the PDF handler.
Parameterized SQL — all database queries use SqlCommand.Parameters to prevent SQL injection.
Role checks on every PostBack — admin pages re-verify the user's role from the database on each request, not just on initial load, preventing privilege retention after demotion.
Authorized PDF export — the ExportPDF.ashx handler verifies the requesting user is either an admin or the employee who owns the agreement before serving the PDF.
Token expiry — employee signing tokens expire after 7 days. Previous tokens are invalidated when a new one is issued.
Fail-closed error handling — all authorization checks default to denying access on exceptions.
No hardcoded credentials — email domains, SMTP sender addresses, and connection strings are all externalized to Web.config.
Custom error pages — <customErrors mode="RemoteOnly"> prevents stack traces from leaking to remote users.
Debug disabled — debug="false" in the compilation tag for production.

Deployment

Create the SQL Server database and run the schema script.
Update Web.config with your connection string, SMTP settings, and <appSettings> values.
Publish to IIS with Windows Authentication enabled and Anonymous Authentication disabled.
Verify debug="false" is set in the <compilation> tag.
Ensure the IIS application pool identity has access to the database.
