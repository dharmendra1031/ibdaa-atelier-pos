import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const controller = read("src/IbdaaAtelier.Web/Controllers/ProductSetupController.cs");
const sql = read("database/005_measurement_uniqueness.sql");
const view = read("src/IbdaaAtelier.Web/Views/ProductSetup/Index.cshtml");

test("numeric size and measurement definitions are admin-only and antiforgery protected", () => {
    assert.match(controller, /Authorize\(Roles = "Admin"\)/);
    assert.equal((controller.match(/ValidateAntiForgeryToken/g) || []).length, 5);
    assert.match(controller, /Measurement\.Create/);
    assert.match(controller, /SizeChart\.Save/);
    assert.match(view, /asp-antiforgery="true"/);
});

test("chart values cannot cross product/category boundaries", () => {
    assert.match(controller, /s\.SizeId == sizeId && s\.ProductId == productId && s\.IsActive/);
    assert.match(controller, /m\.ProductId == productId \|\| m\.CategoryId == product\.CategoryId/);
    assert.match(controller, /value != decimal\.Round\(value, 2\)/);
    assert.match(sql, /WHERE ProductId IS NOT NULL/);
    assert.match(sql, /WHERE CategoryId IS NOT NULL/);
});

test("measurement fields remain configurable and bilingual", () => {
    assert.match(controller, /nameAr\.Length > 100/);
    assert.match(controller, /unit is not \("cm" or "in"\)/);
    assert.match(controller, /IsRequired = isRequired/);
    assert.match(controller, /DisplayOrder = displayOrder/);
    assert.doesNotMatch(controller, /Remove\(|ExecuteDeleteAsync/);
});
