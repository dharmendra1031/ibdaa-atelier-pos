# IBDAA Atelier — Master Delivery and Quality Plan

**Version:** 1.0 · **Date:** 10 November 2026 · **Status:** Planning baseline, not implementation acceptance  
**Working branch:** `development` · **Release branch:** `main`  
**Source of truth:** [IBDAA SRS (23 sections)](IBDAA-Atelier-SRS-2026-11-10.md) · [Original Google Doc](https://docs.google.com/document/d/1OWvb0xnisZzJTEOdGS1sY3PHn_WSn6XmysiM0PtCnok/edit)  
**UI rules:** [Premium Bootstrap / performance standard](ui-ux-performance-standards.md) · **Domain boundaries:** [Architecture notes](architecture.md)

## 1. Mission and hard boundaries

Build a **new**, lightweight IBDAA Atelier shop management and POS system. Use the old `dharmendra1031/tailor` code and previous project discussions as a **reference for tested lessons**, not as a base to copy wholesale.

- Implement **only** SRS v1: administrator, products/categories/gallery, sizes/measurements, alterations, customers, POS orders, payments/refunds, deliveries, cash-register closing, bilingual invoices, reports, roles/settings/audit, verified one-time website catalogue import.
- **Out of scope** unless requirements change: workshop production/assignment, piece-rate salary/payroll, supervisor QC, multibranch consolidation, inventory ledger, loyalty, WhatsApp/SMS, offline POS, customer ecommerce storefront or automatic website order sync. The existing ecommerce website remains live and unchanged.
- Single shop in v1; multiple cashier users and their collection reports are in scope. **Do not import** old branch-specific tables, `StoreCode` compatibility logic or terminal-selection screens without a confirmed requirement.
- Every persisted primary key and transactional request key is **numeric SQL `IDENTITY(1,1)`**, with clear `int`/`bigint` types; never GUID/UUID keys. A human-readable invoice number is separate from its immutable numeric row ID.
- Minimal readable developer-style changes. Before each feature, inspect relevant existing code, remove proven dead/duplicate code in the same scoped change, and avoid speculative abstractions. No generic repositories/unit-of-work wrappers, frontend state framework, redundant CSS themes, dynamic SQL builders or duplicate masters.
- **No changes to** the old Tailor repository, the client's live website, live orders or production SQL data. No EF `EnsureCreated`/`Migrate` at startup. Database scripts are explicit, reviewed, guarded and tested against an isolated database.

## 2. Technology decision: resolve before new UI/features

**Mismatch found:** the SRS recommends **ASP.NET Core 10**, while the initial scaffold currently targets `net8.0` and EF Core 8. Microsoft lists .NET 8 support ending **10 November 2026** and .NET 10 LTS supported until **14 November 2028**.

**Recommended baseline (gate T0):**
- .NET **10 LTS** / ASP.NET Core MVC + Razor; align EF Core/SQL Server package versions after checking compatibility, SDK installation and actual hosting.
- SQL Server 2019+ if the approved target supports it; confirm database collation/Arabic and backup/restore.
- Bootstrap **5.3.8** pinned, self-hosted CSS and only Bootstrap JS features actually used. Modest scoped custom CSS; plain JS modules; no jQuery or dashboard template by default.
- Secure cookie authentication with established .NET password hashing / protection libraries; numeric `Users` and `Roles` keys. Do not write custom cryptography.
- KWD monetary calculations in server-side `decimal(18,3)` with invariant request parsing and explicit rounding. UTC event timestamps; Kuwait local time for display; **delivery dates are calendar dates, not JS ISO timestamps**.
- RDLC invoice rendering is a preferred legacy **reference**, not a compatibility assumption. Prove the selected report renderer works with .NET 10 and Arabic/A4/80mm **before committing to it**; use a supported server-side PDF renderer otherwise.

**Gate T0 is not passed yet.** The tiny scaffold now targets .NET 10 / EF Core 10.0.14; verify host SDK/runtime and a real build before declaring Gate T0 passed. Do not build transactional features before that check.

## 3. Lessons from the earlier project — guards from day one

| Observed mistake / recurring issue | Root cause to prevent | Acceptance guard |
| --- | --- | --- |
| Adding cart items without a customer | Order prerequisites split across UI handlers | Customer-first guard in UI **and** API; state reset when customer changes |
| Save, split pay or retry creates confusion/duplicates | Multiple save paths and unscoped request identity | One confirmed write operation; DB unique numeric request ID; concurrent/response-loss retry tests |
| "Add to list" treated as saved payment | Unsaved staged entries not distinguished from ledger | Stage has no DB write; **Save & Print** posts selected entries atomically and indicates persisted status |
| Partial payment disallowed when full amount not paid | Advance policy accidentally coupled to split policy | Independent settings/validation; unpaid, partial, full and split scenarios |
| Discount after payment changes totals incorrectly | Mixing original amounts, edits and later adjustments | Original snapshots + audited adjustments; test overpayment/refund/balance invariants |
| Cash tender larger than due produces wrong paid amount | Change confused with payment | `received cash` distinct from `posted payment`; refund cash change and keep paid <= due |
| Invoice whitespace, giant print or Arabic text clipping | Template and physical printer not tested together | A4 and **80mm** physical print QA, bilingual long text, exact page margins, no runtime layout hacks |
| Company uploaded logo missing on receipt | Different image source between UI and print | One canonical logo source; test replaced logo in preview and persisted PDF |
| Service/category images differ between POS and Admin | Different fallback and upload paths | One authoritative product/image mapping, stable primary gallery image and image fallback tests |
| Wrong user sees delivery/refund actions | UI hides buttons but backend permission not checked | Matrix of role permissions with direct API 403 tests; no stale form actions |
| Old async API response repaints wrong order | Fetch state not invalidated on new selection | Abort/discard stale requests; loading, failure, empty and rapid-switch tests |
| Invoice number reappears after data deletion | Calculating next number with `MAX + 1` | Monotonic SQL counter/sequence, no reuse, unique index, concurrent save test |
| Branch code/string mismatch, unnecessary screens | Adapting old multi-branch design into single-shop POS | Use numeric IDs directly; no branch code compatibility layer in v1 |
| A source test passes but browser/DB flow fails | Tests don't prove runtime and devices | Require build + isolated DB + logged-in browser + real print evidence for relevant features |

This list is derived from the project's previous issue discussions and documented release audits. It is a **regression checklist**, not a claim those defects exist in this new codebase.

## 4. Fixed domain model and transaction boundaries

### 4.1 Masters

- `ProductCategories` (parent optional), `Products`, `ProductImages` (one primary; 4+ gallery images supported), `ProductSizes`, `MeasurementDefinitions`, `SizeChartValues`.
- `ServiceCategories` and `Services` are separate from products; no fake product mapped into `ServiceId`.
- `Customers` with bilingual names and searchable mobile; duplicates by normalized mobile handled according to confirmed policy. `Users`, `Roles`, `RolePermissions`, `Settings`, `AuditLogs`.
- Website source mapping stored as **source metadata** on products/categories/images, not as their local primary keys; validate and dedupe external references without inventing inventory.

### 4.2 Orders

- `Orders` header, `OrderItems`, `OrderItemMeasurements`, `OrderStatusHistory`, immutable `InvoiceRecords`/snapshots as needed.
- Order line has `ItemType = Product | Service`, `ProductId` or `ServiceId` **exactly one** (SQL CHECK), and always stores name/SKU/category, unit price, discounts, size and tailoring-note snapshots. Individual measurement rows carry label, unit, actual value, display order and source definition.
- A product can have optional alterations as separate clearly linked order services; don't turn product into a service. Model relation only where required by the SRS.
- Required delivery date verified on server. `New → Confirmed → In Processing → Ready for Delivery → Delivered`; `Cancelled` requires reason/permission. Record actor/time/old+new status, and block invalid transitions. Paid cancellation follows refund policy, never silently deletes payment.
- Order edit after confirmation is **audited**, uses optimistic concurrency and explicit allowed fields. Product price/name changes never mutate saved invoices. A historical invoice reprints from persisted snapshots, not current catalogue rows.

### 4.3 Money, printing, cash closing

- `OrderPayments`, `PaymentMethods`, `PaymentRefunds`, `RegisterSessions`, `RegisterTransactions`; transaction records are append-only except explicit controlled reversal/correction records.
- `total = gross - item discount - approved order discount + approved charges`; `net paid = payments - refunds`; `balance = total - net paid`; no negative balance without an explicit documented credit/refund rule.
- Numeric `RequestIds` issued by DB and scoped to actor/action/order; `UNIQUE` constraints + transaction locks. Duplicate identical requests return the saved result; conflicting retry payload returns conflict. Each split-payment submission is **one atomic transaction**.
- Separate **UNPAID save**, **PAID/advance/split collection**, **print/reprint** states. "Add payment" in a dialog stages a list only. Once committed, show persisted amount and issue receipt from saved data. Payment method restrictions enforced on server.
- Record cash tendered, change returned and actual posted cash as separate values. Don't post received cash in excess of balance as revenue.
- Invoice/order public numbers have explicit prefix and a **monotonic counter** under transaction with uniqueness; never use `MAX + 1`, even after cancelling/testing/deletion.
- Register session uses one clear open/close state, opening cash, allowed expenses/refunds and counted closing; one close per session with audit and retry protection. Closed session postings require explicit business rule and server enforcement.
- A4 and 80mm templates, full bilingual item measurements, receipt company branding, controlled printing and accurate white-space/margins.

## 5. UI contract before page implementation

See [UI standards](ui-ux-performance-standards.md). Screen implementations must keep the same navigation, forms, error patterns and visual scale.

**POS = two permanent areas:** left product/service browsing (~60–65%); right customer, delivery, cart, totals, Save/Pay. Never three permanent panels. Customer must be selected before adding any billable line. Product modal handles gallery, size, dynamic measurements and instructions. Amount buttons remain visible with 3–10 cart lines at 1366×768. On phones stack areas or use one Bootstrap offcanvas without introducing a third workflow.

**Admin:** stable sidebar, dashboard KPIs, compact master list/form, separate settings Save areas, data table paging. Unavailable features are not shown as working actions. Icons concise, touch targets >=44px. No feature-only controls that don't work.

**Language and assets:** LTR/RTL support on every screen; Arabic strings measured for clipping. KWD 0.000 throughout; Arabic labels and body fields remain editable. Use actual verified product images and category association, lazy-loading/thumb variants; do not show invented product records or prices.

**State handling:** Loading/error/empty/success, slow network, double-click, stale responses, lost request after server commit, long names, wrong permissions, navigation/back and browser refresh must be explicitly handled.

## 6. Implementation order — one independently testable slice at a time

| Gate | Slice and exact deliverable | Tests and evidence required before next slice |
| --- | --- | --- |
| P0 | **Foundation alignment**: confirm .NET 10/hosting; upgrade scaffold, fix package versions; self-host Bootstrap 5.3.8; common layout/navigation/tokens | Clean restore/build, local home load, no external CSS/JS, desktop/tablet/mobile + RTL screenshots |
| P1 | **Authentication + roles**: admin and POS user, numeric IDs, permissions, logout, antiforgery | Valid/wrong/disabled login, session expiry, cross-role API denies, CSRF denies |
| P2 | **Product categories + service categories**: CRUD, bilingual fields, status and display order | Valid/edit/duplicate/inactive, searchable/paged list, zero invented records |
| P3 | **Product master + gallery**: prices, SKU, 4+ images, primary, active status | Upload/view/replace/remove, content/mime/size checks, admin/POS same primary photo, historical safety |
| P4 | **Sizes + dynamic measurement definitions**: product/category scope, chart values, validation | Two products with different required fields, missing/invalid values rejected, save/read match |
| P5 | **Existing site catalogue initial import**: verified source export/API, dry-run diff, authorized manual import | Counts/IDs/images/prices/sizes matched; dedupe and rerun safe; no live site writes |
| P6 | **Customers + services**: customer search/add/history and alteration masters | Correct mobile/name/Arabic search, no accidental duplicates, service and product stay distinct |
| P7 | **POS cart and unpaid order save**: two-area UI, item measurements, delivery and snapshots | 1/10 products, two different measurements, customer guard, server price verification, one order on retry |
| P8 | **Payment + discount + refund**: unpaid/paid, advance, split, cash change, permission checks | Calculation table, partial/split retry, overpayment, extra discount, refund, authorization, no double post |
| P9 | **Status + delivery + invoice**: authorized state transitions and A4/80mm persisted invoice | Wrong transitions blocked, history logged, printed bilingual measurements/logo, physical printer reprint |
| P10 | **Daily register/collection + reports**: open/close, variance, sales/product/customer/collection filters, exports | Ledger reconciles to report, date edges, retries, closed-day rejects, filter changes don't show stale rows |
| P11 | **Final regression, performance, security and release** | Full acceptance matrix below, migration/restore drill, screenshot QA, physical printing, stakeholder sign-off |

**Per-slice rule:** inspect existing code → confirm SRS contract & data shape → implement **minimal** schema/API/UI → unit/integration tests → responsive/Arabic browser check → commit to `development` with test evidence. No parallel half-finished modules; no "looks fine" approval on untested source.

**Blocked/deferred items** must remain marked blocked/deferred; never silently fake values, mock production totals or call an unfinished feature complete.

## 7. SRS traceability — 19 acceptance cases

The SRS section 18 contains **19** scenarios. The final acceptance report must record all of them with test ID, database, screenshot/order reference and pass/fail:

| ID | SRS acceptance | Main slice |
| --- | --- | --- |
| AT-01 | Create category | P2 |
| AT-02 | Create product | P3 |
| AT-03 | Upload four product images | P3 |
| AT-04 | Set primary image | P3 |
| AT-05 | Deactivate product; historical records stay readable | P3/P7 |
| AT-06 | Select product and size | P4/P7 |
| AT-07 | Measurements persist on correct order item | P4/P7 |
| AT-08 | Multiple items keep different measurements | P7 |
| AT-09 | Create/search customer | P6 |
| AT-10 | Bilingual customer names | P6 |
| AT-11 | Add alteration service to order | P6/P7 |
| AT-12 | Delivery date saved and searchable | P7/P9 |
| AT-13 | Partial/split payment balances | P8 |
| AT-14 | Print bilingual invoice with correct product/size/measurements | P9 |
| AT-15 | Daily collection payment-method totals | P10 |
| AT-16 | Cash closing expected vs actual vs difference | P10 |
| AT-17 | Sales report date-range and filters | P10 |
| AT-18 | Status change history records actor and time | P9 |
| AT-19 | Previous invoice reprint retains original values | P9 |

**Additional non-negotiable regression pack:** role privilege escalation; unknown/inactive product; SQL and HTML injection; two simultaneous Save clicks; network retry after successful DB commit; payment/rate concurrency; payment above balance and cash change; refund beyond paid; empty customer/cart; decimal input with 0/3/4 decimals; Arabic long-name RTL; date at Kuwait midnight; 10 cart rows at 1366×768; primary-image fallback in both screens; disabled payment methods; cancelled/closed register; print same invoice after catalogue price changes; responsive phone and tablet; restore database and uploaded files together.

## 8. Financial and UI test data (isolated database only)

Use test fixtures including:
- Product A: price **25.000 KD**, size M, chest **102 cm**; Product B: price **12.500 KD**, custom size and different required measurements. Values are **test-only**, never seed them into a client's actual catalogue.
- Order total 25.000 KD, Cash 10.000 + KNET 10.000 => balance **5.000 KD**.
- Cash tendered 30.000 against 25.000 due => posted payment **25.000**, change **5.000**.
- Duplicate request ID same payload returns identical order/payment; changed payload returns conflict with no additional row.
- Discount 2.000 on 25.000 before payment => 23.000 due; post-payment changes require explicit audited adjustment and safety check.
- Multiple products with same size label but different measurement definitions remain distinct in DB and invoice.
- Deliberately long Arabic product, staff, customer and measurement labels for screen and receipt tests.

Fixtures use only the isolated test database and are not imported from, or written to, the live website.

## 9. Release gates and measurable quality

A feature is **Done** only when the following are recorded:

1. SRS reference and expected behavior are known; no unresolved high-risk decision affects that feature.
2. UI works in empty/loading/error/filled states at **1440×900**, **1366×768**, **1024×768**, **768×1024** and **390×844**, in LTR and RTL where relevant.
3. Server enforces role, active status, price, validation, concurrency and idempotency—not only JS.
4. SQL migration has a protected test database guard, matching verification query, indexes/FKs, data preservation and rollback/backup explanation.
5. Relevant unit tests, authenticated integration tests and browser tests pass; screenshot/print evidence saved for UI/printing.
6. No unreachable action, duplicate script/CSS, dead code, hidden hard-coded IDs, GUID IDs, fake successful API state or unneeded package.
7. Code review checks naming, simple flow, no N+1 catalogue reads, parameterized SQL and correct disposal/async cancellation.
8. Feature is committed to `development` with exact SHA and test evidence; no production changes.

**Performance goals** (measured on target hardware, not promised outcomes): LCP <=2.5s p75, INP <=200ms p75, CLS <=0.1; locally served version-pinned assets; optimized image thumbnails; paged product searches; avoid repeated API requests; limited custom JavaScript and CSS. Prioritize fast cashier interactions over visual animation.

**Production NO-GO until:** authenticated role matrix passed, exact totals and refunds reconciled, schema verification/restore rehearsed, real bilingual A4/80mm print verified on target printer, website import reviewed against source, and owner accepts critical workflows. No branch merge to `main` without this record.

## 10. Decisions to close as their gate approaches

Only these SRS uncertainties need client/hosting confirmation. Keep reasonable safe defaults to advance independent tasks; do not block writing unrelated UI documentation:

| Decision | Default until confirmed | Required by |
| --- | --- | --- |
| Host runtime & SQL Server version | Prefer .NET 10 LTS; isolated SQL Server 2019+ | **P0 — blocking** |
| Website integration access | One-time approved read-only export/API; no sync loop | P5 |
| Product inventory vs made-to-order | No stock deduction or stock promise | P3/P7 |
| Shop identity/logo/address and approved print template | Placeholders only in development; real values for release | P9 |
| Refund/discount/cash expense approval limits | Admin-only financial overrides; no default cashier bypass | P8 |
| Register opening cash mandatory | Configurable, default on for test flows | P10 |
| Reusable measurement profiles | Save each order item's snapshot first; reuse later if approved | P7 |
| Multiple delivery dates/partial deliveries | One mandatory due date per order | P7/P9 |
| Arabic name auto-fill | Manual editable Arabic; no automatic transliteration service | P6 |
| A4/80mm physical printer compatibility | Two saved-invoice print layouts; verify hardware and renderer | P9 |
| Offline, branches, multiple tills, tax rules | Not implemented unless explicitly approved | Before any related work |

No unauthorized assumptions about actual IBDAA prices, products, discounts, stock or credentials.

## 11. Operational process and reporting

- **Before code:** check GitHub `development` head, read related SRS/plan and existing classes, state what files change and why. Do not recreate old classes just because the older project had them.
- **During:** work in one vertical slice; keep API/database/JS naming straightforward; fix the original cause, not just hide the symptom.
- **Before push:** run build, focused tests and static/security checks; run SQL/browser tests when environment is available. Clearly label unavailable tests as **not run**, not passed.
- **After push:** verify SHA and changed files from GitHub; report done/pending, tests passed/not run, DB change yes/no, main unchanged, remaining blocker.
- **Periodic cleanup:** remove confirmed dead or duplicated code with tests. Don't refactor unrelated working modules merely for style.
- **Privacy:** repository currently appears public. Before adding real client assets, customer data, API integration credentials or screenshots, make it **private** and keep secrets out of Git.
- **One source of truth:** SRS = business behavior; this document = release execution gates; UI standards = visual rules. Keep short roadmap linked to this plan and avoid competing duplicate plans.

### Current verified state

New repository, `main`/`development`, SRS snapshot, starter MVC/catalogue files and manual SQL catalogue skeleton exist. P0 source now targets `net10.0` / EF Core 10.0.14 and includes locally served Bootstrap 5.3.8 with a responsive English/Arabic shell. **Successful .NET build, hosting compatibility and browser/RTL QA are still unverified.** Authentication, website import, transactional POS, database/browser tests and printer QA are not yet completed. This document is a plan, not proof that the product is production-ready.
