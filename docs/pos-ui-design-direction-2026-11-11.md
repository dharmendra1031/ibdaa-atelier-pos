# IBDAA Atelier — Agreed UI/UX Direction (11 November 2026)

**Status:** Visual direction approved for future implementation; reference mockups are not proof of working features.
**Scope:** `development` branch only. Follow `IBDAA-Atelier-SRS-2026-11-10.md` and `ui-ux-performance-standards.md` for functional requirements. This note supersedes the proposed green palette for the next design pass, subject to final brand asset verification.

## Look and feel
- Premium and spacious Moroccan atelier identity: deep burgundy/plum, warm cream/white, restrained muted-gold accents.
- Use consistent garment photography, readable English/Arabic labels, proper RTL support and KWD with exactly three decimals.
- Avoid crowded cards, unnecessary text, nested navigation and oversized decorative elements. Clear hierarchy, generous whitespace, low-friction shop-counter operation.
- Prefer a compact top navigation in the dedicated POS experience; avoid a permanent sidebar eating into the product area.
- Existing ASP.NET Core 10 MVC/Razor, local Bootstrap 5.3 and scoped plain JavaScript remain the technology baseline. No new frontend framework.

## POS main working layout
- Exactly **two persistent areas**: left ~60–65% product/service catalogue, right ~35–40% customer/cart/checkout. Tablet stacks them appropriately.
- Left: product/service switch, search, category chips, photo grid with bilingual names and verified price; show active products only.
- Right: customer selection, required delivery date, cart with image, product/service distinction, item-specific size and measurements, quantity, tailoring notes, item edit/remove, discounts/charges, total, paid, balance and clear Save/Payment action.
- Customer-first validation must remain in both UI and backend.
- Product tap opens an uncluttered details modal: gallery, product sizes, quantity, **admin-configured measurements per order item**, required validation, notes, Add to Cart. Do not spread measurement inputs across the main page.
- Alteration services are separate entities and can be attached to the order with appropriate notes/price permissions.

## Full UI flow and complementary screens
1. Existing role-based employee login (single shop in v1; no branch/POS terminal picker unless SRS changes).
2. Customer search/create, bilingual names, mobile lookup and previous history.
3. Product catalogue and product details/measurements.
4. Cart review, alterations, delivery date, discounts and totals.
5. Save order / full, unpaid, advance, partial and split payments; stage payment rows distinctly from persisted entries; handle cash tender/change and safe retries.
6. Print/reprint bilingual **A4 and 80mm** invoices from saved snapshots, including measurements.
7. Orders list, status history, pending/ready/delivered and remaining balance collection.
8. Daily cash collection, register opening/closing and reconciliation.
9. Reports, product/category/gallery/size/service masters, users/permissions and settings should share the same visual system, with role-appropriate controls.

## Scope/quality constraints
- Do **not** copy extra features suggested by image-generation mockups: workshop production, payroll, multi-branch, POS terminal selection, WAMD as a required method, A5 printing, or ecommerce storefront are not established by this SRS.
- Product imagery, product names and sample KD amounts in concept images are **illustrative** until verified against the client's approved catalogue; never seed fabricated prices or claim an imported catalogue.
- Preserve business logic, numeric SQL IDENTITY keys, historical invoice snapshots, audit trails, permissions and transaction safety. Redesign layout incrementally; don't replace validated logic just to imitate a picture.
- Verify finished implementation with real SQL/browser LTR+RTL tests and physical A4/80mm invoice QA.
