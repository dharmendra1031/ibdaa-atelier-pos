# IBDAA Atelier — UI/UX and Performance Standards

**Status:** Design baseline for implementation  
**Date:** 10 November 2026  
**Branch:** `development`  
**Source of requirements:** [IBDAA Atelier SRS (10 November 2026)](IBDAA-Atelier-SRS-2026-11-10.md)  
**Original SRS:** https://docs.google.com/document/d/1OWvb0xnisZzJTEOdGS1sY3PHn_WSn6XmysiM0PtCnok/edit

This document defines **how the existing SRS will be presented and implemented**, not additional business requirements. The client's live `ibdaaatelier.com` website is a catalogue/brand reference. Do not modify it, scrape it as part of application startup, or assume a given price/image/size without verification.

## 1. Product goals

1. Premium, calm, elegant interface suitable for an atelier **and** fast shop-counter operations.
2. Bootstrap 5.3 as the UI foundation; small project-owned CSS for branding only.
3. Fast, responsive pages on ordinary shop PCs and tablets; mobile-friendly for management.
4. Customer-first POS with large product images and obvious next actions.
5. Full English/Arabic support, including right-to-left (RTL) layout and KWD values to **three decimal places**.
6. Reuse existing, tested *ideas* from the old Tailor project, not its large UI bundles, legacy modules or database.

**Do not build:** Workshop payroll, piece-rate salary, supervisor production approval, cross-branch consolidation, loyalty, customer mobile app, or other SRS future enhancements in this phase.

## 2. UI technology rules

| Area | Decision |
| --- | --- |
| Server UI | ASP.NET Core 10 LTS MVC/Razor (P0 scaffold target; hosting/build unverified) |
| UI framework | Bootstrap 5.3.8, self-hosted under `wwwroot/lib/bootstrap` |
| Assets | Serve Bootstrap and app assets locally; no runtime CDN dependency |
| Icons | Small inline SVG icons; avoid loading a whole icon-font library |
| Interactions | Plain JavaScript modules; Bootstrap bundle only where modal/offcanvas is used |
| Charts | Start with KPIs and simple tables; add one lightweight chart only when needed |
| Data | SQL Server and EF Core for the catalogue; server-side authorization/validation |
| Fonts | System font stack; Arabic-capable system fallbacks; no mandatory external webfonts |
| Layout | Shared Razor layout, a small set of view partials, one scoped stylesheet per complex page if justified |
| IDs | Numeric SQL `IDENTITY(1,1)`; never UUID/GUID |
| Dependencies | No jQuery, Select2, Highcharts, heavyweight dashboard theme, or general-purpose UI state framework by default |

Prefer existing Bootstrap controls (`container-fluid`, `row`, `col`, `card`, `table-responsive`, `modal`, `offcanvas`, `form-control`) over custom copies of them. Avoid a second parallel component library.

## 3. Visual language

These are **proposed design tokens**, not a claim that they exactly match the live website. Before final visual sign-off, compare them with the client's approved logo and brand assets.

| Token | Proposed value | Usage |
| --- | --- | --- |
| Primary | `#154C45` | Headers, primary buttons, active navigation |
| Background | `#F8F7F3` | Warm off-white app canvas |
| Surface | `#FFFFFF` | Cards, dialogs, forms |
| Ink | `#24302E` | Main text |
| Muted | `#66746F` | Supporting labels |
| Border | `#E3E8E3` | Quiet section separation |
| Accent | `#B7955B` | Sparse decorative highlights; not small text on white |
| Danger | `#B33D3D` | Destructive actions and explicit errors |

- Rounded corners: **10–14px**, not oversized cards everywhere.
- Shadows: subtle and used only for elevated panels/dialogs.
- Spacing: Bootstrap's 4/8/12/16/24/32 rhythm; do not invent dozens of spacing tokens.
- Photography: consistent **4:5 garment** crops in the catalogue. A product detail page can use a full gallery.
- Minimum comfortable interactive target: **44 × 44 CSS pixels** on touch screens.
- Typography: clear 14–16px body text, 20–28px section headings, numeric totals visibly larger. Use tabular numerals for KWD amounts.
- Use color together with labels/icons; never communicate payment or delivery status with color alone.
- Place destructive actions away from primary Save/Collect controls and use confirmation when necessary.

