# P1: Secure login setup

**Implementation:** Numeric Users/AuditLogs, secure ASP.NET Core cookie authentication, admin-only area, password hashing and role checks. These are **source changes only** until compiled and tested against the isolated database.

1. Create a backed-up, isolated SQL Server development database.
2. Run `database/001_initial_catalog.sql`, then `database/003_user_authentication.sql` after setting `@ExpectedDatabase` to the exact test database name in **each** script. These scripts intentionally refuse any other database. Do **not** run them on production.
3. Check `database/002_verify_catalog.sql` and `database/004_verify_user_authentication.sql` (set the same test database guard).
4. Configure local secrets from the web-project folder (never commit real values):

   ```powershell
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:AtelierDb" "Server=.;Database=IBDAA_TEST;Trusted_Connection=True;TrustServerCertificate=True;"
   dotnet user-secrets set "Bootstrap:AdminUser" "admin"
   dotnet user-secrets set "Bootstrap:AdminPassword" "REPLACE_WITH_A_STRONG_UNIQUE_12_PLUS_CHARACTER_PASSWORD"
   dotnet run -- --bootstrap-admin
   dotnet user-secrets remove "Bootstrap:AdminPassword"
   dotnet user-secrets remove "Bootstrap:AdminUser"
   ```

5. Run `dotnet run` and open the HTTPS launch URL. A secure cookie intentionally will **not** authenticate over plain HTTP.
6. Log in at `/Auth/Login`. The `/Admin` route must reject unauthenticated users and non-Admin roles; logout uses POST with antiforgery.

**Security review:** Change in test DB must be verified against authenticated browser traffic before production. No default admin password or seeded users are included. Lockout starts after five failed attempts for 15 minutes. Disabled users are rejected by server-side cookie validation; user role changes invalidate the existing cookie. User-management CRUD and permission matrix beyond Admin/Cashier are planned next.

**Release blocker:** Authentication code must pass real .NET 10 build, test SQL application, browser session expiry/logout, disabled account, incorrect password, CSRF and access-control checks. Source checks do not certify release.
