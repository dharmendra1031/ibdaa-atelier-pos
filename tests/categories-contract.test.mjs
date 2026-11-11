import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const controller = read("src/IbdaaAtelier.Web/Controllers/CategoriesController.cs");
const form = read("src/IbdaaAtelier.Web/Models/Catalog/CategoryForm.cs");
const editor = read("src/IbdaaAtelier.Web/Views/Categories/Editor.cshtml");
const index = read("src/IbdaaAtelier.Web/Views/Categories/Index.cshtml");

test("category mutations are admin-only with antiforgery and audit", () => {
    assert.match(controller, /Authorize\(Roles = "Admin"\)/);
    assert.equal((controller.match(/ValidateAntiForgeryToken/g) || []).length, 3);
    assert.match(controller, /AddAudit\("Category\.Create"/);
    assert.match(controller, /AddAudit\("Category\.Edit"/);
    assert.match(controller, /Category\.Deactivate/);
    assert.match(controller, /BeginTransactionAsync/);
    assert.match(editor, /asp-antiforgery="true"/);
    assert.match(index, /asp-antiforgery="true"/);
});

test("bilingual categories reject missing names, duplicates and nested hierarchy", () => {
    assert.match(form, /\[Required, StringLength\(100\)\]\s+public string Name/);
    assert.match(form, /\[Required, StringLength\(100\)\]\s+public string NameAr/);
    assert.match(controller, /item\.ParentCategoryId == null && item\.IsActive/);
    assert.match(controller, /item\.Name == form\.Name/);
    assert.match(controller, /subcategory|subcategories/i);
    assert.match(controller, /DisplayOrder/);
});

test("category list uses server paging and preserves query", () => {
    assert.match(controller, /PageSize = 25/);
    assert.match(controller, /CountAsync\(ct\)/);
    assert.match(controller, /Skip\(\(page - 1\) \* PageSize\)\.Take\(PageSize\)/);
    assert.match(index, /asp-route-search="@Model\.Search"/);
    assert.match(index, /asp-route-page/);
});

test("category deactivation is not a delete", () => {
    assert.match(controller, /category\.IsActive = !category\.IsActive/);
    assert.doesNotMatch(controller, /Remove\(|ExecuteDeleteAsync|DeleteCategory/i);
});
