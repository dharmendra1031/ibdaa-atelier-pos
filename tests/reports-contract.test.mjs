import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const controller = read("src/IbdaaAtelier.Web/Controllers/ReportsController.cs");
const view = read("src/IbdaaAtelier.Web/Views/Reports/Index.cshtml");
const menu = read("src/IbdaaAtelier.Web/Views/Shared/_Navigation.cshtml");

test("report access is Admin-only and period is bounded in Kuwait-local dates", () => {
    assert.match(controller, /Authorize\(Roles = "Admin"\)/);
    assert.match(controller, /DateTime\.UtcNow\.AddHours\(3\)/);
    assert.match(controller, /AddHours\(-3\)/);
    assert.match(controller, /to\.Value <= today/);
    assert.match(controller, /DayNumber <= 366/);
    assert.match(menu, /asp-controller="Reports"/);
});

test("report data derives from saved orders, payment ledger and refunds", () => {
    assert.match(controller, /db\.Orders\.AsNoTracking\(\)/);
    assert.match(controller, /db\.OrderPayments\.AsNoTracking\(\)/);
    assert.match(controller, /db\.OrderRefunds\.AsNoTracking\(\)/);
    assert.match(controller, /Status != "Cancelled"/);
    assert.match(controller, /receivable - lifetimeReceived \+ lifetimeRefunded/);
    assert.match(controller, /GroupBy\(p => p\.Method\)/);
    assert.match(controller, /GroupBy\(r => r\.Method\)/);
    assert.match(controller, /OrderItems\.AsNoTracking\(\)/);
});

test("bilingual report labels outstanding and exposes bounded CSV sales export", () => {
    assert.match(view, /name="period"/);
    assert.match(view, /name="from"/);
    assert.match(view, /name="to"/);
    assert.match(view, /all active orders/);
    assert.match(view, /Additional detailed reports and PDF\/Excel export remain pending/);
    assert.match(view, /asp-action="ExportSales"/);
    assert.match(controller, /public async Task ExportSales/);
    assert.match(controller, /Take\(250\)/);
    assert.match(controller, /CsvCell/);
    assert.match(controller, /Response\.Headers\.CacheControl = "no-store"/);
    assert.match(controller, /Invoice Number,Order Date \(Kuwait\),Customer,Mobile,Status/);
    assert.doesNotMatch(controller, /Order ID,Order Number,Order Date/);
    assert.doesNotMatch(controller, /order\.OrderNumber/);
    assert.match(view, /Product & service totals before order discounts/);
    assert.match(view, /CultureInfo\.InvariantCulture/);
    assert.doesNotMatch(view, /asp-action="Pdf"/);
});
