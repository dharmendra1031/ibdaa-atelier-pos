# IBDAA Atelier POS

Clean ASP.NET Core 8 / SQL Server project for **IBDAA Atelier**.
This is a new application. The Mashgal Tailoring codebase is reference-only; no legacy workshop salary or multi-branch modules have been copied.

## Requirements

- [SRS snapshot (10 November 2026)](docs/IBDAA-Atelier-SRS-2026-11-10.md)
- [Original Google Docs SRS](https://docs.google.com/document/d/1OWvb0xnisZzJTEOdGS1sY3PHn_WSn6XmysiM0PtCnok/edit)
- [Implementation stages](docs/implementation-plan.md)
- [Architecture decisions](docs/architecture.md)

## Run locally

Requires .NET SDK 8 and SQL Server.

```powershell
git switch development
dotnet restore src/IbdaaAtelier.Web/IbdaaAtelier.Web.csproj
dotnet run --project src/IbdaaAtelier.Web/IbdaaAtelier.Web.csproj
```

The landing page runs without a database. To view catalogue data, first review `database/001_initial_catalog.sql`, replace the database guard with an approved **test** database name and run it manually in SSMS. Set a local connection string without checking secrets into Git:

```powershell
cd src/IbdaaAtelier.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AtelierDb" "Server=.;Database=IBDAA_TEST;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet run
```

No migration, catalogue import or external website sync runs on application startup.

## Current scope

This initial development build contains the application shell, read-only active product catalogue, category/gallery/size/measurement data models, guarded SQL catalogue schema and a read-only verification script. **Admin login/editing, checkout, payments, invoice printing and website synchronization are not implemented yet.** No sample products or fabricated prices have been loaded.

All new persistent primary keys are numeric SQL `IDENTITY(1,1)`. Use the original website as the catalogue source, but import only after validating its platform, source data, permissions and image storage. Persist historical order-item snapshots when implementing billing.

## Checks

```powershell
node --test tests/*.test.mjs
```

These are lightweight source/schema contract checks only. Run `dotnet build` and perform manual SQL/database/browser acceptance in a real .NET 8 test environment before a release.

Work in `development`; promote reviewed code to `main` only after tests and acceptance.
