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

test("bilingual report explicitly labels global outstanding and partial export coverage", () => {
    assert.match(view, /name="period"/);
    assert.match(view, /name="from"/);
    assert.match(view, /name="to"/);
    assert.match(view, /all active orders/);
    assert.match(view, /Detailed reports and exports are not yet available/);
    assert.match(view, /CultureInfo\.InvariantCulture/);
    assert.doesNotMatch(view, /asp-action="Export"|asp-action="Pdf"/);
});
