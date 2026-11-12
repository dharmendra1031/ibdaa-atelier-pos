import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const controller = read("src/IbdaaAtelier.Web/Controllers/PosController.cs");
const view = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
const pos = read("src/IbdaaAtelier.Web/wwwroot/js/pos.js");

test("cashier can find an existing invoice by its saved number", () => {
    const lookup = controller.split("public async Task<IActionResult> FindInvoice")[1].split("[HttpPost")[0];
    assert.match(controller, /Authorize\(Roles = "Cashier"\)/);
    assert.match(lookup, /o\.OrderNumber == invoiceNumber/);
    assert.match(lookup, /db\.OrderItems\.AsNoTracking\(\)/);
    assert.match(lookup, /payments\.GetPaidAsync\(order\.OrderId, ct\)/);
    assert.doesNotMatch(lookup, /db\.OrderPayments|db\.OrderRefunds/);
    assert.match(lookup, /order\.Status == "Cancelled" \? 0m/);
    assert.doesNotMatch(lookup, /SaveChangesAsync|Orders\.Add|OrderPayments\.Add/);
});

test("invoice binding stays in the cart and never creates or edits an order", () => {
    assert.match(view, /id="findInvoiceNumber"/);
    assert.match(view, /data-invoice-url=/);
    assert.match(view, /id="boundInvoice" hidden/);
    assert.match(view, /id="boundInvoiceItems"/);
    assert.match(pos, /function showInvoice\(invoice\)/);
    assert.match(pos, /invoiceNumberView\.textContent = invoice\.orderNumber/);
    assert.match(pos, /newOrderCart\.hidden = true/);
    assert.match(pos, /boundInvoiceView\.hidden = false/);
    assert.match(pos, /if \(saving \|\| completed \|\| boundInvoiceId\) return/);
    assert.match(pos, /boundInvoiceId = 0/);
    assert.match(pos, /renderCart\(\)/);
});

test("returning invoice uses the original payment and print routes", () => {
    assert.match(pos, /receive\.href = shell\.dataset\.paymentUrl \+ id/);
    assert.match(pos, /receive\.hidden = invoice\.balance <= 0 \|\| invoice\.status === "Cancelled"/);
    assert.match(pos, /boundInvoiceDetails"\)\.href = shell\.dataset\.orderUrl \+ id/);
    assert.match(view, /data-payment-url="@Url\.Action\("Take", "Payments"\)"/);
    assert.match(view, /data-order-url="@Url\.Action\("Details", "Orders"\)"/);
});
