import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");

test("dashboard sizing shortcut opens products before requiring a product ID", () => {
    const dashboard = read("src/IbdaaAtelier.Web/Views/Admin/Index.cshtml");
    assert.doesNotMatch(dashboard, /asp-controller="ProductSetup" asp-action="Index"/);
    assert.match(dashboard, /asp-controller="Products" asp-action="Index"/);
});

test("order and customer dates display Kuwait local time", () => {
    const orders = read("src/IbdaaAtelier.Web/Views/Orders/Index.cshtml");
    const details = read("src/IbdaaAtelier.Web/Views/Orders/Details.cshtml");
    const customers = read("src/IbdaaAtelier.Web/Views/Customers/Index.cshtml");
    assert.match(orders, /order\.CreatedAtUtc\.AddHours\(3\)/);
    assert.match(details, /receipt\.CreatedAtUtc\.AddHours\(3\)/);
    assert.match(details, /refund\.CreatedAtUtc\.AddHours\(3\)/);
    assert.match(details, /step\.ChangedAtUtc\.AddHours\(3\)/);
    assert.match(customers, /customer\.RegisteredAtUtc\.AddHours\(3\)/);
});

test("manually edited dashboard and report dates select custom period", () => {
    const admin = read("src/IbdaaAtelier.Web/Views/Admin/Index.cshtml");
    const reports = read("src/IbdaaAtelier.Web/Views/Reports/Index.cshtml");
    assert.match(admin, /dashboardPeriod\.value = "custom"/);
    assert.match(reports, /reportPeriod\.value = "custom"/);
});

test("WAMD is a display name without changing the stored Card payment method", () => {
    const details = read("src/IbdaaAtelier.Web/Views/Orders/Details.cshtml");
    const reports = read("src/IbdaaAtelier.Web/Views/Reports/Index.cshtml");
    const refund = read("src/IbdaaAtelier.Web/Views/Refunds/Issue.cshtml");
    assert.match(details, /receipt\.Method == "Card" \? "WAMD"/);
    assert.match(reports, /method\.Method == "Card" \? "WAMD"/);
    assert.match(refund, /method\.Key == "Card" \? "WAMD"/);
});

test("login has a single language selector and photo deletion requires confirmation", () => {
    const login = read("src/IbdaaAtelier.Web/Views/Auth/Login.cshtml");
    const layout = read("src/IbdaaAtelier.Web/Views/Shared/_Layout.cshtml");
    const gallery = read("src/IbdaaAtelier.Web/Views/ProductImages/Index.cshtml");
    const galleryJs = read("src/IbdaaAtelier.Web/wwwroot/js/product-gallery.js");
    assert.doesNotMatch(login, /auth-language/);
    assert.match(layout, /asp-controller="Language" asp-action="Set"/);
    assert.match(gallery, /class="image-delete-form"/);
    assert.match(galleryJs, /window\.confirm\(message\)/);
});
