# IBDAA Atelier — Multiple POS Billing and Blocker Audit
Date: 11 November 2026
Branch: `development`
Scope: SRS v1 sections 3–10, 14–18 and 23; the full 23-section SRS has been read. Reports work is deferred at the owner's request.

## Audit scope

The in-scope application is a **single shop with multiple cashier accounts**, not the older project's multibranch or multiple-till/terminal setup. Examine concurrent customer/order saves, two distinct bills, repeated taps/retries, 1–30 line items, per-item measurements, product/alteration selection, full/partial/split payments, invoice preview and shared cash-register closing. Server assertions below are code/schema findings, **not evidence that simultaneous SQL transactions were exercised**.

### Implemented fixes in this audit

| Area | Before | Scoped change |
| --- | --- | --- |
| Alteration lookup | Only the first 24 active services displayed; no way to reach later services | Server-side name search and 24/page navigation, keeping Alterations panel selected |
| Payment validation | Each method checked individually, but cash + KNET total could exceed due; server then rejected | Show inline validation before posting when sum is zero or exceeds balance; server decimal validation unchanged |
| Multi-item billing | API limit 30 order lines but UI could keep adding without explanation | Warn before attempting the 31st line; existing service/API limit unchanged |
| Invoice | `File(pdf, "application/pdf", fileName)` forced an attachment/download | Serve the persisted PDF inline so the `target="_blank"` A4/80mm actions open browser preview; no RDLC changes |
| Cashier sign-in | Cashier was sent to Home landing page after login | Cashier to POS, Admin to dashboard; safe local return URL retains priority |

### Safeguards found in code (need runtime proof)

- `Orders`, `OrderRequests`, `PaymentRequests`, `PaymentBatches` use numeric SQL identity IDs and unique request constraints.
- Each order request is owned by the authenticated user. A same-payload retry replays the saved result; changed payload is refused.
- Order line builder uses currently active product/service, validated product-specific size and per-order-item measurement snapshots. Server recalculates authoritative KWD prices.
- Payment saving locks an order under serializable isolation, rechecks the remaining balance, records split rows in one transaction and guards duplicate request IDs.
- Register row is locked before payment/refund; register closing uses that same session row lock and reconciles cash after receipts/refunds.
- Only one shop-wide register session can be open. This is deliberate for single-shop v1, not proof of support for several physical tills.
- Historical invoices use persisted items/measurements and shop-branding snapshot rather than a live product card.

### Still open — must not be called production-ready

**P0 runtime verification required**
1. Two cashier accounts in separate browsers create separate orders simultaneously (same and different customer); two unique monotonic order IDs, no cross-cart state.
2. Two devices pay one order concurrently using distinct numeric payment request IDs; combined net paid must not exceed total, no double ledger posting or deadlock.
3. One device retries an order or payment after the server commits but before the browser receives a response; no duplicates, clear recovery screen.
4. Concurrent payment/refund while Admin/Cashier closes the shared cash register; no late postings to closed sessions, no missing cash or balance.
5. Print and reprint the same bilingual A4 and 80mm invoice on real devices; verify Arabic wrapping, measurements, logo, and exact margins.
6. Authenticated browser tests at 1366x768 and tablet/mobile sizes; 10 cart lines, customer changes, back/refresh, keyboard and Arabic RTL.

**Functional gaps / pending decisions**
- Separate physical tills/drawers are **not** supported by current single open-register schema. SRS §23 asks for confirmation; do not add multibranch/terminal structures without that decision.
- POS draft and reserved order request are in tab-local `sessionStorage`. A browser's *Duplicate Tab* action can copy the same pending request ID if a save is already in progress; SQL rejects conflicting reuse, but a friendly recovery workflow is not yet implemented. Test this edge.
- Cashier must save an unpaid order before opening separate Payment page. Next design pass may reduce navigation without combining unsafe writes.
- Cashier discounts/charges and audited post-confirmation edits from SRS §§7.5 and 8.3 are not implemented in current billing contracts. Do not fake totals in UI.
- A closed register may prevent subsequent payments when opening is required; test friendly cashier guidance/reopening.
- Real website catalogue, source images/prices, cash-drawer hardware, release browser/performance evidence are not yet verified.
- Existing 30-line maximum is a deliberate current server limit; larger single-customer bulk sales need a separate agreed requirement before changing DB/API limits.
- Additional configurable service categories / category filters remain for a later UI slice; search and paging now make all active services reachable.
- Reports explicitly deferred.

### Required real SQL/browser scenario matrix

| ID | Setup / action | Pass condition |
| --- | --- | --- |
| MB-01 | Cashiers A+B save separate bills at once | Different OrderId, RequestId, matching customers |
| MB-02 | Cashiers A+B bill the same customer at once | Both orders valid; item snapshots independent |
| MB-03 | One bill: multiple products with differing measurements and alterations | Each item retains its measurements, totals match |
| MB-04 | Double-click and response-loss retry on Save | One order; same request fingerprint/ID |
| MB-05 | Duplicate tab while Save is pending, modify second cart | No silent overwrites; recoverable conflict |
| MB-06 | Cashiers A+B submit full/split payments on same order | Net paid <= total; exactly-once batches |
| MB-07 | Cashier A collects while Admin refunds | Ledger consistent; authorized rows only |
| MB-08 | Cashier submits payment as register closes | Consistent session assignment or explicit reject |
| MB-09 | Cash + KNET + change | Paid totals exclude returned cash |
| MB-10 | View invoice and reprint at 80mm/A4 | Persisted invoice, correct Arabic and measurements |

## Verification and release rule

Added focused source-contract tests (`tests/pos-multi-billing-contract.test.mjs`), which inspect SQL/ASP.NET/JavaScript safeguards and the new UI links. They **cannot substitute** for running MB-01–MB-10 against isolated SQL and authenticated browsers. No database migrations, Reports edits, production website/data or `main` changes were included.
