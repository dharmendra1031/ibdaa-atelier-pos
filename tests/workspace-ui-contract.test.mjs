import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const load = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const layout = load("src/IbdaaAtelier.Web/Views/Shared/_Layout.cshtml");
const menu = load("src/IbdaaAtelier.Web/Views/Shared/_Navigation.cshtml");
const css = load("src/IbdaaAtelier.Web/wwwroot/css/site.css");
const dashboard = load("src/IbdaaAtelier.Web/Views/Admin/Index.cshtml");
const home = load("src/IbdaaAtelier.Web/Controllers/HomeController.cs");

test("one shared menu powers desktop sidebar and responsive mobile navigation", () => {
    assert.equal((layout.match(/partial name="_Navigation"/g) || []).length, 2);
    assert.match(layout, /class="app-sidebar"/);
    assert.match(layout, /class="mobile-menu/);
    assert.match(layout, /href="#mainContent"/);
    assert.match(css, /max-width: 1023\.98px/);
    assert.match(css, /\.app-layout \{[^}]*display: grid/);
    assert.match(css, /\.mobile-menu \{ display: block/);
});

test("cashier has sales links, administration links require Admin role", () => {
    assert.match(menu, /User\.IsInRole\("Admin"\)/);
    assert.match(menu, /asp-controller="Pos"/);
    assert.match(menu, /asp-controller="Orders"/);
    assert.match(menu, /asp-controller="Customers"/);
    assert.match(menu, /asp-controller="Products"/);
    assert.match(menu, /asp-controller="ShopSettings"/);
    assert.match(menu, /asp-controller="Reports"/);
});

test("dashboard keeps operational KPIs without repeating sidebar navigation", () => {
    assert.match(dashboard, /Model\.Collected/);
    assert.match(dashboard, /Model\.Outstanding/);
    assert.match(dashboard, /Model\.PendingDeliveries/);
    assert.doesNotMatch(dashboard, /dashboard-quick-links|Sizes \/ Measurements/);
    assert.match(dashboard, /admin-master-link/);
    assert.equal((dashboard.match(/asp-controller="Products" asp-action="Index"/g) || []).length, 1);
});

test("authenticated users land on their authorized operational workspace", () => {
    assert.match(home, /User\.Identity\?\.IsAuthenticated/);
    assert.match(home, /User\.IsInRole\("Admin"\) \? "Admin" : "Pos"/);
});
