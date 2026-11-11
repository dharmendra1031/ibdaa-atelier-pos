# Delivery stages — 10 November 2026

**Execution and acceptance source of truth:** [Master delivery and quality plan](master-delivery-quality-plan.md). This page is a short status checklist, not a competing implementation specification.

## Stage 0 — Architecture and compatibility

- [x] Review previous project bugs and 23-section IBDAA SRS
- [x] Record module-by-module dependencies, scope exclusions and acceptance tests
- [x] Upgrade target framework to .NET 10 LTS and align EF Core SQL Server to 10.0.14
- [x] Execute a clean .NET 10 Restore and Release build in GitHub Actions
- [ ] Confirm actual deployment hosting/runtime and execute isolated SQL / browser smoke checks
- [x] Integrate local Bootstrap 5.3.8 and premium responsive shell with English/Arabic language selection
- [ ] Verify browser rendering at desktop/tablet/mobile widths, LTR/RTL, and physical device touch targets
- [x] Run Node source-contract tests in GitHub Actions (27 text/source checks)
- [ ] Run isolated SQL, real authenticated browser and integration acceptance tests

## Stage 1 — Repository and catalogue foundation (current)

- [x] Separate GitHub repository; `main` and `development` branches
- [x] Preserve the original SRS in `docs/` and link the canonical Google Doc
- [x] Minimal ASP.NET Core 10 MVC application and read-only catalogue screen
- [x] Category, product, gallery, size-chart and measurement models
- [x] SQL Server `IDENTITY` schema script and read-only verification script
- [x] Premium Bootstrap 5.3 UI/UX and performance standards documented
- [ ] Execute SQL verification and authenticated integration tests in an approved test environment
- [ ] Confirm the source website's platform/API and catalogue field mappings

## Stage 2 — Authenticated administration

- [x] Admin/Cashier user model, password hashing, basic role guards, audit and antiforgery code committed
- [ ] Execute login, logout, lockout and role permission tests against isolated SQL and browser
- [x] Product/category CRUD source, bilingual details, status and pricing validation (CI build only)
- [ ] Verify CRUD, permissions, errors and concurrency against isolated SQL and browser
- [ ] Gallery upload with file inspection, primary image and cleanup
- [x] Product size and category/product measurement configuration UI + guarded source code
- [ ] Test size chart boundaries and update forms end-to-end; implement full label/size editing
- [ ] Controlled initial import of real IBDAA products, images and prices

## Stage 2.5 — Additional masters

- [x] Customer create/edit/search code, mobile dedupe and audit committed
- [ ] Verify customer CRUD and Kuwait international-prefix search against isolated SQL
- [ ] Add the separate alteration-service master (draft not yet committed)

## Stage 3 — POS order lifecycle

- [ ] Customer selection, order/cart UI in the SRS two-panel layout
- [ ] Product versus alteration-service line distinction and server-side pricing
- [ ] Per-order-item size and measurement snapshots; historical immutability
- [ ] Numeric request IDs and transaction-safe duplicate order protection
- [ ] Delivery date, order status, permitted discounts and audit

## Stage 4 — Payments, printing and reports

- [ ] Cash/KNET/bank/other, advances, split payments, refunds
- [ ] Register opening/closing and daily collection reports
- [ ] Bilingual invoice from persisted records and device printing checks
- [ ] Sales, product, collection and delivery reports; role-based visibility

## Release gate

Do not deploy or connect the live database until security, backup/restore, API permissions, financial retries, Arabic/English rendering and physical printing are verified. Workshop payroll and multi-branch consolidation are not part of this SRS.
