import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const root = new URL("../", import.meta.url);
const read = path => readFileSync(new URL(path, root), "utf8");

test("mobile lookup uses the same normalizer as customer save", () => {
    const controller = read("src/IbdaaAtelier.Web/Controllers/CustomersController.cs");
    assert.match(controller, /var mobileSearch = PhoneNumbers\.Normalize\(search\);/);
    assert.match(controller, /c\.Mobile\.Contains\(mobileSearch\)/);
    assert.doesNotMatch(controller, /c\.Mobile\.Contains\(search\)/);
});

test("product category changes cannot orphan size chart definitions", () => {
    const controller = read("src/IbdaaAtelier.Web/Controllers/ProductsController.cs");
    assert.match(controller, /form\.CategoryId != product\.CategoryId/);
    assert.match(controller, /oldCategoryValues/);
    assert.match(controller, /overlappingFields/);
    assert.match(controller, /ModelState\.AddModelError\(nameof\(form\.CategoryId\)/);
});

test("duplicate product and category measurements cannot silently collide", () => {
    const controller = read("src/IbdaaAtelier.Web/Controllers/ProductSetupController.cs");
    assert.match(controller, /productCategoryId/);
    assert.match(controller, /x\.ProductId\.HasValue/);
    assert.match(controller, /x\.IsActive/);
});

test("category cannot be disabled while active products remain", () => {
    const controller = read("src/IbdaaAtelier.Web/Controllers/CategoriesController.cs");
    assert.match(controller, /db\.Products\.AnyAsync\(item => item\.CategoryId == id && item\.IsActive/);
});
