# IBDAA Atelier POS — SRS-first Cashier UX Audit

**Date:** 11 November 2026  
**Branch:** `development`  
**References:** `docs/IBDAA-Atelier-SRS-2026-11-10.md` (all 23 sections), `docs/master-delivery-quality-plan.md`, `docs/pos-ui-design-direction-2026-11-11.md`, old Tailor/Mashgal POS release checklist (reference only).

**Rule for each next task:** Read the full current SRS and UI direction, then inspect the existing flow/source and related acceptance tests before touching implementation. If an action cannot be justified by an SRS requirement, do not add it. Prefer fewer steps while keeping accounting, permissions, snapshots and audit on the server.

## Cashier flow: observed problems and changes

| SRS | Before | Change in this slice | Effect |
| --- | --- | --- | --- |
| 6.1–6.3, 7.4 | New customer linked to a separate Create page; after save, return to POS and search again | Quick-create dialog within POS; uses the same customer form validation, mobile normalization, duplicate handling and audit; automatically selects the saved customer | Avoids leaving POS and re-searching |
| 7.1–7.2 | Category dropdown and an extra Search submit | One-click category chips, with separate product search | One fewer action for category choice |
| 7.6 | Product category/search reload restores cart but resets delivery date | Persist selected delivery date with the current session cart, respecting minimum date | Avoids repeated date input |
| 7.2–7.3, 4.3 | Product modal did not display gallery despite product images being stored | Display product gallery with thumbnails above size, dynamic measurements and notes | Keeps selection on one focused dialog |
| 7.5, 9.1, 10 | After Save Unpaid, cashier had to open Order Details before taking payment | Direct **Take payment** link after saved order; separate **View order / Print** link | Removes one intermediate navigation; existing safe payment endpoint unchanged |
| 7.1, 17.3 | Global admin sidebar consumes ~214px even during billing | Scoped full-width POS layout with two working areas, quick links, compact burgundy/cream styling | More room for product images and readable cart |

**No new POS terminal/branch chooser, workshop, salary, inventory ledger, fabricated catalogue items, or fake prices were added.** Previous project was used only to cross-check low-click cashier patterns and regression risks, not to copy unrelated modules.

## Intended operating path

1. Login as authorized cashier/admin. Select existing customer by name/mobile **or** create a bilingual customer in the POS dialog.
2. Choose Products or Alterations. Tap a category chip or search products.
3. Tap a real active product; inspect image gallery, choose size, fill configured mandatory per-item measurements/notes and add to cart.
4. Set required delivery date; review cart and total. Preserve cart and date when navigating categories.
5. Save an unpaid order safely (DB numeric request ID, server pricing and duplicate-retry protection remain unchanged).
6. If collecting money, go **directly** to existing Payment screen to take full/advance/partial/split payment. After saving, the existing Order Details page provides persisted A4/80mm invoice/receipt links; alternatively keep the order unpaid.
7. Continue from Orders & Delivery for later balance, status and reprint. Cash Register open/close remains separate as required by permissions/settings.

## Remaining UX and SRS gaps — not delivered in this slice

- A genuinely integrated one-screen Save → payment modal → Save & Print experience still needs a transaction-safe design and dedicated authenticated SQL/browser tests. Do not pretend staging money is saved.
- The payment form is still a separate page, and print requires the order page. This slice removed an intermediate step but **did not** combine order and payment writes.
- SRS 7.5 and 8.3 require permission-controlled discount/charge/confirmed-order edits; current UI/endpoint coverage must be checked before implementing. No speculative financial changes.
- Product gallery/source content must be verified against an authorized client catalogue import before real product images/prices can be shown.
- Review customer-history entry, role restrictions, Arabic long-label screens, mobile/tablet layouts, 1366×768 cart footer visibility and accessibility on a **real browser**.
- Reports are **deferred at user request**; no changes to Reports files in this slice.
- All 19 SRS end-to-end cases, physical printer testing, and production database validation remain separate release gates. Source contract tests are **not** browser/SQL acceptance evidence.

## Risk/test checklist for this slice

- New customer English/Arabic/mobile required; invalid and duplicate mobile show inline error; POS customer selected only after server confirms save.
- New customer cannot silently reassign an existing cart. Guard customer changes and verify cart reset.
- Saved customer is a numeric auto-increment row; creation and audit succeed together or both roll back.
- Category search, pagination and reload must not lose unsaved cart/delivery date.
- Two different products keep different sizes/measurements and saved snapshot values.
- Full/partial/split payments, cash change, duplicate submit and server-side permission checks remain unchanged.
- Direct payment link only appears after a confirmed saved order. Order number and receipt come from persisted data.
- English and Arabic RTL, desktop/tablet/mobile, gallery upload fallback, hardware print and logged-in browser flows need real QA.

## Files touched in implementation
- `Controllers/CustomersController.cs` — validated quick-create endpoint, shared new-customer saving.
- `Controllers/PosController.cs` — product images in product details response.
- `Views/Pos/Index.cshtml` — quick-create dialog, category chips, direct payment/print links.
- `wwwroot/js/pos.js` — quick-create selection, gallery, delivery-date persistence and payment navigation.
- `Views/Shared/_Layout.cshtml`, `wwwroot/css/site.css` — scoped POS workspace and colors.
- `tests/pos-fast-flow-contract.test.mjs` — source contract checks.

**No SQL schema migration, report implementation, main-branch change, old-repository write or client website write in this UX slice.** Real authenticated browser/DB acceptance still required before claiming production readiness.
