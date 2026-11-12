import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const list = read("src/IbdaaAtelier.Web/Views/Orders/Index.cshtml");
const orders = read("src/IbdaaAtelier.Web/Controllers/OrdersController.cs");
const details = read("src/IbdaaAtelier.Web/Views/Orders/Details.cshtml");
const payment = read("src/IbdaaAtelier.Web/Services/PaymentService.cs");
const form = read("src/IbdaaAtelier.Web/Models/Payments/PaymentForm.cs");
const paymentView = read("src/IbdaaAtelier.Web/Views/Payments/Take.cshtml");
const paymentController = read("src/IbdaaAtelier.Web/Controllers/PaymentsController.cs");
const invoice = read("src/IbdaaAtelier.Web/Views/Invoices/Index.cshtml");
const report = read("src/IbdaaAtelier.Web/Services/InvoiceReportService.cs");
const srs = read("docs/IBDAA-Atelier-SRS-2026-11-10.md");

test("cashier finds a saved invoice and receives only its outstanding balance", () => {
    assert.match(orders, /order\.OrderId == invoiceId/);
    assert.match(orders, /order\.CustomerMobile\.Contains\(search\)/);
    assert.match(list, /User\.IsInRole\("Cashier"\)/);
    assert.match(list, /Model\.BalanceByOrder\.GetValueOrDefault\(order\.OrderId\) > 0m/);
    assert.match(list, /order\.Status != "Cancelled"/);
    assert.match(list, /asp-controller="Payments" asp-action="Take"/);
    assert.match(details, /Receive Balance/);
});

test("returning-customer payment shows existing ledger and keeps original order ID", () => {
    assert.match(form, /List<OrderPayment> PreviousPayments/);
    assert.match(form, /decimal Refunded/);
    assert.match(payment, /previousPayments = await db\.OrderPayments\.AsNoTracking\(\)/);
    assert.match(payment, /PreviousPayments = previousPayments/);
    assert.match(payment, /order\.Total - paid/);
    assert.match(paymentView, /Model\.PreviousPayments/);
    assert.match(paymentView, /previous\.Change > 0m/);
    assert.match(paymentView, /method\.Method == "Card" \? "WAMD"/);
    assert.match(paymentView, /name="OrderId" value="@Model\.Form\.OrderId"/);
});

test("saving a balance returns to updated invoice, without creating a new order or delivering", () => {
    assert.match(paymentController, /Authorize\(Roles = "Cashier"\)/);
    assert.match(paymentController, /return RedirectToAction\("Index", "Invoices", new \{ id = form\.OrderId, paper = "80mm" \}\);/);
    assert.match(payment, /request\.Fingerprint != fingerprint/);
    assert.match(payment, /amount > balance/);
    assert.match(payment, /IsolationLevel\.Serializable/);
    assert.doesNotMatch(payment, /order\.Status\s*=(?!=)/);
    assert.match(invoice, /TempData\["Message"\]/);
    assert.match(invoice, /asp-route-paper="80mm"/);
    assert.match(invoice, /asp-route-paper="a4"/);
    assert.match(report, /header\["OrderNumber"\] = order\.OrderId\.ToString\(CultureInfo\.InvariantCulture\)/);
    assert.match(report, /payment\.Change > 0m/);
    assert.match(srs, /Returning Customer - Remaining Balance Payment/);
});
