# IBDAA Atelier — UI visual references

These are the three **original concept images** selected during the UI/UX discussion on 11 November 2026.

| Area | File |
| --- | --- |
| Admin Dashboard | [admin.png](admin.png) |
| Cashier POS Billing | [pos.png](pos.png) |
| Login | [login.png](login.png) |

**Visual target:** reproduce the approved palette, composition, typography, spacing, icons, imagery and responsive feel as closely as technically possible in the existing ASP.NET Core MVC/Bootstrap implementation.

**Functional authority:** the SRS (`docs/IBDAA-Atelier-SRS-2026-11-10.md`) and the role-separation decision supersede illustrative labels in generated images. In particular:
- No branch selector, POS terminal number, or unconfirmed stock/inventory management.
- Admin is for dashboard, management and oversight; Admin cannot use POS order creation/receive-payment endpoints.
- Only the Cashier has the dedicated two-area POS. Avoid a persistent sidebar there. The cart and checkout actions must remain visible in the available viewport where practical; test narrow/short screens and allow necessary overflow for accessibility rather than hiding items.
- No fabricated sample prices, customers or orders in working software. Do not blindly replicate mock numbers or labels.
- Login should route to the appropriate workspace based on the authenticated role. Any mockup controls (role tabs, mobile sign-in, remember-me, reset password) require real product/SRS approval and a working secured backend before adding.
- Images are **design assets only**, not screenshots of implemented software.

Use the full-resolution PNGs as stable version-controlled references; do not modify or recompress them.
