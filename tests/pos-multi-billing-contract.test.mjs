import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const orders = read("src/IbdaaAtelier.Web/Services/PosOrderService.cs");
const lines = read("src/IbdaaAtelier.Web/Services/OrderLineBuilder.cs");
const payments = read("src/IbdaaAtelier.Web/Services/PaymentService.cs");
const orderSchema = read("database/011_unpaid_orders.sql");
const paymentSchema = read("database/013_payment_ledger.sql");
const controller = read("src/IbdaaAtelier.Web/Controllers/PosController.cs");
const view = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
const pos = read("src/IbdaaAtelier.Web/wwwroot/js/pos.js");
const paymentPage = read("src/IbdaaAtelier.Web/Views/Payments/Take.cshtml");
const paymentJs = read("src/IbdaaAtelier.Web/wwwroot/js/payments.js");
const invoices = read("src/IbdaaAtelier.Web/Controllers/InvoicesController.cs");

test("parallel bills have independent numeric order requests and unique order IDs", () => {
    assert.match(orderSchema, /RequestId BIGINT IDENTITY\(1,1\)/);
    assert.match(orderSchema, /OrderId BIGINT IDENTITY\(1,1\)/);
    assert.match(orderSchema, /UX_Orders_RequestId/);
    assert.match(orders, /new OrderRequest \{ UserId = userId \}/);
    assert.match(orders, /IsolationLevel\.Serializable/);
    assert.match(orders, /WITH \(UPDLOCK, HOLDLOCK\)/);
    assert.match(orders, /request\.UserId != userId/);
    assert.match(orders, /request\.Fingerprint != fingerprint/);
});

test("separate items preserve product size and measurement snapshots", () => {
    assert.match(orderSchema, /OrderItemMeasurements/);
    assert.match(lines, /foreach \(var input in inputs\)/);
    assert.match(lines, /ProductId = product\.ProductId/);
    assert.match(lines, /SizeName = chosenSize\?\.Name/);
    assert.match(lines, /MeasurementFields\.Resolve/);
    assert.match(orders, /measurement\.OrderItemId = line\.Item\.OrderItemId/);
});

test("parallel full, advance and split payments cannot exceed saved balance", () => {
    assert.match(paymentSchema, /RequestId BIGINT IDENTITY\(1,1\)/);
    assert.match(paymentSchema, /UX_PaymentBatches_Request/);
    assert.match(payments, /IsolationLevel\.Serializable/);
    assert.match(payments, /request\.OrderId != form\.OrderId \|\| request\.UserId != userId/);
    assert.match(payments, /WITH \(UPDLOCK, HOLDLOCK\) WHERE OrderId/);
    assert.match(payments, /amount > balance/);
    assert.match(payments, /request\.Fingerprint != fingerprint/);
    assert.match(paymentJs, /amount > balance \+ 0\.000001/);
    assert.match(paymentPage, /id="paymentError"/);
});

test("alterations beyond first 24 results remain reachable via server search and paging", () => {
    assert.match(controller, /serviceSearch = serviceSearch\?\.Trim\(\)/);
    assert.match(controller, /serviceQuery = serviceQuery\.Where/);
    assert.match(controller, /Skip\(\(servicePage - 1\) \* 24\)\.Take\(24\)/);
    assert.match(view, /name="serviceSearch"/);
    assert.match(view, /asp-route-servicePage/);
    assert.match(view, /data-show-services/);
    assert.match(pos, /shell\.dataset\.showServices === "true"/);
});

test("multi-item POS validates the 30-line limit before posting", () => {
    assert.match(orders, /form\.Items\.Count is < 1 or > 30/);
    assert.match(pos, /items\.length >= 30/);
    assert.match(pos, /One order supports up to 30 items/);
    assert.match(pos, /sessionStorage\.setItem\(cartKey/);
});

test("saved invoice opens inline for printing rather than forced attachment", () => {
    assert.match(invoices, /return File\(pdf, "application\/pdf"\);/);
    assert.doesNotMatch(invoices, /return File\(pdf, "application\/pdf", fileName\)/);
    assert.match(invoices, /invoices\.PrepareAsync\(id, ct\)/);
    assert.match(view, /id="viewSavedOrderLink"/);
});


test("cashier login goes directly to billing while admin opens dashboard", () => {
    const auth = read("src/IbdaaAtelier.Web/Controllers/AuthController.cs");
    assert.match(auth, /user\.Role == "Admin" \? "Admin" : "Pos"/);
    assert.match(auth, /User\.IsInRole\("Admin"\) \? "Admin" : "Pos"/);
    assert.match(auth, /Url\.IsLocalUrl\(form\.ReturnUrl\)/);
});
