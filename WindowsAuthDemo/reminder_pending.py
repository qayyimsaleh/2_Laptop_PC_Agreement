#!/usr/bin/env python3
"""
=============================================================================
 Laptop/Desktop Agreement - Daily Email Reminder Script
=============================================================================
 Runs two jobs in one pass:

 JOB 1 — Pending Reminder
   Sends reminders to Employee (TO), HOD (CC), IT Staff (CC) for
   agreements still in "Pending" status (employee hasn't signed yet).
   Urgency escalates the longer it sits unsigned:
     0-2 days  -> REMINDER  (blue)
     3-6 days  -> FOLLOW-UP (amber)
     7+ days   -> URGENT    (red)

 JOB 2 — Draft Expiry Warning
   Sends a warning to the IT staff member who owns a Draft agreement
   that will be deleted TOMORROW by sp_CleanupStaleDrafts (@DaysOld=7).
   Drafts 6-7 days old = expiring tomorrow.

 Requirement:  pip install pyodbc
 Usage:        python reminder_pending.py
               python reminder_pending.py --dry-run
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

# Load .env file if present (requires: pip install python-dotenv)
try:
    from dotenv import load_dotenv
    load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".env"))
except ImportError:
    pass  # Fall back to environment variables already set in the OS


# =============================================================================
# CONFIGURATION — values loaded from environment variables
# Set these in .env (local) or as system environment variables (production)
# =============================================================================

DB_SERVER   = os.getenv("DB_SERVER", "")
DB_NAME     = os.getenv("DB_NAME", "")
DB_DRIVER   = os.getenv("DB_DRIVER", "ODBC Driver 17 for SQL Server")
DB_USERNAME = os.getenv("DB_USERNAME", "")
DB_PASSWORD = os.getenv("DB_PASSWORD", "")

SMTP_SERVER  = os.getenv("SMTP_SERVER", "")
SMTP_PORT    = int(os.getenv("SMTP_PORT", "25"))

FROM_EMAIL   = os.getenv("FROM_EMAIL", "")
FROM_NAME    = os.getenv("FROM_NAME", "Laptop/Desktop Agreement System")

APP_BASE_URL = os.getenv("APP_BASE_URL", "")

# Must match sp_CleanupStaleDrafts @DaysOld value
CLEANUP_DAYS = 7

# Log file (same folder as the script)
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


def get_expiring_drafts(conn):
    """Drafts that are (CLEANUP_DAYS-1) to CLEANUP_DAYS days old — deleted tomorrow."""
    query = f"""
        SELECT
            a.id,
            a.agreement_number,
            a.it_staff_win_id,
            a.employee_email,
            a.created_date,
            COALESCE(a.serial_number, '')    AS serial_number,
            COALESCE(m.model, 'N/A')         AS model_name,
            DATEDIFF(DAY, a.created_date, GETDATE()) AS age_days
        FROM hardware_agreements a
        LEFT JOIN hardware_model m ON a.model_id = m.id
        WHERE a.agreement_status = 'Draft'
          AND a.created_date >= DATEADD(DAY, -{CLEANUP_DAYS},     GETDATE())
          AND a.created_date <  DATEADD(DAY, -({CLEANUP_DAYS}-1), GETDATE())
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


