# IBDAA Atelier — official gents catalogue

Source: https://www.ibdaaatelier.com/products
Checked: 12 November 2026. Public Arabic product names and KWD prices below are
the source of truth. English translations and broad POS category groupings
are for navigation, not claims about fabric, color, stock or sizing.

| Original product title | Display price (KWD) |
|---|---:|
| قندورة "سلطان" | 23.000 |
| قفطان الزمرد | 26.000 |
| قندورة "أصيل" | 25.000 |
| قندورة نسيج الغسق | 24.000 |
| جابادور "أمير الأناقة" | 23.000 |
| جابادور الأزرق الكشخة | 25.000 |
| قفطان "نسمات الأفق" | 24.000 |
| قندورة "هيبة صيف" | 25.000 |
| جلابة "نسيم كازا" | 30.000 |
| جابادور "إبداع" المغربي | 24.000 |
| قندورة الهبة | 23.000 |
| جلابية الحضور | 30.000 |
| جابادور "الأفق الأزرق" | 26.000 |
| عرض الصيف (٢قندورة+قفطان) | 66.000 (regular 75.000) |
| "أناقة الأب والابن" بلمسة مغربية | 51.000 |
| "نسمات الصيف" Summer Breeze | 19.000 |

## Safe setup

- **Fresh empty TEST database:** run `database/029_demo_seed.sql` and then `database/030_demo_media_and_details.sql` after the preceding schema migrations.
- **Existing ladies-seeded TEST database:** back up `IBDAA_TEST`, inspect orders and run `database/037_replace_ladies_test_catalog.sql`. It makes the old sample products and rates inactive, preserving their numeric IDs and historical invoices. Unreferenced synthetic customers are deleted; customers referenced by orders are kept.
- Run `database/038_verify_gents_catalog.sql` after either route to inspect product names, effective prices and photo coverage. Do not execute these scripts against production.

## Photos, sizes and tailoring rates

The public listing did not expose verified direct downloadable photo files.
`ProductImages.ImagePath` uses uploaded files under the app's `wwwroot/uploads/products/{ProductId}/`.
The old female garment images and `demo.png` links are **not** assigned to any new products.
Upload **the shop's own original images** through Admin > Product Images before the catalogue is used by cashiers.
Do not borrow similar garments' photos from unrelated merchants.

No made-up stock levels, size charts, alteration service prices or customer identities are seeded.
The SRS-provided Chest, Shoulder, Sleeve Length and Total Length measurement labels
are optional until shop tailoring values are configured and approved.
The publicly displayed product prices may change; verify against the site before release.