## 4. Screen map

Only the screens required by the SRS are in scope:

| Screen | Main responsibility | UI approach |
| --- | --- | --- |
| Login | Secure employee entry | Simple focused form |
| Admin Dashboard | Sales, orders, collections, due deliveries | Compact KPI tiles, date filter, short actionable lists |
| Categories / Products | Bilingual product management | Searchable table, short form, image gallery |
| Product Sizes / Measurements | Size chart and dynamic field definitions | Product-scoped editor, plain form controls |
| Alteration Services | Category, service, pricing | Compact list and edit form |
| Customers | Search, create, history | Fast mobile/name lookup and details |
| POS Billing | Customer → product/service → size/measurements → cart → pay | **Two working areas** only |
| Orders / Delivery | Status and due-date tracking | Search/filter, detail drawer or dedicated detail page |
| Payments / Closing | Collection, split payment, refunds, cash reconciliation | Clear transaction dialogs and explicit totals |
| Reports | Sales, products, collection, delivery | Date range, server paging, export/print |
| Users / Settings | Role-controlled configuration | Focused sections with independent Save boundaries |

Do not create a separate public e-commerce storefront in this application. The client's website already exists.

## 5. POS: exactly two working areas

For desktops and counter tablets:

```text
+------------------------------------------------------------------+
| Brand / Branch or Counter / Language / User / Exit                |
+----------------------------------------+-------------------------+
| LEFT: Product & Service Catalogue      | RIGHT: Current Order    |
|                                        |                         |
| Search | Category tabs                 | 1. Customer + Delivery  |
| Product images, names, prices          | 2. Cart and Item Details|
|                                        | 3. Amounts / Discounts  |
| Tap product -> details + size +        | 4. Save / Pay / Print   |
| dynamic measurements -> Add to cart    |                         |
+----------------------------------------+-------------------------+
```

- Left **~60–65%**, right **~35–40%** at common desktop widths. No third permanent column.
- Right column shows the selected customer and running totals at all times without hiding the Save/Pay action behind a long scroll.
- Customer selection is required before adding billable items, following the existing project's verified customer-first workflow.
- Product tile: primary image, bilingual name when present, valid current price, optional sale price, clear selected state. Inactive items never appear in new-sale lookup.
- Product details: Bootstrap modal on desktop, accessible full-screen modal on smaller screens; product gallery, available sizes, quantity, dynamic measurement fields, tailoring notes, Add to Cart.
- **Measurements belong to each cart item/order item.** Validate required values, units and sizes. Changing one item's measurements must not alter another.
- Alterations are distinct line items/operations; do not treat every product as a service or overload a `ServiceId`.
- Use one payment area. Cash, KNET/card, bank transfer, other, advances and split payment should have one consistent flow; show total, paid, outstanding, tendered cash and change when relevant.
- Do not automatically mark a transaction paid simply because Save Order succeeded.
- Print/reprint from persisted invoice data, not unsaved browser totals.
- On narrow screens stack catalogue and cart or expose the cart through a Bootstrap offcanvas. It remains **two conceptual areas**, not a new three-column layout.

## 6. Admin flow

- Dashboard prioritizes **Today / This Week / Month / Custom** and actionable orders, not decoration.
- Master screens use clear List + Add/Edit flow. Product gallery editing is inside product details, not a separate confusing global image manager.
- Keep Category, Product, Alteration Service and Measurement definitions distinct.
- Show required fields explicitly, validate server-side and preserve form input on errors.
- Use server-side paging for long tables, with useful category, active-status and date filters.
- Keep each Save action scoped to the section the user changed. Show actionable success/error messages.
- Admin-only features must be protected on the server, not simply hidden in the browser.
- Record audit entries for price edits, product deactivation, order changes, discounts, refunds and cash closing when those flows are implemented.

## 7. English / Arabic and accessibility

