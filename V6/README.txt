APPLIANCE MANAGEMENT + AUTHENTICATOR
====================================
Version notes: Phase 0 enterprise hardening applied (see GitHub commits).

SETUP
-----
1. Ensure SQL Server database APPLIANCE_DB exists (or set connection in license).
2. Run SQL script once:
     V6/Migrations/001_Phase0_EnterpriseFixes.sql
   This adds Products.CurrentStock, SaleDetail.UnitCost, and invoice counters.
3. Build ApplianceManagement and Authenticator (Visual Studio / MSBuild).
4. Generate license.dat with Authenticator → copy next to ApplianceManagement.exe.

AUTHENTICATOR
-------------
- First run: enter installation default, then you MUST set a new master password (min 8 chars).
- Default password is no longer shown on the login screen.
- LICENSE TAB: Store Name, Software Name, Vendor Contact, App Version, Shop Phone,
  Invoice Prefix, Expiry, Allow Print, Connection String → Generate license.dat
- ADMIN TOOLS: Manage Products / Modify Sale / Modify Purchase

MAIN APP
--------
Login (seed): admin / admin123
  (password is upgraded to PBKDF2 automatically on first successful login)

Features hardened in Phase 0:
- License connection string used when present (else App.config)
- PBKDF2 password hashes (legacy SHA256 still accepted once, then upgraded)
- Invoice numbers allocated under row lock (no duplicate invoices under concurrency)
- Sale stock checked with UPDLOCK; Products.CurrentStock maintained when column exists
- Bill prints real InvoiceNo after save
- Logs written to logs/app-YYYYMMDD.log next to the EXE

Settings: Theme (Blue/Green/Dark/Purple/Teal), Font Size, Form Size, Max Discount %
Store branding comes from license.dat only.

IMPORTANT
---------
- Do not commit license.dat or auth_master.dat
- Rebuild after pulling; run the SQL migration before relying on stock locks
