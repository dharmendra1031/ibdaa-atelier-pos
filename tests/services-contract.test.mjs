import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const load = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const controller = load("src/IbdaaAtelier.Web/Controllers/ServicesController.cs");
const schema = load("database/009_alteration_services.sql");
const context = load("src/IbdaaAtelier.Web/Data/AtelierDbContext.cs");

test("alterations are independent product types with numeric IDs", () => {
    assert.match(schema, /ServiceCategoryId INT IDENTITY\(1,1\)/);
    assert.match(schema, /ServiceId INT IDENTITY\(1,1\)/);
    assert.match(context, /DbSet<ServiceCategory>/);
    assert.match(context, /DbSet<AlterationService>/);
    assert.doesNotMatch(schema, /\bProductId\b|UNIQUEIDENTIFIER|NEWID\(/i);
});
test("service endpoints only allow Admin and require antiforgery on all writes", () => {
    assert.match(controller, /Authorize\(Roles = "Admin"\)/);
    assert.equal((controller.match(/ValidateAntiForgeryToken/g)||[]).length, 4);
    assert.match(controller, /BeginTransactionAsync/);
    assert.match(controller, /Service\.Create/);
    assert.match(controller, /ServiceCategory\.Deactivate/);
});
test("service price and duplicate validation are on the server", () => {
    assert.match(controller, /decimal\.Round\(form\.Price, 3\)/);
    assert.match(controller, /s\.Name == form\.Name/);
    assert.match(schema, /UNIQUE INDEX UX_AlterationServices_CategoryName/);
    assert.match(schema, /Price DECIMAL\(18,3\)/);
    assert.match(schema, /DB_NAME\(\) <> @ExpectedDatabase/);
});
test("inactive services are never hard deleted and results are paginated", () => {
    assert.match(controller, /service\.IsActive = !service\.IsActive/);
    assert.match(controller, /Skip\(\(page - 1\) \* PageSize\)/);
    assert.doesNotMatch(controller, /ExecuteDeleteAsync|\.Remove\(/);
});
