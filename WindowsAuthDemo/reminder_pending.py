#!/usr/bin/env python3
"""
=============================================================================
 Laptop/PC Agreement - Pending Agreement Daily Email Reminder
=============================================================================
 Sends email reminders to Employee (TO), HOD (CC), and IT Admin (CC)
 for laptop/pc agreements still in "Pending" status.

 Requirement:  pip install pyodbc
 Usage:        python pending_agreement_reminder.py
               python pending_agreement_reminder.py --dry-run
=============================================================================
"""

import pyodbc
import smtplib
import logging
import argparse
import sys
import os
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from datetime import datetime


# =============================================================================
# CONFIGURATION
# =============================================================================

# Database (SQL Server)
DB_SERVER   = r""
DB_NAME     = ""
DB_DRIVER   = ""
DB_USERNAME = ""
DB_PASSWORD = ""

# SMTP (no credentials needed)
SMTP_SERVER = ""
SMTP_PORT   = 

# Email sender
FROM_EMAIL  = ""
FROM_NAME   = "Laptop/PC Agreement System"

# Application base URL (for "Sign Agreement Now" button link in email)
APP_BASE_URL = ""

# Log file (same folder as the script / exe)
LOG_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "reminder_log.txt")


# =============================================================================
# LOGGING
# =============================================================================

def setup_logging():
    logger = logging.getLogger("Reminder")
    logger.setLevel(logging.DEBUG)
    fmt = logging.Formatter("%(asctime)s [%(levelname)s] %(message)s", datefmt="%Y-%m-%d %H:%M:%S")

    ch = logging.StreamHandler(sys.stdout)
    ch.setLevel(logging.INFO)
    ch.setFormatter(fmt)
    logger.addHandler(ch)

    try:
        fh = logging.FileHandler(LOG_FILE, encoding="utf-8")
        fh.setLevel(logging.DEBUG)
        fh.setFormatter(fmt)
        logger.addHandler(fh)
    except Exception:
        pass

    return logger


# =============================================================================
# DATABASE
# =============================================================================

def get_connection():
    conn_str = (
        f"DRIVER={DB_DRIVER};"
        f"SERVER={DB_SERVER};"
        f"DATABASE={DB_NAME};"
        f"UID={DB_USERNAME};"
        f"PWD={DB_PASSWORD};"
    )
    return pyodbc.connect(conn_str, timeout=30)


def get_pending_agreements(conn):
    query = """
        SELECT 
            a.id,
            a.agreement_number,
            a.employee_email,
            a.hod_email,
            a.it_staff_win_id,
            a.agreement_view_token,
            a.created_date,
            a.submitted_date,
            COALESCE(a.employee_name, '')    AS employee_name,
            COALESCE(a.serial_number, '')    AS serial_number,
            COALESCE(a.asset_number, '')     AS asset_number,
            COALESCE(m.model, 'N/A')         AS model_name,
            DATEDIFF(DAY, COALESCE(a.submitted_date, a.created_date), GETDATE()) AS days_pending
        FROM hardware_agreements a
        LEFT JOIN hardware_model m ON a.model_id = m.id
        WHERE a.agreement_status = 'Pending'
        ORDER BY a.created_date ASC
    """
    cursor = conn.cursor()
    cursor.execute(query)
    columns = [d[0] for d in cursor.description]
    return [dict(zip(columns, row)) for row in cursor.fetchall()]


def get_it_email(conn, win_id, cache):
    if not win_id:
        return ""
    if win_id in cache:
        return cache[win_id]
    try:
        row = conn.cursor().execute(
            "SELECT email FROM hardware_users WHERE win_id = ? AND active = 1", (win_id,)
        ).fetchone()
        email = row[0] if row and row[0] else ""
    except Exception:
        email = ""
    cache[win_id] = email
    return email


def log_to_db(conn, total, sent, failed):
    try:
        cursor = conn.cursor()
        cursor.execute("""
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='hardware_reminder_log' AND xtype='U')
            CREATE TABLE hardware_reminder_log (
                id INT IDENTITY(1,1) PRIMARY KEY,
                run_date DATETIME NOT NULL DEFAULT GETDATE(),
                total_pending INT NOT NULL DEFAULT 0,
                emails_sent INT NOT NULL DEFAULT 0,
                emails_failed INT NOT NULL DEFAULT 0,
                status VARCHAR(50) NOT NULL DEFAULT 'Completed'
            )
        """)
        cursor.execute(
            "INSERT INTO hardware_reminder_log (run_date, total_pending, emails_sent, emails_failed, status) "
            "VALUES (GETDATE(), ?, ?, ?, ?)",
            (total, sent, failed, "Completed" if failed == 0 else "Completed with errors")
        )
        conn.commit()
    except Exception:
        pass


# =============================================================================
# EMAIL
# =============================================================================

