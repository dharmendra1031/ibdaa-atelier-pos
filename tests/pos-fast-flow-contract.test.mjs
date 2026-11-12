import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const view = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
const pos = read("src/IbdaaAtelier.Web/wwwroot/js/pos.js");
const customers = read("src/IbdaaAtelier.Web/Controllers/CustomersController.cs");
const controller = read("src/IbdaaAtelier.Web/Controllers/PosController.cs");
const layout = read("src/IbdaaAtelier.Web/Views/Shared/_Layout.cshtml");
const css = read("src/IbdaaAtelier.Web/wwwroot/css/site.css");

test("quick customer creation keeps cashier in POS and uses existing validation", () => {
    assert.match(view, /id="quickCustomerDialog"/);
    assert.match(view, /id="quickCustomerForm"/);
    assert.match(view, /data-customer-create-url=/);
    assert.match(view, /name="NameAr"/);
    assert.match(customers, /Authorize\(Roles = "Admin,Cashier"\)/);
    assert.match(customers, /public async Task<IActionResult> QuickCreate\(CustomerForm form/);
    assert.match(customers, /ValidateAntiForgeryToken/);
    assert.match(customers, /await ValidateAsync\(form, ct\)/);
    assert.match(customers, /await CreateCustomerAsync\(form, ct\)/);
    assert.match(customers, /AddAudit\("Customer.Create"/);
    assert.match(pos, /body: new FormData\(quickCustomerForm\)/);
    assert.match(pos, /RequestVerificationToken: token/);
    assert.match(pos, /customerId = customer.customerId/);
});

test("category selection is one click and keeps delivery date on reload", () => {
    assert.match(view, /class="pos-category /);
    assert.match(view, /asp-route-categoryId="@category.CategoryId"/);
    assert.doesNotMatch(view, /id="catalogCategory"/);
    assert.match(pos, /deliveryDate: deliveryDate.value/);
    assert.match(pos, /saved.deliveryDate >= deliveryDate.min/);
});

test("product modal shows gallery but keeps order-item measurements separate", () => {
    assert.match(controller, /var images = await db.ProductImages.AsNoTracking\(\)/);
    assert.match(controller, /Images = images, Sizes = sizes, Measurements = measurements/);
    assert.match(pos, /product.images.forEach/);
    assert.match(pos, /product.measurements.forEach/);
    assert.match(pos, /measurementId: Number\(field.dataset.measurementId\)/);
});

test("unpaid saves and prints, while paid opens the payment popup", () => {
    assert.match(view, /id="posChooseUnpaid"/);
    assert.match(view, /id="posChoosePaid"/);
    assert.match(view, /id="posPaymentDialog"/);
    assert.match(pos, /paymentDialog.showModal\(\)/);
    assert.match(pos, /finishSale\(order.orderId, tab/);
    assert.match(pos, /requestId: id, customerId, deliveryDate: date/);
    assert.doesNotMatch(view, /id="newOrderButton"/);
});

test("premium POS uses two full-width work areas without a persistent sidebar", () => {
    assert.match(layout, /pos-app-layout/);
    assert.match(layout, /@if \(controller != "Pos"\)/);
    assert.doesNotMatch(view, /class="[^"]*pos-heading|pos-shortcuts/);
    assert.doesNotMatch(css, /\.pos-app-layout \.app-sidebar/);
    assert.match(css, /\.pos-app-layout \.pos-shell > \.col-lg-7/);
    assert.match(css, /\.pos-page/);
    assert.match(view, /class="col-lg-7"/);
    assert.match(view, /class="col-lg-5"/);
});


test("POS shows loading and clear success or failed messages", () => {
    assert.match(view, /id="posToast"/);
    assert.match(view, /id="posToastText"/);
    assert.match(pos, /toast\.setAttribute\("role", error \? "alert" : "status"\)/);
    assert.match(pos, /setBusy\(quickCustomerSave, true/);
    assert.match(pos, /setBusy\(saveButton, true/);
    assert.match(pos, /setBusy\(saveButton, false/);
    assert.match(pos, /Searching customers\.\.\./);
    assert.match(pos, /Loading product\.\.\./);
    assert.match(pos, /Retry Save safely/);
    assert.match(pos, /false, true\)/);
});

test("payment and order status submissions show processing without changing financial APIs", () => {
    const payment = read("src/IbdaaAtelier.Web/wwwroot/js/payments.js");
    const details = read("src/IbdaaAtelier.Web/Views/Orders/Details.cshtml");
    assert.match(payment, /Processing payment\.\.\./);
    assert.match(payment, /if \(save\.disabled\)/);
    assert.match(payment, /save\.setAttribute\("aria-busy", "true"\)/);
    assert.match(payment, /window\.addEventListener\("pageshow"/);
    assert.match(payment, /\.order-feedback \.btn-close/);
    assert.match(read("src/IbdaaAtelier.Web/Views/Payments/Take.cshtml"),
        /class="order-feedback order-feedback-error"/);
    assert.match(details, /class="order-feedback"/);
    assert.match(details, /class="order-feedback order-feedback-error"/);
    assert.match(details, /id="orderStatusForm"/);
    assert.match(details, /Processing\.\.\./);
});
