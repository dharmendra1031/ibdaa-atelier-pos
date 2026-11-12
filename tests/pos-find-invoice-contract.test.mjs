import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const controller = read("src/IbdaaAtelier.Web/Controllers/PosController.cs");
const view = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
const pos = read("src/IbdaaAtelier.Web/wwwroot/js/pos.js");
const lookup = controller.split("public async Task<IActionResult> FindInvoice")[1].split("[HttpPost")[0];

test("cashier can find an existing invoice by its saved number", () => {
    assert.match(controller, /Authorize\(Roles = "Cashier"\)/);
    assert.match(lookup, /o\.OrderId == invoiceId/);
    assert.match(lookup, /db\.OrderItems\.AsNoTracking\(\)/);
    assert.match(lookup, /payments\.GetPaidAsync\(order\.OrderId, ct\)/);
    assert.doesNotMatch(lookup, /db\.OrderPayments|db\.OrderRefunds/);
    assert.match(lookup, /order\.Status == "Cancelled" \? 0m/);
    assert.doesNotMatch(lookup, /SaveChangesAsync|Orders\.Add|OrderPayments\.Add/);
});

test("invoice binding stays in the cart and never creates or edits an order", () => {
    assert.match(view, /id="findInvoiceNumber"/);
    assert.match(view, /pattern="\[0-9\]\+"/);
    assert.match(lookup, /long\.TryParse\(input, out var invoiceId\)/);
    assert.match(view, /data-invoice-url=/);
    assert.match(view, /id="boundInvoice" hidden/);
    assert.match(view, /id="boundInvoiceItems"/);
    assert.match(pos, /function showInvoice\(invoice\)/);
    assert.match(pos, /invoiceNumberView\.textContent = String\(invoice\.orderId\)/);
    assert.match(pos, /newOrderCart\.hidden = true/);
    assert.match(pos, /boundInvoiceView\.hidden = false/);
    assert.match(pos, /if \(paidMode \|\| saving \|\| completed \|\| boundInvoiceId\) return/);
    assert.match(pos, /boundInvoiceId = 0/);
    assert.match(pos, /renderCart\(\)/);
});

test("returning invoice uses the original payment and print routes", () => {
    assert.match(pos, /receive\.hidden = \(invoice\.balance <= 0 && !paymentPayload\) \|\| invoice\.status === "Cancelled"/);
    assert.match(pos, /boundInvoiceBalance = invoice\.balance/);
    assert.match(view, /id="boundInvoicePayment"/);
    assert.match(pos, /getElementById\("boundInvoicePayment"\)\.addEventListener\("click", openPaymentDialog\)/);
    assert.match(view, /data-payment-prepare-url=/);
    assert.match(view, /data-payment-record-url=/);
});