- Use `lang="en"` or `lang="ar"` and `dir="ltr"`/`dir="rtl"` at the root or applicable section.
- Prefer Bootstrap logical utilities (`ms-*`, `me-*`, `text-start`, `text-end`) rather than hard-coded left/right margins.
- Keep product names in their actual language, including Arabic product titles on images/cards.
- Labels, errors, buttons, order statuses and print labels all need English/Arabic coverage; avoid mixed untranslatable strings.
- KWD precision is **0.000**. Calculations are authoritative on the server using `decimal`, never JavaScript floats for final saved balances.
- Keyboard navigation, visible focus, accessible labels, dialog focus restoration and WCAG AA color contrast are acceptance criteria.
- Long Arabic labels must not clip on forms or RDLC receipts. Test both LTR and RTL with real long content.

## 8. Performance budget (targets, not measured results)

| Metric / constraint | Target |
| --- | --- |
| Largest Contentful Paint | <= 2.5 s at the 75th percentile of real device visits |
| Interaction to Next Paint | <= 200 ms at the 75th percentile |
| Cumulative Layout Shift | <= 0.1 |
| JS outside Bootstrap | Keep first-page transferred JS under ~50 KB compressed when practicable |
| Custom CSS | Keep small; prefer Bootstrap variables/utilities and remove unused styles |
| Catalogue results | Server filtered and paginated; no full catalogue JSON on first load |
| Product images | Thumbnail variants, modern format, explicit dimensions and lazy loading below first row |
| API calls | Avoid duplicate bootstrap/search requests, uncontrolled polling and N+1 database queries |
| Static assets | Fingerprinted/versioned and cached; do not cache sensitive responses |
| Billing | Show one clear loading state and prevent duplicate clicks while saving |
| Reports | Server-side aggregation, paginated results and indexed date/filter columns |

Measure these on a realistic shop PC, tablet and mobile throttling profile. A declared budget is not proof of passing it. Performance must be verified after the pages and real catalogue data exist.

## 9. Simple implementation conventions

1. Build **one screen at a time**, matching SRS before adding extras.
2. Keep Razor markup readable. Use a partial only when the same piece is actually reused, not for every small block.
3. Keep a small set of CSS variables in one stylesheet; prefer scoped page classes and Bootstrap's component styles.
4. Put page-specific behavior in a small `.js` or `.mjs` file. Avoid giant all-page scripts, custom state frameworks and unnecessary abstraction layers.
5. Use numeric IDs in URLs and contracts. Separate Product from Service in model names and code.
6. Validate everything on the server; do not rely on HTML attributes alone.
7. All mutations require authentication, permission checks, antiforgery and safe transaction handling.
8. Integrate the live website **read-only** at first, only after checking its platform and available catalogue export/API. Never overwrite historical orders.
9. Keep migrations manual, guarded against the wrong database and reviewed on an isolated test DB.
10. Do not borrow the old Tailor project's salary/workshop/branch navigation, long CSS or entire POS JavaScript bundle.

## 10. Order of implementation and acceptance

| Priority | Delivery | Completion evidence |
| --- | --- | --- |
| 1 | Shared Bootstrap layout and approved brand tokens | Admin/POS responsive shell; English and Arabic screenshots; no heavy theme |
| 2 | Authenticated Product/Category masters | CRUD, role checks, price validation, active filter and gallery tests |
| 3 | Sizes / dynamic measurement editor | Required fields, size charts, product/category scope, server validation |
| 4 | Two-area POS and saved order snapshots | Customer-first cart, separate product/service, per-item measurements and retry tests |
| 5 | Payments, RDLC invoice and daily closing | Idempotent records, cash change, real-device print and permissions |
| 6 | Dashboards and reports | Correct server totals, filters, exports and measured page performance |

### UX acceptance checklist

- [ ] Desktop POS has only two working areas; cashier can reach customer, cart and payment without confusion.
- [ ] Product photos are real, optimized and not stretched; no fabricated prices or catalogue records.
- [ ] A product's size and mandatory measurements persist on the **correct item**.
- [ ] English and Arabic fit on desktop and mobile; RTL flips layout consistently.
- [ ] Saving/retrying cannot duplicate an order or payment; correct balance is shown.
- [ ] Responsive UI, keyboard operation, contrast, printing and performance targets have real test evidence.
- [ ] No Workshop/Salary/Multi-Branch features or unnecessary frontend dependencies have slipped in.

**Implementation rule:** The SRS governs behavior; this document governs UI presentation and lightweight technical choices. [The master plan](master-delivery-quality-plan.md) defines implementation gates and acceptance. Any conflict requires a recorded decision before code changes.