def log_pending_to_db(conn, total, sent, failed):
    try:
        cursor = conn.cursor()
        cursor.execute("""
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='hardware_reminder_log' AND xtype='U')
            CREATE TABLE hardware_reminder_log (
                id            INT IDENTITY(1,1) PRIMARY KEY,
                run_date      DATETIME NOT NULL DEFAULT GETDATE(),
                total_pending INT NOT NULL DEFAULT 0,
                emails_sent   INT NOT NULL DEFAULT 0,
                emails_failed INT NOT NULL DEFAULT 0,
                status        VARCHAR(50) NOT NULL DEFAULT 'Completed'
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


def log_draft_expiry_to_db(conn, total, sent, failed):
    try:
        cursor = conn.cursor()
        cursor.execute("""
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='hardware_draft_expiry_log' AND xtype='U')
            CREATE TABLE hardware_draft_expiry_log (
                id             INT IDENTITY(1,1) PRIMARY KEY,
                run_date       DATETIME NOT NULL DEFAULT GETDATE(),
                total_expiring INT NOT NULL DEFAULT 0,
                emails_sent    INT NOT NULL DEFAULT 0,
                emails_failed  INT NOT NULL DEFAULT 0,
                status         VARCHAR(50) NOT NULL DEFAULT 'Completed'
            )
        """)
        cursor.execute(
            "INSERT INTO hardware_draft_expiry_log (run_date, total_expiring, emails_sent, emails_failed, status) "
            "VALUES (GETDATE(), ?, ?, ?, ?)",
            (total, sent, failed, "Completed" if failed == 0 else "Completed with errors")
        )
        conn.commit()
    except Exception:
        pass


# =============================================================================
# JOB 1 EMAIL — Pending Reminder
# =============================================================================

def get_urgency(days):
    if days >= 7:
        return {"color": "#dc2626", "label": "URGENT",    "icon": "&#9888;",
                "bg": "#fef2f2", "txt": "#991b1b"}
    elif days >= 3:
        return {"color": "#f59e0b", "label": "FOLLOW-UP", "icon": "&#128276;",
                "bg": "#fffbeb", "txt": "#92400e"}
    else:
        return {"color": "#3b82f6", "label": "REMINDER",  "icon": "&#128233;",
                "bg": "#eff6ff", "txt": "#1e40af"}


def build_pending_email(a):
    days     = a["days_pending"] or 0
    u        = get_urgency(days)
    days_txt = f"{days} day{'s' if days != 1 else ''}"
    agr      = a.get("agreement_number")      or "N/A"
    model    = a.get("model_name")            or "N/A"
    serial   = a.get("serial_number")         or "N/A"
    asset    = a.get("asset_number")          or "N/A"
    token    = a.get("agreement_view_token")  or ""
    today    = datetime.now().strftime("%A, %d %B %Y")

    if a.get("submitted_date"):
        submitted = a["submitted_date"].strftime("%d/%m/%Y")
    elif a.get("created_date"):
        submitted = a["created_date"].strftime("%d/%m/%Y")
    else:
        submitted = "N/A"

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

    subject = f"[Phase 2 \u2014 {u['label']}] Laptop/Desktop Agreement {agr} \u2014 Pending Your Agreement ({days_txt})"

    body = f"""<!DOCTYPE html>
<html>
<head><meta charset="utf-8"></head>
<body style="font-family:Arial,sans-serif;line-height:1.6;color:#333;margin:0;padding:0">
<div style="max-width:600px;margin:0 auto">

    <div style="background:{u['color']};color:#fff;padding:24px;text-align:center;border-radius:10px 10px 0 0">
        <p style="margin:0 0 4px;font-size:.75rem;opacity:.75;letter-spacing:.08em;text-transform:uppercase;">Phase 2 &mdash; Pending Employee Signature</p>
        <h1 style="margin:0;font-size:1.4rem">{u['icon']} Agreement Pending Your Signature</h1>
        <p style="margin:8px 0 0;opacity:0.9">Daily Reminder &mdash; {today}</p>
    </div>

    <div style="background:#f9f9f9;padding:24px;border-radius:0 0 10px 10px;border:1px solid #ddd">
        <p>Dear Employee,</p>
        <p>This is a friendly reminder that the following laptop/desktop agreement is still
           <strong>pending your signature</strong>.
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
            <tr><th style="padding:10px;text-align:left;border-bottom:1px solid #ddd;background:#f2f2f2">Status</th>
                <td style="padding:10px;border-bottom:1px solid #ddd">
                    <span style="background:{u['color']};color:#fff;padding:4px 12px;border-radius:20px;
                                 font-weight:bold;font-size:0.85rem">Pending</span></td></tr>
            <tr><th style="padding:10px;text-align:left;border-bottom:1px solid #ddd;background:#f2f2f2">Model</th>
                <td style="padding:10px;border-bottom:1px solid #ddd">{model}</td></tr>
            <tr><th style="padding:10px;text-align:left;border-bottom:1px solid #ddd;background:#f2f2f2">Serial Number</th>
                <td style="padding:10px;border-bottom:1px solid #ddd">{serial}</td></tr>
            <tr><th style="padding:10px;text-align:left;border-bottom:1px solid #ddd;background:#f2f2f2">Asset Number</th>
                <td style="padding:10px;border-bottom:1px solid #ddd">{asset}</td></tr>
            <tr><th style="padding:10px;text-align:left;border-bottom:1px solid #ddd;background:#f2f2f2">Submitted Date</th>
                <td style="padding:10px;border-bottom:1px solid #ddd">{submitted}</td></tr>
            <tr><th style="padding:10px;text-align:left;border-bottom:1px solid #ddd;background:#f2f2f2">Pending Since</th>
                <td style="padding:10px;border-bottom:1px solid #ddd"><strong>{days_txt}</strong></td></tr>
        </table>

        {btn}
        {banner}

        <p style="color:#6b7280;font-size:0.9rem">
            If you have already signed this agreement, please disregard this reminder.
            For any questions or issues, please contact the IT department.</p>

        <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0">
        <p style="color:#9ca3af;font-size:0.8rem;text-align:center">
            This is an automated daily reminder from the Laptop/Desktop Agreement System.<br>
            You will stop receiving reminders once the agreement is signed.</p>
    </div>

