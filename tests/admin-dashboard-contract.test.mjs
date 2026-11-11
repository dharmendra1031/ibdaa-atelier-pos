import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");

const controller = read("src/IbdaaAtelier.Web/Controllers/AdminController.cs");
const model = read("src/IbdaaAtelier.Web/Models/Masters/AdminDashboardPage.cs");
const view = read("src/IbdaaAtelier.Web/Views/Admin/Index.cshtml");

test("dashboard is Admin-only and summarizes real persisted financial transactions", () => {
    assert.match(controller, /Authorize\(Roles = "Admin"\)/);
    assert.match(controller, /db\.OrderPayments\.AsNoTracking\(\)/);
    assert.match(controller, /db\.OrderRefunds\.AsNoTracking\(\)/);
    assert.match(controller, /db\.Orders\.AsNoTracking\(\)/);
    assert.match(controller, /db\.Customers\.CountAsync/);
    assert.doesNotMatch(controller, /new Random|DemoSales|HardcodedTotal/i);
});

test("dashboard groups dates in Kuwait and separates cash received from refund and outstanding", () => {
    assert.match(controller, /DateTime\.UtcNow\.AddHours\(3\)/);
    assert.match(controller, /AddHours\(-3\)/);
    assert.match(controller, /Status != "Cancelled"/);
    assert.match(controller, /totalReceivable - totalReceived \+ totalRefunded/);
    assert.match(controller, /received\.Sum/);
    assert.match(controller, /returned\.Sum/);
    assert.match(model, /public decimal Net => Received - Refunded/);
});

test("bilingual compact dashboard has bounded period filters and KWD precision", () => {
    assert.match(controller, /case "custom"/);
    assert.match(controller, /to\.Value <= today/);
    assert.match(controller, /DayNumber <= 366/);
    assert.match(view, /name="period"/);
    assert.match(view, /name="from"/);
    assert.match(view, /name="to"/);
    assert.match(view, /ToString\("0\.000", CultureInfo\.InvariantCulture\)/);
    assert.match(view, /isArabic/);
});