def get_urgency(days):
    if days >= 7:
        return {"color": "#dc2626", "label": "URGENT", "icon": "&#9888;",
                "bg": "#fef2f2", "txt": "#991b1b"}
    elif days >= 3:
        return {"color": "#f59e0b", "label": "FOLLOW-UP", "icon": "&#128276;",
                "bg": "#fffbeb", "txt": "#92400e"}
    else:
        return {"color": "#3b82f6", "label": "REMINDER", "icon": "&#128233;",
                "bg": "#eff6ff", "txt": "#1e40af"}


def build_email(a):
    days = a["days_pending"] or 0
    u = get_urgency(days)
    days_txt = f"{days} day{'s' if days != 1 else ''}"
    agr = a.get("agreement_number") or "N/A"
    model = a.get("model_name") or "N/A"
    serial = a.get("serial_number") or "N/A"
    asset = a.get("asset_number") or "N/A"
    token = a.get("agreement_view_token") or ""
    today = datetime.now().strftime("%A, %d %B %Y")

    submitted = ""
    if a.get("submitted_date"):
        submitted = a["submitted_date"].strftime("%d/%m/%Y")
    elif a.get("created_date"):
        submitted = a["created_date"].strftime("%d/%m/%Y")
    else:
        submitted = "N/A"

    # Sign agreement button
    btn = ""
    if token:
        url = f"{APP_BASE_URL.rstrip('/')}/Agreement.aspx?token={token}"
        btn = f"""
            <div style="text-align:center;margin:30px 0">
                <a href="{url}" style="display:inline-block;background:{u['color']};color:#fff;
                   padding:16px 40px;text-decoration:none;border-radius:8px;font-weight:bold;font-size:16px">
                    {u['icon']} Sign Agreement Now
                </a>
            </div>
            <p style="text-align:center;font-size:0.85rem;color:#6b7280">
                Or copy this link: <span style="background:#f3f4f6;padding:4px 8px;border-radius:4px;
                font-family:monospace;font-size:0.8rem;word-break:break-all">{url}</span>
            </p>"""

    # Urgency banner
    banner = ""
    if days >= 7:
        banner = f"""
            <div style="background:{u['bg']};padding:16px;border-radius:8px;border-left:4px solid {u['color']};margin:20px 0">
                <p style="margin:0;color:{u['txt']}">
                    <strong>{u['icon']} Urgent:</strong> This agreement has been pending for over 7 days.
                    Please sign it as soon as possible or contact IT support if you have any questions.
                </p>
            </div>"""
    elif days >= 3:
        banner = f"""
            <div style="background:{u['bg']};padding:16px;border-radius:8px;border-left:4px solid {u['color']};margin:20px 0">
                <p style="margin:0;color:{u['txt']}">
                    <strong>{u['icon']} Follow-up:</strong> This agreement has been pending for {days_txt}.
                    Please take a moment to review and sign it.
                </p>
            </div>"""

    subject = f"[{u['label']}] Laptop/PC Agreement {agr} - Pending Your Agreement ({days_txt})"

    body = f"""<!DOCTYPE html>
<html>
<head><meta charset="utf-8"></head>
<body style="font-family:Arial,sans-serif;line-height:1.6;color:#333;margin:0;padding:0">
<div style="max-width:600px;margin:0 auto">

    <div style="background:{u['color']};color:#fff;padding:24px;text-align:center;border-radius:10px 10px 0 0">
        <h1 style="margin:0;font-size:1.5rem">{u['icon']} Agreement Pending Your Agreement</h1>
        <p style="margin:8px 0 0;opacity:0.9">Daily Reminder - {today}</p>
    </div>

    <div style="background:#f9f9f9;padding:24px;border-radius:0 0 10px 10px;border:1px solid #ddd">
        <p>Dear Employee,</p>
        <p>This is a friendly reminder that the following laptop/pc agreement is still
           <strong>pending your agreement</strong>.
           Please review and sign the agreement at your earliest convenience.</p>

        <div style="background:#fff;padding:16px;border-radius:8px;border:1px solid #e5e7eb;margin:20px 0;overflow:hidden">
            <span style="font-weight:700;font-size:1.1rem;color:#1e293b">Agreement #{agr}</span>
            <span style="float:right;background:{u['color']};color:#fff;padding:6px 14px;
                         border-radius:20px;font-weight:bold;font-size:0.85rem">{days_txt} pending</span>
        </div>

        <h3 style="color:#374151;border-bottom:2px solid {u['color']};padding-bottom:8px">Agreement Details</h3>
        <table style="width:100%;border-collapse:collapse;margin:20px 0">
            <tr><th style="padding:10px;text-align:left;border-bottom:1px solid #ddd;background:#f2f2f2;width:40%">Agreement Number</th>
                <td style="padding:10px;border-bottom:1px solid #ddd">{agr}</td></tr>
            <tr><th style="padding:10px;text-align:left;border-bottom:1px solid #ddd;background:#f2f2f2;width:40%">Status</th>
                <td style="padding:10px;border-bottom:1px solid #ddd">
                    <span style="background:{u['color']};color:#fff;padding:4px 12px;border-radius:20px;
                                 font-weight:bold;font-size:0.85rem">Pending</span></td></tr>
            <tr><th style="padding:10px;text-align:left;border-bottom:1px solid #ddd;background:#f2f2f2;width:40%">Model</th>
                <td style="padding:10px;border-bottom:1px solid #ddd">{model}</td></tr>
            <tr><th style="padding:10px;text-align:left;border-bottom:1px solid #ddd;background:#f2f2f2;width:40%">Serial Number</th>
                <td style="padding:10px;border-bottom:1px solid #ddd">{serial}</td></tr>
            <tr><th style="padding:10px;text-align:left;border-bottom:1px solid #ddd;background:#f2f2f2;width:40%">Asset Number</th>
                <td style="padding:10px;border-bottom:1px solid #ddd">{asset}</td></tr>
            <tr><th style="padding:10px;text-align:left;border-bottom:1px solid #ddd;background:#f2f2f2;width:40%">Submitted Date</th>
                <td style="padding:10px;border-bottom:1px solid #ddd">{submitted}</td></tr>
            <tr><th style="padding:10px;text-align:left;border-bottom:1px solid #ddd;background:#f2f2f2;width:40%">Pending Since</th>
                <td style="padding:10px;border-bottom:1px solid #ddd"><strong>{days_txt}</strong></td></tr>
        </table>

        {btn}
        {banner}

        <p style="color:#6b7280;font-size:0.9rem">
            If you have already signed this agreement, please disregard this reminder.
            For any questions or issues, please contact the IT department.</p>

        <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0">
        <p style="color:#9ca3af;font-size:0.8rem;text-align:center">
            This is an automated daily reminder from the Laptop/PC Agreement System.<br>
            You will stop receiving reminders once the agreement is signed.</p>
    </div>

</div>
</body>
</html>"""

    return subject, body