</div>
</body>
</html>"""

    return subject, body


def send_pending_email(agreement, it_email, logger, dry_run=False):
    emp_email = agreement.get("employee_email") or ""
    hod_email = agreement.get("hod_email")      or ""
    agr_num   = agreement.get("agreement_number") or "?"

    if not emp_email:
        logger.warning(f"  [{agr_num}] Skipped - no employee email")
        return False

    subject, html_body = build_pending_email(agreement)

    cc_list = []
    if hod_email: cc_list.append(hod_email)
    if it_email:  cc_list.append(it_email)
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
        try: server.quit()
        except Exception: pass


# =============================================================================
# JOB 2 EMAIL — Draft Expiry Warning
# =============================================================================

def build_draft_expiry_email(draft):
    agr       = draft.get("agreement_number") or "N/A"
    model     = draft.get("model_name")       or "N/A"
    serial    = draft.get("serial_number")    or "N/A"
    win_id    = draft.get("it_staff_win_id")  or "N/A"
    emp_email = draft.get("employee_email")   or "\u2014"
    age       = draft.get("age_days") or CLEANUP_DAYS - 1

    created_str = draft["created_date"].strftime("%d/%m/%Y %H:%M") if draft.get("created_date") else "N/A"

    edit_url = f"{APP_BASE_URL.rstrip('/')}/Agreement.aspx?id={draft['id']}" if APP_BASE_URL else ""

    btn = ""
    if edit_url:
        btn = f"""
            <div style="text-align:center;margin:28px 0">
                <a href="{edit_url}"
                   style="display:inline-block;background:#f59e0b;color:#fff;
                          padding:14px 36px;text-decoration:none;border-radius:8px;
                          font-weight:bold;font-size:15px">
                    &#9998; Open &amp; Complete Draft Now
                </a>
            </div>
            <p style="text-align:center;font-size:0.82rem;color:#6b7280">
                Or copy this link:
                <span style="background:#f3f4f6;padding:3px 7px;border-radius:4px;
                             font-family:monospace;font-size:0.8rem;word-break:break-all">{edit_url}</span>
            </p>"""

    subject = (
        f"[Phase 1 \u2014 ACTION REQUIRED] Draft Agreement {agr} "
        f"Expires Tomorrow \u2014 Complete or It Will Be Deleted"
    )

    body = f"""<!DOCTYPE html>
