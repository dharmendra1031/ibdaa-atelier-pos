# POS UI component checklist

Reference: `docs/design-reference/pos.png`
Working branch: `development`
Scope: verify each component against the approved reference and real browser behavior.

| Component | Verification | Status |
| --- | --- | --- |
| Header / branding | Premium spacing, real cashier role and username, sign-out, language switch, RTL, mobile | Code updated; browser screenshot review pending |
| Customer toolbar | Customer search/selection, quick add, mobile and delivery alignment | Pending |
| Catalogue tabs | Products / Alterations layout and active state | Pending |
| Categories | Width, spacing, selection and scroll | Pending |
| Product cards | Real images, consistent card heights, titles and prices | Pending |
| Cart empty state | Vertical alignment and empty messaging | Pending |
| Cart populated state | Quantity/edit/remove, totals, responsive fit and overflow | Pending |
| Checkout actions | Disabled/loading/error/success and save protection | Pending |
| Dialogs | Measurements, quick customer, payment and responsive placement | Pending |

## Rules
- Fix one component at a time, without unrelated UI or billing changes.
- Compare desktop, tablet, mobile and Arabic RTL before sign-off.
- Source tests passing does not mean pixel-perfect screenshot verification.
- Keep persistent IDs numeric and preserve original order/payment behavior.
