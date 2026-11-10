# Architecture and boundaries

The [10 November 2026 SRS](IBDAA-Atelier-SRS-2026-11-10.md) defines this new application. The [master delivery and quality plan](master-delivery-quality-plan.md) captures implementation dependencies, old-project regression lessons and release gates. Before feature work, check hosting and align the current .NET 8 scaffold with the SRS-recommended .NET 10 LTS. The original document remains the canonical reference.

## Source of truth

- The existing live website `ibdaaatelier.com` remains unchanged. Its catalogue is the source for a later, validated import, not for automatic production writes.
- A Product is a sellable garment with bilingual names, pricing, photos, sizes and measurement definitions.
- An Alteration Service is a separate operation. Do not hide Products inside a ServiceId merely to reuse old code.
- A future Order Item must snapshot the selected product/service, unit price, size and entered measurements. Subsequent catalogue changes cannot rewrite historical invoices.
- Payments, refunds and register reconciliation belong to their own records and must use transaction-safe retry protection.

## Initial catalogue schema

`ProductCategories`, `Products`, `ProductImages`, `ProductSizes`, `MeasurementDefinitions` and `SizeChartValues`, each with an integer SQL `IDENTITY(1,1)` primary key. ProductCode is computed from ProductId. Images have one optional primary image and unlimited gallery entries. Configurable measurement definitions apply to either a category or a product.

The initial catalogue view is **read-only**. Authenticated write APIs, secure image handling, website import and POS sales are intentionally not present yet. When adding write operations, validate that each size-chart measurement belongs to the correct product or category; FKs alone cannot enforce that relationship.

## Security and deployment

- No SQL migration or external HTTP sync runs on application startup.
- Require a backed-up, explicitly selected **test** database before applying `database/001_initial_catalog.sql`.
- Store credentials only in local user-secrets or a managed secret store.
- For uploaded images, validate actual file content and store only controlled local/object-storage paths.
- Do not expose administrative create/edit endpoints before authentication and authorization.

## Legacy Tailor reference

The prior `dharmendra1031/tailor` project was consulted for numeric request IDs, safe retries, order/receipt snapshots, customer-first billing, permission guards and SQL transaction handling. No legacy workshop, payroll, salary or multi-branch logic was copied. This codebase deliberately starts with a smaller, focused domain.
