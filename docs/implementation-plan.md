# Delivery stages — 10 November 2026

## Stage 1 — Repository and catalogue foundation (current)

- [x] Separate GitHub repository; `main` and `development` branches
- [x] Preserve the original SRS in `docs/` and link the canonical Google Doc
- [x] Minimal ASP.NET Core 8 MVC application and read-only catalogue screen
- [x] Category, product, gallery, size-chart and measurement models
- [x] SQL Server `IDENTITY` schema script and read-only verification script
- [ ] Execute .NET build and SQL verification in an approved test environment
- [ ] Confirm the source website's platform/API and catalogue field mappings

## Stage 2 — Authenticated administration

- [ ] User accounts, password hashing, roles, audit trail and antiforgery
- [ ] Product/category CRUD, bilingual details, status and pricing validation
- [ ] Gallery upload with file inspection, primary image and cleanup
- [ ] Product sizes and configurable category/product measurement forms
- [ ] Controlled initial import of real IBDAA products, images and prices

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
