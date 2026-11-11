import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const controller = read("src/IbdaaAtelier.Web/Controllers/ShopSettingsController.cs");
const schema = read("database/021_shop_settings.sql");
const context = read("src/IbdaaAtelier.Web/Data/AtelierDbContext.cs");
const view = read("src/IbdaaAtelier.Web/Views/ShopSettings/Index.cshtml");
const runner = read("tests/sql/run-smoke.sh");

test("settings use a single numeric identity row with no client seed data", () => {
    assert.match(schema, /SettingsId INT IDENTITY\(1,1\)/);
    assert.match(schema, /CK_ShopSettings_Singleton CHECK \(SettingsId = 1\)/);
    assert.match(schema, /DB_NAME\(\) <> @ExpectedDatabase/);
    assert.doesNotMatch(schema, /INSERT INTO|UNIQUEIDENTIFIER|NEWID\s*\(/i);
    assert.match(context, /DbSet<ShopSettings>/);
});

test("settings edits enforce Admin permissions, CSRF and audited transaction", () => {
    assert.match(controller, /Authorize\(Roles = "Admin"\)/);
    assert.match(controller, /ValidateAntiForgeryToken/);
    assert.match(controller, /IsolationLevel\.Serializable/);
    assert.match(controller, /UPDLOCK, HOLDLOCK/);
    assert.match(controller, /ShopSettings\.Update/);
    assert.match(controller, /transaction\.CommitAsync/);
    assert.match(view, /asp-antiforgery="true"/);
});

test("isolated SQL CI includes guarded settings scripts and smoke test", () => {
    assert.match(runner, /021_shop_settings\.sql/);
    assert.match(runner, /022_verify_shop_settings\.sql/);
    assert.match(runner, /shop-settings-smoke\.sql/);
});
