import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const orderController = read("src/IbdaaAtelier.Web/Controllers/OrdersController.cs");
const orderView = read("src/IbdaaAtelier.Web/Views/Orders/Index.cshtml");
const details = read("src/IbdaaAtelier.Web/Views/Orders/Details.cshtml");
const model = read("src/IbdaaAtelier.Web/Models/Orders/OrderDetailsPage.cs");
const payment = read("src/IbdaaAtelier.Web/Views/Payments/Take.cshtml");
const js = read("src/IbdaaAtelier.Web/wwwroot/js/payments.js");

test("order list uses existing payment and refund ledger to show paid and outstanding", () => {
    assert.match(orderController, /orderIds\.Contains\(p\.OrderId\)/);
    assert.match(orderController, /orderIds\.Contains\(r\.OrderId\)/);
    assert.match(orderController, /GroupBy\(p => p\.OrderId\)/);
    assert.match(orderController, /GroupBy\(r => r\.OrderId\)/);
    assert.match(orderController, /received\.GetValueOrDefault\(o\.OrderId\) - refunded\.GetValueOrDefault\(o\.OrderId\)/);
    assert.match(orderController, /o\.Status == "Cancelled" \? 0m/);
    assert.match(model, /PaidByOrder/);
    assert.match(model, /BalanceByOrder/);
    assert.match(orderView, /Model\.BalanceByOrder\.GetValueOrDefault/);
    assert.match(orderView, /Model\.PaidByOrder\.GetValueOrDefault/);
});

test("refund and order cancellation controls stay separate from cashier payment action", () => {
    assert.match(details, /Manager actions/);
    assert.match(details, /User\.IsInRole\("Admin"\)/);
    assert.match(details, /Record a refund/);
    assert.match(details, /asp-controller="Refunds"/);
    assert.match(details, /asp-controller="OrderStatus"/);
    assert.match(details, /asp-controller="Payments"/);
});

test("one-click full payment fills the selected method without altering server totals", () => {
    assert.match(payment, /id="quickPaymentMethod"/);
    assert.match(payment, /id="fillPaymentBalance"/);
    assert.match(payment, /data-balance=/);
    assert.match(payment, /data-method="@line.Method"/);
    assert.match(js, /input\.dataset\.method === method\.value/);
    assert.match(js, /if \(tendered\) tendered\.value = ""/);
    assert.match(js, /if \(!form\.checkValidity\(\)\) return/);
});
