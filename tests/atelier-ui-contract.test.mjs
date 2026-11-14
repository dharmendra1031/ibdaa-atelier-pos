import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const css = read("src/IbdaaAtelier.Web/wwwroot/css/site.css");
const layout = read("src/IbdaaAtelier.Web/Views/Shared/_Layout.cshtml");
const login = read("src/IbdaaAtelier.Web/Views/Auth/Login.cshtml");
const admin = read("src/IbdaaAtelier.Web/Views/Admin/Index.cshtml");
const pos = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");

test("approved premium palette is shared by all workspaces", () => {
    assert.match(css, /--atelier-primary: #70203d;/);
    assert.match(css, /--atelier-accent: #ca9953;/);
    assert.match(css, /\.admin-page \.app-sidebar/);
    assert.match(css, /\.pos-page \.site-header/);
    assert.match(css, /\.auth-wrap/);
    assert.match(layout, /auth-page/);
    assert.match(layout, /admin-page/);
});

test("admin dashboard has management shortcuts but never a new POS sale", () => {
    assert.match(admin, /class="admin-hero/);
    assert.match(admin, /admin-kpis/);
    assert.match(admin, /admin-master-link/);
    assert.doesNotMatch(admin, /asp-controller="Pos"/);
});

test("login preserves the real credential form and role-based redirection", () => {
    const auth = read("src/IbdaaAtelier.Web/Controllers/AuthController.cs");
    assert.match(login, /asp-for="UserName"/);
    assert.match(login, /asp-for="Password"/);
    assert.match(login, /asp-antiforgery="true"/);
    assert.match(login, /auth-showcase/);
    assert.match(auth, /user\.Role == "Admin" \? "Admin" : "Pos"/);
    assert.doesNotMatch(login, /name="role"/);
});

test("two-section cashier POS has no nested scrolling cart and retains saved actions", () => {
    assert.match(pos, /class="pos-shell row g-4"/);
    assert.match(pos, /class="pos-checkout-actions(?:\s|")/);
    assert.match(pos, /id="orderCart"/);
    assert.match(pos, /id="saveOrderButton"/);
    assert.match(pos, /id="openFindInvoice"/);
    assert.match(pos, /id="boundInvoicePayment"/);
    assert.match(pos, /data-direct-print-url=/);
    assert.match(css, /\.pos-app-layout \.pos-checkout \.pos-cart \{ max-height: none; overflow: visible; \}/);
    assert.match(css, /\.pos-app-layout \.pos-checkout-actions[\s\S]*?position: sticky; bottom: 0/);
});

test("approved mockup images are retained as reference files", () => {
    const references = read("docs/design-reference/README.md");
    for (const file of ["admin.png", "pos.png", "login.png"]) {
        assert.match(references, new RegExp(file.replace(".", "\\.")));
    }
});

test("POS Arabic labels are readable and retired customer layout styles stay removed", () => {
    assert.match(layout, /نقطة البيع/);
    assert.match(pos, /حفظ واختيار العميل/);
    assert.doesNotMatch(pos, /ط§|ظ„|ظ†|آ·|أ—/);
    assert.doesNotMatch(css, /\.pos-order-context|\.pos-context-customer|\.pos-context-delivery/);
});
