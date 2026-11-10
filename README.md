# IBDAA Atelier POS

Clean ASP.NET Core 10 / SQL Server project for **IBDAA Atelier**.
This is a new application. The Mashgal Tailoring codebase is reference-only; no legacy workshop salary or multi-branch modules have been copied.

## Requirements

- [SRS snapshot (10 November 2026)](docs/IBDAA-Atelier-SRS-2026-11-10.md)
- [Original Google Docs SRS](https://docs.google.com/document/d/1OWvb0xnisZzJTEOdGS1sY3PHn_WSn6XmysiM0PtCnok/edit)
- [Master delivery and QA plan](docs/master-delivery-quality-plan.md)
- [Implementation stages](docs/implementation-plan.md)
- [Architecture decisions](docs/architecture.md)
- [Premium Bootstrap UI and performance standards](docs/ui-ux-performance-standards.md)

## Run locally

The P0 scaffold targets **.NET 10 LTS**, EF Core SQL Server 10.0.14 and locally served Bootstrap 5.3.8. Install a .NET 10 SDK and SQL Server for development. Hosting capability and a successful .NET build still require verification; the migration has not been applied.

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

This initial development build contains a premium responsive Bootstrap shell with English/Arabic switching, a read-only active product catalogue, category/gallery/size/measurement data models, guarded SQL catalogue schema and a read-only verification script. **Admin login/editing, checkout, payments, invoice printing and website synchronization are not implemented yet.** No sample products or fabricated prices have been loaded.

All new persistent primary keys are numeric SQL `IDENTITY(1,1)`. Use the original website as the catalogue source, but import only after validating its platform, source data, permissions and image storage. Persist historical order-item snapshots when implementing billing.

## Checks

```powershell
node --test tests/*.test.mjs
```

These are lightweight source/schema contract checks only. Run `dotnet build` and perform manual SQL/database/browser acceptance in a real .NET 10 test environment before a release.

The P0 UI uses the self-hosted Bootstrap 5.3.8 LTR/RTL CSS variants (MIT licensed) from `wwwroot/lib/bootstrap`, loading only one variant per request. No CDN or Bootstrap JavaScript bundle is loaded because the current screens do not need interactive Bootstrap components. The language-switch form uses the ASP.NET Core antiforgery tag helper and a strictly local redirect.

Work in `development`; promote reviewed code to `main` only after tests and acceptance.