<html>
<head><meta charset="utf-8"></head>
<body style="font-family:Arial,sans-serif;line-height:1.6;color:#333;margin:0;padding:0">
<div style="max-width:600px;margin:0 auto">

    <div style="background:#f59e0b;color:#fff;padding:24px;text-align:center;border-radius:10px 10px 0 0">
        <p style="margin:0 0 4px;font-size:.75rem;opacity:.75;letter-spacing:.08em;text-transform:uppercase;">Phase 1 &mdash; Draft Expiry Warning</p>
        <h1 style="margin:0;font-size:1.35rem">&#9888; Draft Agreement Expiring Tomorrow</h1>
        <p style="margin:8px 0 0;opacity:0.9">
            Auto-deletion runs every {CLEANUP_DAYS} days &mdash;
            this draft is {age} day{'s' if age != 1 else ''} old
        </p>
    </div>

    <div style="background:#f9f9f9;padding:24px;border-radius:0 0 10px 10px;border:1px solid #ddd">
        <p>Dear IT Staff (<strong>{win_id}</strong>),</p>
        <p>
            This draft agreement is <strong style="color:#b45309">{age} day{'s' if age != 1 else ''} old</strong>
            and will be <strong style="color:#dc2626">permanently deleted tomorrow</strong>
            by the scheduled cleanup job
            (<code>sp_CleanupStaleDrafts @DaysOld={CLEANUP_DAYS}</code>).
        </p>

        <div style="background:#fffbeb;padding:14px 16px;border-radius:8px;border-left:4px solid #f59e0b;margin:20px 0">
            <p style="margin:0;color:#92400e">
                <strong>&#9888; To prevent deletion:</strong> open the draft, complete the required
                details and submit it, or manually change its status so it is no longer a draft.
            </p>
        </div>

        <h3 style="color:#374151;border-bottom:2px solid #f59e0b;padding-bottom:6px">Draft Details</h3>
        <table style="width:100%;border-collapse:collapse;margin:16px 0">
            <tr><th style="padding:9px 10px;text-align:left;background:#f2f2f2;border-bottom:1px solid #ddd;width:42%">Agreement Number</th>
                <td style="padding:9px 10px;border-bottom:1px solid #ddd">{agr}</td></tr>
            <tr><th style="padding:9px 10px;text-align:left;background:#f2f2f2;border-bottom:1px solid #ddd">Status</th>
                <td style="padding:9px 10px;border-bottom:1px solid #ddd">
                    <span style="background:#6b7280;color:#fff;padding:3px 12px;border-radius:20px;font-size:.82rem;font-weight:bold">Draft</span>
                </td></tr>
            <tr><th style="padding:9px 10px;text-align:left;background:#f2f2f2;border-bottom:1px solid #ddd">Model</th>
                <td style="padding:9px 10px;border-bottom:1px solid #ddd">{model}</td></tr>
            <tr><th style="padding:9px 10px;text-align:left;background:#f2f2f2;border-bottom:1px solid #ddd">Serial Number</th>
                <td style="padding:9px 10px;border-bottom:1px solid #ddd">{serial}</td></tr>
            <tr><th style="padding:9px 10px;text-align:left;background:#f2f2f2;border-bottom:1px solid #ddd">Intended Employee</th>
                <td style="padding:9px 10px;border-bottom:1px solid #ddd">{emp_email}</td></tr>
            <tr><th style="padding:9px 10px;text-align:left;background:#f2f2f2;border-bottom:1px solid #ddd">Created Date</th>
                <td style="padding:9px 10px;border-bottom:1px solid #ddd">{created_str}</td></tr>
            <tr><th style="padding:9px 10px;text-align:left;background:#f2f2f2;border-bottom:1px solid #ddd">Age</th>
                <td style="padding:9px 10px;border-bottom:1px solid #ddd">
                    <strong style="color:#dc2626">{age} day{'s' if age != 1 else ''}</strong> (deletes tomorrow)
                </td></tr>
        </table>

        {btn}

        <p style="color:#6b7280;font-size:0.9rem">
            If this draft is no longer needed, no action is required &mdash; it will be removed automatically.</p>

        <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0">
        <p style="color:#9ca3af;font-size:0.8rem;text-align:center">
            This is an automated expiry notice from the Laptop/Desktop Agreement System.<br>
            Draft agreements older than {CLEANUP_DAYS} days are removed by the scheduled cleanup job.</p>
    </div>

