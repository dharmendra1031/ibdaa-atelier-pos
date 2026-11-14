import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const layout = read("src/IbdaaAtelier.Web/Views/Shared/_Layout.cshtml");
const pos = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
const css = read("src/IbdaaAtelier.Web/wwwroot/css/site.css");

test("POS renders a single cashier header without duplicate navigation sections", () => {
    assert.match(layout, /class="workspace-header-title pos-header-title"/);
    assert.match(layout, /<details class="pos-quick-menu">/);
    assert.match(layout, /<nav class="pos-quick-menu-links"/);
    assert.match(layout, /asp-controller="Orders" asp-action="Index"/);
    assert.match(layout, /asp-controller="Register" asp-action="Index"/);
    assert.match(layout, /asp-controller="Customers" asp-action="Index"/);
    assert.match(css, /\.pos-quick-menu-links \{/);
    assert.match(layout, /@if \(controller != "Pos"\)\s*\{\s*<aside class="app-sidebar"/);
    assert.match(layout, /@if \(controller != "Pos"\)\s*\{\s*<details class="mobile-menu/);
    assert.match(layout, /@if \(controller != "Pos"\)\s*\{\s*<footer class="site-footer"/);
    assert.doesNotMatch(pos, /pos-heading|pos-shortcuts/);
    assert.doesNotMatch(css, /pos-heading|pos-shortcuts/);
});

test("POS retains only the necessary billing work areas and actions", () => {
    assert.equal((pos.match(/class="col-lg-7"/g) || []).length, 1);
    assert.equal((pos.match(/class="col-lg-5"/g) || []).length, 1);
    for (const id of ["openFindInvoice", "posChooseUnpaid", "posChoosePaid",
                      "saveOrderButton", "openQuickCustomer", "boundInvoicePayment"]) {
        assert.equal((pos.match(new RegExp('id="' + id + '"', "g")) || []).length, 1,
            id + " should have exactly one action");
    }
    assert.doesNotMatch(pos, /asp-controller="Orders"|asp-controller="Register"|pos-shortcuts/);
});

test("management pages keep the shared navigation on desktop and mobile", () => {
    assert.equal((layout.match(/partial name="_Navigation"/g) || []).length, 2);
    assert.match(layout, /class="app-sidebar"/);
    assert.match(layout, /class="mobile-menu/);
    assert.match(layout, /class="workspace-breadcrumb/);
});
