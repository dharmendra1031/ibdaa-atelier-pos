import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const read = file => readFileSync(new URL("../" + file, import.meta.url), "utf8");
const view = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
const css = read("src/IbdaaAtelier.Web/wwwroot/css/site.css");
const js = read("src/IbdaaAtelier.Web/wwwroot/js/pos.js");

test("POS retains two content areas with a wider responsive work surface", () => {
    assert.match(view, /class="col-lg-7"/);
    assert.match(view, /class="col-lg-5"/);
    assert.match(css, /\.app-layout \{ max-width: 1700px/);
    assert.match(css, /row-cols-xxl-4|pos-product-grid/);
});

test("missing uploaded photos are clearly marked rather than presented as product photos", () => {
    assert.match(view, /pos-missing-photo/);
    assert.match(view, /No photo yet/);
    assert.match(view, /product.ImagePath/);
    assert.doesNotMatch(view, /src="https?:\/\//);
});

test("save remains unavailable until a customer and cart are selected", () => {
    assert.match(view, /id="saveOrderButton"[^>]*disabled/);
    assert.match(js, /saveButton\.disabled = saving \|\| completed \|\| !customerId \|\| items\.length === 0/);
    assert.match(js, /saveButton\.disabled = true/);
    assert.match(js, /customerAbort\?\.abort\(\);\s*customerResults\.replaceChildren\(\);\s*renderCart\(\)/);
});

test("idempotent server-side unpaid order save remains unchanged", () => {
    const server = read("src/IbdaaAtelier.Web/Services/PosOrderService.cs");
    assert.match(server, /IsolationLevel\.Serializable/);
    assert.match(server, /UPDLOCK, HOLDLOCK/);
    assert.match(server, /request\.Fingerprint != fingerprint/);
});

test("cashier POS header shows authenticated role and username without changing admin", () => {
    const layout = read("src/IbdaaAtelier.Web/Views/Shared/_Layout.cshtml");
    const auth = read("src/IbdaaAtelier.Web/Controllers/AuthController.cs");
    assert.match(layout, /controller == "Pos" && User\.IsInRole\("Cashier"\)/);
    assert.match(layout, /pos-header-account/);
    assert.match(layout, /pos-header-account-copy/);
    assert.match(layout, /@User\.Identity\?\.Name/);
    assert.match(layout, /أمين الصندوق/);
    assert.match(css, /\.pos-page \.site-header/);
    assert.match(css, /\.pos-header-avatar/);
    assert.match(css, /\.pos-header-title/);
    assert.match(css, /@media \(max-width: 575\.98px\)/);
    assert.match(auth, /new Claim\(ClaimTypes\.Role, user\.Role\)/);
});

test("cart header shows compact customer searches and Invoice NO with matched heading size", () => {
    const checkout = view.indexOf('class="card filter-card pos-checkout"');
    const name = view.indexOf('id="customerSearch"');
    const mobile = view.indexOf('id="customerMobileSearch"');
    const cart = view.indexOf('id="orderCart"');

    assert.ok(checkout >= 0 && checkout < name && name < mobile && mobile < cart);
    assert.match(view, /class="pos-cart-header"[\s\S]*?id="cartCount"[\s\S]*?id="nextOrderNumber"/);
    assert.match(view, /NextOrderNumber\.Replace\("IA-", "NO-"\)/);
    assert.match(view, /\? "فاتورة" : "Invoice"/);
    assert.doesNotMatch(view, /pos-customer-bar|pos-order-preview|customerMobileDisplay/);
    assert.match(js, /async function searchCustomers\(input\)/);
    assert.match(js, /customerSearch\.addEventListener\("input", \(\) => searchCustomers\(customerSearch\)\)/);
    assert.match(js, /customerMobileSearch\.addEventListener\("input", \(\) => searchCustomers\(customerMobileSearch\)\)/);
    assert.match(js, /nextOrderNumber\.textContent = "NO-" \+ number\[1\]/);
    assert.match(css, /\.pos-app-layout \.pos-cart-header/);
    assert.match(css, /\.pos-order-number \{/);
    assert.match(css, /font-size: 22px; font-weight: 650; white-space: nowrap/);
    assert.match(css, /\.pos-app-layout \.pos-cart-header \.pos-order-heading,\s*\.pos-order-number \{ font-size: clamp\(15px, 4vw, 18px\); \}/);
    assert.match(css, /@media \(max-width: 420px\)/);
    assert.doesNotMatch(css, /pos-order-preview|pos-customer-bar|pos-customer-mobile/);
});