</div>
</body>
</html>"""

    return subject, body


def send_draft_expiry_email(draft, it_email, logger, dry_run=False):
    agr_num = draft.get("agreement_number") or "?"

    if not it_email:
        logger.warning(f"  [{agr_num}] Skipped - no IT staff email for {draft.get('it_staff_win_id')}")
        return False

    subject, html_body = build_draft_expiry_email(draft)

    if dry_run:
        logger.info(f"  [{agr_num}] DRY RUN -> TO: {it_email}")
        return True

    msg = MIMEMultipart("alternative")
    msg["From"]    = f"{FROM_NAME} <{FROM_EMAIL}>"
    msg["To"]      = it_email
    msg["Subject"] = subject
    msg.attach(MIMEText(html_body, "html", "utf-8"))

    server = smtplib.SMTP(SMTP_SERVER, SMTP_PORT, timeout=30)
    try:
        server.sendmail(FROM_EMAIL, [it_email], msg.as_string())
        logger.info(f"  [{agr_num}] Sent -> TO: {it_email}")
        return True
    finally:
        try: server.quit()
        except Exception: pass


# =============================================================================
# MAIN
# =============================================================================

def main():
    parser = argparse.ArgumentParser(description="Laptop/Desktop Agreement - Daily Reminder")
    parser.add_argument("--dry-run", "-d", action="store_true",
                        help="Preview without sending emails")
    args = parser.parse_args()

    logger = setup_logging()
    logger.info("=" * 60)
    logger.info("Laptop/Desktop Agreement - Daily Reminder")
    logger.info(f"Run time: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    if args.dry_run:
        logger.info("*** DRY RUN MODE - No emails will be sent ***")
    logger.info("=" * 60)

    p_total = p_sent = p_failed = 0
    d_total = d_sent = d_failed = 0

    try:
        logger.info(f"Connecting to {DB_SERVER} / {DB_NAME} ...")
        conn = get_connection()
        logger.info("Connected.")

        # ── JOB 1: Pending reminders ──────────────────────────────────────
        logger.info("")
        logger.info("[ JOB 1 ] Pending Agreement Reminders")
        logger.info("-" * 40)

        agreements = get_pending_agreements(conn)
        p_total = len(agreements)
        logger.info(f"Found {p_total} pending agreement(s)")

        if p_total > 0:
            it_cache = {}
            for a in agreements:
                try:
                    it_em = get_it_email(conn, a.get("it_staff_win_id"), it_cache)
                    if send_pending_email(a, it_em, logger, dry_run=args.dry_run):
                        p_sent += 1
                    else:
                        p_failed += 1
                except Exception as e:
                    p_failed += 1
                    logger.error(f"  [{a.get('agreement_number','?')}] ERROR: {e}")

        logger.info(f"Result: Total={p_total} | Sent={p_sent} | Failed={p_failed}")

        # ── JOB 2: Draft expiry warnings ──────────────────────────────────
        logger.info("")
        logger.info("[ JOB 2 ] Draft Expiry Warnings (expiring tomorrow)")
        logger.info("-" * 40)

        drafts = get_expiring_drafts(conn)
        d_total = len(drafts)
        logger.info(f"Found {d_total} draft(s) expiring tomorrow")

        if d_total > 0:
            it_cache = {}
            for d in drafts:
                try:
                    it_em = get_it_email(conn, d.get("it_staff_win_id"), it_cache)
                    if send_draft_expiry_email(d, it_em, logger, dry_run=args.dry_run):
                        d_sent += 1
                    else:
                        d_failed += 1
                except Exception as e:
                    d_failed += 1
                    logger.error(f"  [{d.get('agreement_number','?')}] ERROR: {e}")

        logger.info(f"Result: Total={d_total} | Sent={d_sent} | Failed={d_failed}")

        # ── Log both jobs to DB ───────────────────────────────────────────
        if not args.dry_run:
            log_pending_to_db(conn,      p_total, p_sent, p_failed)
            log_draft_expiry_to_db(conn, d_total, d_sent, d_failed)

        conn.close()

    except pyodbc.Error as e:
        logger.error(f"Database error: {e}")
        sys.exit(1)
    except Exception as e:
        logger.error(f"Unexpected error: {e}", exc_info=True)
        sys.exit(1)

    total_failed = p_failed + d_failed
    logger.info("")
    logger.info("=" * 60)
    logger.info(f"DONE | Pending sent: {p_sent} | Draft warnings sent: {d_sent} | Failed: {total_failed}")
    logger.info("=" * 60)

    if total_failed > 0:
        sys.exit(2)


if __name__ == "__main__":
    main()