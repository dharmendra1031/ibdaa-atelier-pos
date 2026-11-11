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

test("after unpaid save cashier can go directly to payment or print", () => {
    assert.match(view, /data-payment-url=/);
    assert.match(view, /id="viewSavedOrderLink"/);
    assert.match(pos, /savedOrderLink.href = shell.dataset.paymentUrl \+ savedId/);
    assert.match(pos, /viewSavedOrderLink.href = shell.dataset.orderUrl \+ savedId/);
    assert.match(pos, /requestId: id, customerId, deliveryDate: date/);
});

test("premium POS uses two full-width work areas without a persistent sidebar", () => {
    assert.match(layout, /pos-app-layout/);
    assert.match(css, /\.pos-app-layout \.app-sidebar/);
    assert.match(css, /\.pos-app-layout \.pos-shell > \.col-lg-7/);
    assert.match(css, /\.pos-page/);
    assert.match(view, /class="col-lg-7"/);
    assert.match(view, /class="col-lg-5"/);
});
