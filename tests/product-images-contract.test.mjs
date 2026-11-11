import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const controller = read("src/IbdaaAtelier.Web/Controllers/ProductImagesController.cs");
const storage = read("src/IbdaaAtelier.Web/Services/ProductImageFiles.cs");
const gallery = read("src/IbdaaAtelier.Web/Views/ProductImages/Index.cshtml");
const editor = read("src/IbdaaAtelier.Web/Views/Products/Editor.cshtml");

test("photo operations require Admin role and antiforgery", () => {
    assert.match(controller, /Authorize\(Roles = "Admin"\)/);
    assert.equal((controller.match(/ValidateAntiForgeryToken/g) || []).length, 4);
    assert.match(controller, /RequestSizeLimit\(6 \* 1024 \* 1024\)/);
    assert.match(gallery, /asp-antiforgery="true"/);
});

test("uploads validate type and size and never trust a supplied file path", () => {
    assert.match(storage, /MaxFileBytes = 5 \* 1024 \* 1024/);
    for (const type of ["image/jpeg", "image/png", "image/webp"])
        assert.ok(storage.includes(type));
    assert.match(storage, /Path\.GetExtension\(file\.FileName\)/);
    assert.match(storage, /productId\.ToString/);
    assert.match(storage, /imageId\.ToString/);
    assert.doesNotMatch(storage, /Guid\.|NewGuid\(|UUID|UNIQUEIDENTIFIER/);
});

test("gallery supports multiple pictures, primary, replacement and deletion", () => {
    for (const action of ["Upload", "Replace", "MakePrimary", "Delete"])
        assert.match(controller, new RegExp("Task<IActionResult> " + action + "\\("));
    assert.match(controller, /ExecuteUpdateAsync/);
    assert.match(controller, /ProductImage\.Replace/);
    assert.match(controller, /ProductImage\.Delete/);
    assert.match(controller, /ProductImage\.MakePrimary/);
    assert.match(gallery, /gallery-photo/);
    assert.match(gallery, /image-upload-input/);
    assert.match(editor, /partial name="_ProductTabs"/);
    assert.match(read("src/IbdaaAtelier.Web/Views/Shared/_ProductTabs.cshtml"), /asp-controller="ProductImages"/);
});

test("database and disk operations keep image rows and paths consistent", () => {
    assert.match(controller, /IsolationLevel\.Serializable/);
    assert.match(controller, /UPDLOCK, HOLDLOCK/);
    assert.match(controller, /transaction\.CommitAsync/);
    assert.match(storage, /FileMode\.CreateNew/);
    assert.match(storage, /Path\.GetFileName\(fileName\)/);
    assert.match(controller, /TryDelete\(productId, oldPath\)/);
});
