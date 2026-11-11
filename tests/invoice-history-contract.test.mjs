import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");

const sql = read("database/023_invoice_snapshots.sql");
const save = read("src/IbdaaAtelier.Web/Services/PosOrderService.cs");
const mapper = read("src/IbdaaAtelier.Web/Services/InvoiceReportService.cs");
const shop = read("src/IbdaaAtelier.Web/Controllers/ShopSettingsController.cs");
const runner = read("tests/sql/run-smoke.sh");
const project = read("src/IbdaaAtelier.Web/IbdaaAtelier.Web.csproj");
const orders = read("src/IbdaaAtelier.Web/Views/Orders/Details.cshtml");

test("immutable invoice branding uses one numeric snapshot row per order", () => {
    assert.match(sql, /InvoiceSnapshotId BIGINT IDENTITY\(1,1\)/);
    assert.match(sql, /CONSTRAINT UX_OrderInvoiceSnapshots_Order UNIQUE \(OrderId\)/);
    assert.match(sql, /DB_NAME\(\) <> @ExpectedDatabase/);
    assert.match(save, /db\.OrderInvoiceSnapshots\.Add/);
    assert.match(save, /await transaction\.CommitAsync/);
    assert.match(mapper, /db\.OrderInvoiceSnapshots\.AsNoTracking\(\)/);
    assert.match(mapper, /savedBrand\?\.BusinessName/);
    assert.match(mapper, /savedBrand\?\.LogoPath/);
    assert.doesNotMatch(sql, /uniqueidentifier|NEWID/i);
});

test("old uploaded logos remain on disk so past invoices can be reprinted", () => {
    assert.match(shop, /saved invoice snapshots may still refer/);
    assert.doesNotMatch(shop, /File\.Delete\(oldFullPath\)/);
    assert.match(runner, /023_invoice_snapshots\.sql/);
    assert.match(runner, /024_verify_invoice_snapshots\.sql/);
    assert.match(read("tests/sql/order-schema-smoke.sql"), /INSERT dbo\.OrderInvoiceSnapshots/);
});

test("RDLC print actions and patched cryptography dependencies are wired", () => {
    assert.match(orders, /asp-controller="Invoices"/);
    assert.match(orders, /asp-route-paper="80mm"/);
    assert.match(orders, /asp-route-paper="a4"/);
    assert.match(project, /System\.Security\.Cryptography\.Xml" Version="10\.0\.13"/);
    assert.match(read("src/IbdaaAtelier.Web/Reports/DataSets/InvoiceDataSet.xsd"), /name="ShopEmail"/);
});