def send_email(agreement, it_email, logger, dry_run=False):
    emp_email = agreement.get("employee_email") or ""
    hod_email = agreement.get("hod_email") or ""
    agr_num = agreement.get("agreement_number") or "?"

    if not emp_email:
        logger.warning(f"  [{agr_num}] Skipped - no employee email")
        return False

    subject, html_body = build_email(agreement)

    cc_list = []
    if hod_email:
        cc_list.append(hod_email)
    if it_email:
        cc_list.append(it_email)
    all_recipients = [emp_email] + cc_list

    if dry_run:
        logger.info(f"  [{agr_num}] DRY RUN -> TO: {emp_email} | CC: {', '.join(cc_list) or 'none'}")
        return True

    msg = MIMEMultipart("alternative")
    msg["From"]    = f"{FROM_NAME} <{FROM_EMAIL}>"
    msg["To"]      = emp_email
    msg["Subject"] = subject
    if cc_list:
        msg["Cc"] = ", ".join(cc_list)
    msg.attach(MIMEText(html_body, "html", "utf-8"))

    server = smtplib.SMTP(SMTP_SERVER, SMTP_PORT, timeout=30)
    try:
        server.sendmail(FROM_EMAIL, all_recipients, msg.as_string())
        logger.info(f"  [{agr_num}] Sent -> TO: {emp_email} | CC: {', '.join(cc_list) or 'none'}")
        return True
    finally:
        try:
            server.quit()
        except Exception:
            pass


# =============================================================================
# MAIN
# =============================================================================

def main():
    parser = argparse.ArgumentParser(description="Laptop/PC Agreement - Pending Reminder")
    parser.add_argument("--dry-run", "-d", action="store_true",
                        help="Preview without sending emails")
    args = parser.parse_args()

    logger = setup_logging()
    logger.info("=" * 60)
    logger.info("Laptop/PC Agreement - Pending Reminder")
    logger.info(f"Run time: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    if args.dry_run:
        logger.info("*** DRY RUN MODE - No emails will be sent ***")
    logger.info("=" * 60)

    sent = 0
    failed = 0
    total = 0

    try:
        logger.info(f"Connecting to {DB_SERVER} / {DB_NAME} ...")
        conn = get_connection()
        logger.info("Connected.")

        agreements = get_pending_agreements(conn)
        total = len(agreements)
        logger.info(f"Found {total} pending agreement(s)")

        if total == 0:
            logger.info("Nothing to send.")
        else:
            it_cache = {}
            logger.info("-" * 40)

            for a in agreements:
                try:
                    it_em = get_it_email(conn, a.get("it_staff_win_id"), it_cache)
                    if send_email(a, it_em, logger, dry_run=args.dry_run):
                        sent += 1
                    else:
                        failed += 1
                except Exception as e:
                    failed += 1
                    logger.error(f"  [{a.get('agreement_number', '?')}] ERROR: {e}")

            logger.info("-" * 40)

        if not args.dry_run:
            log_to_db(conn, total, sent, failed)

        conn.close()

    except pyodbc.Error as e:
        logger.error(f"Database error: {e}")
        sys.exit(1)
    except Exception as e:
        logger.error(f"Error: {e}", exc_info=True)
        sys.exit(1)

    logger.info("")
    logger.info(f"DONE | Pending: {total} | Sent: {sent} | Failed: {failed}")
    logger.info("=" * 60)

    if failed > 0:
        sys.exit(2)


if __name__ == "__main__":
    main()