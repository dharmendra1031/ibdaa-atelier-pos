import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const removed = path => existsSync(new URL("../" + path, import.meta.url));
const controller = read("src/IbdaaAtelier.Web/Controllers/PaymentsController.cs");
const posController = read("src/IbdaaAtelier.Web/Controllers/PosController.cs");
const posView = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
const pos = read("src/IbdaaAtelier.Web/wwwroot/js/pos.js");
const list = read("src/IbdaaAtelier.Web/Views/Orders/Index.cshtml");
const details = read("src/IbdaaAtelier.Web/Views/Orders/Details.cshtml");
const dashboard = read("src/IbdaaAtelier.Web/Views/Admin/Index.cshtml");

test("old payment form and JavaScript are removed, financial endpoints remain", () => {
    assert.equal(removed("src/IbdaaAtelier.Web/Views/Payments/Take.cshtml"), false);
    assert.equal(removed("src/IbdaaAtelier.Web/wwwroot/js/payments.js"), false);
    assert.doesNotMatch(controller, /Task<IActionResult> Take\(PaymentForm/);
    assert.match(controller, /Task<IActionResult> Prepare\(/);
    assert.match(controller, /Task<IActionResult> Record\(/);
    assert.match(controller, /Authorize\(Roles = "Cashier"\)/);
    assert.match(controller, /return RedirectToAction\("Index", "Pos", new \{ invoiceId = id, receive = true \}\)/);
});

test("cashier balance action loads the same saved invoice and POS popup", () => {
    assert.match(list, /asp-controller="Pos" asp-action="Index" asp-route-invoiceId="@order.OrderId" asp-route-receive="true"/);
    assert.match(details, /asp-controller="Pos" asp-action="Index" asp-route-invoiceId="@Model.Order.OrderId" asp-route-receive="true"/);
    assert.match(posController, /long\? invoiceId = null, bool receive = false/);
    assert.match(posView, /data-open-invoice-id="@Model.OpenInvoiceId"/);
    assert.match(posView, /id="posPaymentDialog"/);
    assert.match(pos, /showInvoice\(invoice\)/);
    assert.match(pos, /shell\.dataset\.openInvoicePayment === "true"/);
    assert.match(pos, /openPaymentDialog\(\)/);
    assert.match(pos, /savedOrder \|\| paymentPayload \|\| recoveryInvoiceNumber/);
    assert.match(pos, /url\.searchParams\.set\("invoiceId", String\(invoice\.orderId\)\)/);
    assert.match(pos, /url\.searchParams\.delete\("invoiceId"\)/);
    assert.match(pos, /paymentPayload = \{ requestId: paymentRequestId, orderId: order\.orderId, lines \}/);
});

test("dashboard has one product shortcut, no duplicate sidebar links", () => {
    assert.equal((dashboard.match(/asp-controller="Products" asp-action="Index"/g) || []).length, 1);
    assert.doesNotMatch(dashboard, /Quick Links|Sizes \/ Measurements/);
});

test("saved invoice survives refresh and its payment history stays in the POS popup", () => {
    const lookup = posController.split("public async Task<IActionResult> FindInvoice")[1].split("[HttpPost")[0];
    assert.match(lookup, /db\.OrderPayments\.AsNoTracking\(\)/);
    assert.match(lookup, /OrderBy\(payment => payment\.PaymentId\)/);
    assert.match(lookup, /payment\.CreatedAtUtc\.AddHours\(3\)/);
    assert.match(lookup, /Payments = previousPayments\.Select/);
    assert.match(posView, /id="posPreviousPayments"/);
    assert.match(posView, /id="posPreviousPaymentRows"/);
    assert.match(pos, /for \(const payment of invoice\.payments \|\| \[\]\)/);
    assert.match(pos, /payment\.method === "Card" \? "WAMD"/);

    const invoiceStart = pos.indexOf("function showInvoice(invoice)");
    const invoiceEnd = pos.indexOf("openInvoiceButton.addEventListener", invoiceStart);
    const showInvoice = pos.slice(invoiceStart, invoiceEnd);
    assert.match(showInvoice, /url\.searchParams\.set\("invoiceId"/);
    assert.doesNotMatch(showInvoice, /url\.searchParams\.delete\("invoiceId"/);

    const returnStart = pos.indexOf('backToNewBill.addEventListener');
    const returnEnd = pos.indexOf('const allItemsDialog', returnStart);
    assert.match(pos.slice(returnStart, returnEnd), /url\.searchParams\.delete\("invoiceId"\)/);
    assert.match(pos, /shell\.dataset\.openInvoicePayment === "true"/);
});
