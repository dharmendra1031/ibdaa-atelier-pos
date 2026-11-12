import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");

test("invoice number is the numeric SQL identity, without a second sequence", () => {
    const schema = read("database/011_unpaid_orders.sql");
    const order = read("src/IbdaaAtelier.Web/Models/Orders/Order.cs");
    const saved = read("src/IbdaaAtelier.Web/Models/Orders/OrderSaveForm.cs");
    assert.match(schema, /OrderId BIGINT IDENTITY\(1,1\)/);
    assert.doesNotMatch(schema, /OrderNumber|IA-|NO-/);
    assert.match(order, /\[NotMapped\] public string OrderNumber => OrderId\.ToString/);
    assert.doesNotMatch(order, /public string OrderNumber \{ get; set; \}/);
    assert.match(saved, /record SavedOrder\(long OrderId, decimal Total, bool Replayed\)/);
});

test("Find Invoice accepts numeric IDs and queries the original order", () => {
    const controller = read("src/IbdaaAtelier.Web/Controllers/PosController.cs");
    const view = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
    const lookup = controller.split("public async Task<IActionResult> FindInvoice")[1].split("[HttpPost")[0];
    assert.match(lookup, /input\.All\(char\.IsAsciiDigit\)/);
    assert.match(lookup, /long\.TryParse\(input, out var invoiceId\)/);
    assert.match(lookup, /o\.OrderId == invoiceId/);
    assert.doesNotMatch(lookup, /OrderNumber|IA-|NO-/);
    assert.match(view, /id="findInvoiceNumber"[^>]*type="search"[^>]*maxlength="20"/);
    assert.match(view, /inputmode="numeric" pattern="\[0-9\]\+"/);
    assert.match(view, /id="nextOrderNumber">@Model.NextInvoiceNumber/);
    assert.match(controller, /NextInvoiceNumber = await GetNextInvoiceNumberAsync\(ct\)/);
    assert.match(controller, /MaxAsync\(order => \(long\?\)order.OrderId, ct\) \?\? 0/);
    assert.match(controller, /return checked\(lastInvoiceId \+ 1\)/);
    assert.match(controller, /return Json\(new \{ requestId, nextInvoiceNumber = await GetNextInvoiceNumberAsync\(ct\) \}\)/);
});

test("POS, orders, payment and both RDLC print modes use numeric IDs", () => {
    const pos = read("src/IbdaaAtelier.Web/wwwroot/js/pos.js");
    const orders = read("src/IbdaaAtelier.Web/Controllers/OrdersController.cs");
    const invoice = read("src/IbdaaAtelier.Web/Services/InvoiceReportService.cs");
    const details = read("src/IbdaaAtelier.Web/Views/Orders/Details.cshtml");
    const list = read("src/IbdaaAtelier.Web/Views/Orders/Index.cshtml");
    const print = read("src/IbdaaAtelier.Web/Views/Invoices/Index.cshtml");
    assert.match(pos, /newInvoiceNumber = String\(data\.nextInvoiceNumber\)/);
    assert.match(pos, /invoiceNumberView\.textContent = String\(data\.orderId\)/);
    assert.match(pos, /invoiceNumberView\.textContent = String\(invoice\.orderId\)/);
    assert.match(orders, /order\.OrderId == invoiceId/);
    assert.match(invoice, /header\["OrderNumber"\] = order\.OrderId\.ToString\(CultureInfo\.InvariantCulture\)/);
    assert.match(details, /@Model\.Order\.OrderId/);
    assert.match(list, /@order\.OrderId/);
    assert.match(print, /Model\.OrderId/);
    for (const report of [
        read("src/IbdaaAtelier.Web/Reports/Invoice80mm.rdlc"),
        read("src/IbdaaAtelier.Web/Reports/InvoiceA4.rdlc")
    ])
        assert.match(report, /Fields!OrderNumber\.Value/);
    assert.doesNotMatch(pos + orders + invoice + details + list + print, /IA-|NO-/);
});

test("legacy computed prefix removal is guarded and preserves existing invoices", () => {
    const migration = read("database/035_numeric_invoice_number.sql");
    const verify = read("database/036_verify_numeric_invoice_number.sql");
    assert.match(migration, /DB_NAME\(\) <> @ExpectedDatabase/);
    assert.match(migration, /DROP INDEX UX_Orders_Number ON dbo\.Orders/);
    assert.match(migration, /ALTER TABLE dbo\.Orders DROP COLUMN OrderNumber/);
    assert.doesNotMatch(migration, /DELETE FROM|TRUNCATE TABLE|DBCC CHECKIDENT|UPDATE dbo\.Orders/);
    assert.match(verify, /COLUMNPROPERTY\(OBJECT_ID\(N'dbo\.Orders'\), 'OrderId', 'IsIdentity'\)/);
    assert.match(verify, /Legacy prefixed invoice column still exists/);
});
