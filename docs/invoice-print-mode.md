# Invoice print mode

Admin > Shop settings has **Open invoice PDF in a new tab** (default on).

- On: Save & Print opens the saved 80mm invoice PDF in a new tab. The cashier can use Chrome's PDF print button and choose a destination.
- Off: Save & Print sends the same RDLC-generated 80mm PDF directly to the Windows default printer through SumatraPDF. No extra invoice tab or browser print dialog opens.

Direct printing requires the ASP.NET app to run on the same Windows cashier PC as the printer. Install SumatraPDF there, set `Printing:SumatraPdfPath` to its executable in local configuration, and set the receipt printer as Windows default. Verify paper size, margins, driver and a test receipt at the actual printer.

The direct print endpoint accepts only localhost requests, so the print job goes to the printer attached to that same computer. Existing order and payment writes finish before printing starts; a print failure does not duplicate or roll back an order. A failed print can be retried from Orders.

Apply `database/040_invoice_print_mode.sql` to an approved backed-up database before deploying this code. The local `IBDAA_TEST` database received this migration on 14 November 2026.
