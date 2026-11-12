import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const controller = read("src/IbdaaAtelier.Web/Controllers/ShopSettingsController.cs");
const schema = read("database/021_shop_settings.sql");
const singletonMigration = read("database/027_shop_settings_singleton.sql");
const footerMigration = read("database/031_invoice_receipt_footer.sql");
const context = read("src/IbdaaAtelier.Web/Data/AtelierDbContext.cs");
const view = read("src/IbdaaAtelier.Web/Views/ShopSettings/Index.cshtml");
const runner = read("tests/sql/run-smoke.sh");
const settingsVerifier = read("database/022_verify_shop_settings.sql");
const singletonVerifier = read("database/028_verify_shop_settings_singleton.sql");

test("settings use a single numeric identity row with no client seed data", () => {
    assert.match(schema, /SettingsId INT IDENTITY\(1,1\)/);
    assert.match(schema, /CK_ShopSettings_Singleton CHECK \(SettingsId = 1\)/);
    assert.match(schema, /DB_NAME\(\) <> @ExpectedDatabase/);
    assert.doesNotMatch(schema, /INSERT INTO|UNIQUEIDENTIFIER|NEWID\s*\(/i);
    assert.match(singletonMigration, /DROP CONSTRAINT CK_ShopSettings_Singleton/);
    assert.match(singletonMigration, /CREATE UNIQUE INDEX UX_ShopSettings_OneRow/);
    assert.match(footerMigration, /ALTER TABLE dbo\.ShopSettings ADD ReceiptFooter NVARCHAR\(500\) NULL/);
    assert.match(footerMigration, /ALTER TABLE dbo\.OrderInvoiceSnapshots ADD ReceiptFooter NVARCHAR\(500\) NULL/);
    assert.match(context, /DbSet<ShopSettings>/);
});

test("settings edits enforce Admin permissions, CSRF and audited transaction", () => {
    assert.match(controller, /Authorize\(Roles = "Admin"\)/);
    assert.match(controller, /ValidateAntiForgeryToken/);
    assert.match(controller, /IsolationLevel\.Serializable/);
    assert.match(controller, /UPDLOCK, HOLDLOCK/);
    assert.doesNotMatch(controller, /WHERE SettingsId = 1/);
    assert.match(controller, /ShopSettings\.Update/);
    assert.match(controller, /transaction\.CommitAsync/);
    assert.match(view, /asp-antiforgery="true"/);
});

test("isolated SQL CI includes guarded settings scripts and smoke test", () => {
    assert.match(runner, /021_shop_settings\.sql/);
    assert.match(runner, /022_verify_shop_settings\.sql/);
    assert.match(settingsVerifier, /CK_ShopSettings_Names/);
    assert.doesNotMatch(settingsVerifier, /CK_ShopSettings_Singleton'/);
    assert.match(singletonVerifier, /UX_ShopSettings_OneRow/);
    assert.match(runner, /shop-settings-smoke\.sql/);
    assert.match(runner, /031_invoice_receipt_footer\.sql/);
    assert.match(runner, /032_verify_invoice_receipt_footer\.sql/);
});

test("logo is validated against existing image headers and stored under a safe shop path", () => {
    const files = read("src/IbdaaAtelier.Web/Services/ProductImageFiles.cs");
    const newController = read("src/IbdaaAtelier.Web/Controllers/ShopSettingsController.cs");
    const editor = read("src/IbdaaAtelier.Web/Views/ShopSettings/Index.cshtml");
    assert.match(files, /MaxFileBytes = 5 \* 1024 \* 1024/);
    assert.match(newController, /images\.ReadAsync\(logo, ct\)/);
    assert.match(newController, /FileMode\.CreateNew/);
    assert.match(newController, /"uploads", "shop"/);
    assert.match(newController, /ShopSettings\.UpdateLogo/);
    assert.match(editor, /enctype="multipart\/form-data"/);
    assert.match(editor, /asp-action="UploadLogo"/);
});
