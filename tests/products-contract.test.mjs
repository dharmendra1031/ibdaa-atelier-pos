import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const controller = read("src/IbdaaAtelier.Web/Controllers/ProductsController.cs");
const form = read("src/IbdaaAtelier.Web/Models/Catalog/ProductForm.cs");
const view = read("src/IbdaaAtelier.Web/Views/Products/Editor.cshtml");
const list = read("src/IbdaaAtelier.Web/Views/Products/Index.cshtml");

test("product writes are restricted, antiforgery protected and audited", () => {
    assert.match(controller, /Authorize\(Roles = "Admin"\)/);
    assert.equal((controller.match(/ValidateAntiForgeryToken/g) || []).length, 3);
    assert.match(controller, /Product\.Create/);
    assert.match(controller, /Product\.Edit/);
    assert.match(controller, /Product\.Deactivate/);
    assert.match(controller, /BeginTransactionAsync/);
    assert.match(view, /asp-antiforgery="true"/);
    assert.match(list, /asp-antiforgery="true"/);
});

test("KWD prices and bilingual values are checked server-side", () => {
    assert.match(form, /\[Required, StringLength\(150\)\]\s+public string Name/);
    assert.match(form, /\[Required, StringLength\(150\)\]\s+public string NameAr/);
    assert.match(controller, /decimal\.Round\(form\.RegularPrice, 3\)/);
    assert.match(controller, /discount > form\.RegularPrice/);
    assert.match(controller, /decimal\.Round\(discount, 3\)/);
    assert.match(controller, /Choose an active category/);
});

test("product list is paged and inactive products remain for history", () => {
    assert.match(controller, /PageSize = 24/);
    assert.match(controller, /Skip\(\(page - 1\) \* PageSize\)\.Take\(PageSize\)/);
    assert.match(controller, /product\.IsActive = !product\.IsActive/);
    assert.doesNotMatch(controller, /ExecuteDeleteAsync|db\.Products\.Remove/);
    assert.match(list, /CultureInfo\.InvariantCulture/);
    assert.match(list, /asp-route-page/);
});
