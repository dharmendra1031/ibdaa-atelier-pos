# IBDAA Atelier business flow review — 12 November 2026

Sources: the canonical linked Google Doc (read on 12 November 2026), local SRS snapshot `IBDAA-Atelier-SRS-2026-11-10.md`, and current `development` code. The findings are proposed clarifications, not a replacement for the canonical SRS.

## How staff currently set up the shop

1. Admin creates bilingual product categories, then products with category, description, regular/optional catalogue sale price, active status and display order. Product setup adds sizes, size charts and product/category measurement definitions. Product images are managed separately, with a primary image. Only active products in active categories appear at POS.
2. Admin creates service categories and alteration services with bilingual name, fixed price, optional completion estimate and active status. Services are independent order items and do not need a product record. Current service master has no image upload; POS service photos are presentation assets, not service data.
3. Admin configures shop identity, invoice footer, register requirement and the POS order discount switch. The switch defaults off. When enabled, cashier can enter an order amount discount before saving. The server validates it again and stores subtotal, discount and net total on the order. Catalogue discount price remains a separate product master price.

## Customer visit to delivery in the current software

| Step | Current path | Status against SRS |
| --- | --- | --- |
| Identify customer | Cashier POS name/mobile search or quick bilingual customer creation | Implemented; customer purchase/measurement history is not shown in POS. |
| Choose work | Active products by category/search with gallery, size, configurable measurements and notes; or active alteration service by category/search | Implemented. Service quantity and notes can be edited in cart. Per-service manual price override is absent. |
| Review quote | Cart shows quantity, unit price and subtotal. Optional order discount appears only when Admin enables it. Delivery date required. | Order discount implemented in this change. Additional charges remain absent. |
| Save order | Cashier saves an unpaid order. Server re-prices items, checks customer/date/measurements, saves item snapshots and an invoice branding snapshot, and prevents duplicate request retries. | Implemented. Payment is a separate step after save. |
| Collect payment | Same invoice can receive later full, partial or split payments. Cash tender/change, register rule, ledger and balance are server checked. | Implemented. Cashier is taken to updated invoice after payment. |
| Process and hand over | Admin changes status New → Confirmed → In Processing → Ready for Delivery → Delivered; cashier can find the order and receive balance. | Partly implemented: delivery status action is Admin only, and no dedicated handover confirmation/customer pickup record exists. |
| Print and reprint | A4/80mm RDLC reads persisted order items, measurements, payments/refunds and invoice branding snapshot. | Implemented in code; physical printer and long Arabic text still need shop hardware QA. |

## SRS items to clarify or correct

- **Discount permission (sections 7, 8, 12–15):** distinguish catalogue `DiscountPrice` from order-level amount discount. The approved behavior here is a shop-wide Admin switch; off means POS field hidden and nonzero discount rejected by server. On means cashier may discount from zero through subtotal before order save. Post-payment discount changes remain unsupported and require an audited financial adjustment policy.
- **Invoice number (sections 7.5 and 10.1):** SRS mentions an order number and also says print only one invoice number. Use the saved numeric `OrderId` itself as the sole customer-facing invoice number, without a prefix. Do not promise the next invoice number before saving because concurrent saves allocate IDs.
- **Service price (section 15.3):** phrase “price (if permitted)” needs a specific role/limit rule. Current service price is fixed from Admin master and re-read on save. Cashier price override is not available.
- **Product gallery (sections 4.3 and 15.1):** “support at least four images” describes capacity, while the example workflow says “upload at least four” before activation. Current Admin can activate with fewer than four. Specify whether four images are mandatory for every product or only a supported capacity.
- **Delivery responsibility (sections 8 and 15.4):** SRS says “mark Delivered” but does not name the actor or define pickup proof. Current role matrix permits Admin only. Decide whether cashier may hand over after collecting balance, whether outstanding balance blocks delivery, and whether pickup name/time/signature is required.
- **Order edits and charges (sections 7.5 and 8.3):** SRS requires charges and audited edits after confirmation but does not define who may change each field, when an invoice is reissued, or how paid amounts are reconciled. Current software has neither additional charges nor confirmed-order editing.
- **Configurable permissions/statuses (sections 8.1, 12, 13):** SRS calls for configurable roles/permissions and extra statuses. Current implementation has fixed Admin/Cashier roles and fixed status transitions. These are open feature gaps, not settings already present.
- **SRS source mismatch:** the canonical Google Doc currently has **19** acceptance scenarios. The local SRS snapshot contains **25**, including six later returning-balance scenarios. The master plan's 19 count matches the canonical document. Decide whether to promote the six local additions to the canonical Doc, then align the plan and acceptance matrix. Add explicit tests for discount switch off/on, over-subtotal and three-decimal validation, invoice totals and zero-balance order.
- **Initial catalogue import (section 19):** local `IBDAA_TEST` contains demo products. Their names/images/prices are not an approved import from the live site. Actual catalogue cutover still requires source review and authorization.
- **Report revenue after discount (section 11):** order sales totals use saved net `Orders.Total`, but product/service item totals remain gross line amounts. Label those breakdowns as gross or define an allocation rule before presenting them as net product revenue.

## Verification still needed

Authenticated browser checks for Admin switch and POS in both languages; physical A4/80mm printing; migration on an isolated SQL Server CI run; complete SRS acceptance matrix; actual client catalogue review. These are not established by source contract tests alone.
