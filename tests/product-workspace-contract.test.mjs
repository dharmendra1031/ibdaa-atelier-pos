import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const tabs = read("src/IbdaaAtelier.Web/Views/Shared/_ProductTabs.cshtml");
const editor = read("src/IbdaaAtelier.Web/Views/Products/Editor.cshtml");
const gallery = read("src/IbdaaAtelier.Web/Views/ProductImages/Index.cshtml");
const setup = read("src/IbdaaAtelier.Web/Views/ProductSetup/Index.cshtml");
const list = read("src/IbdaaAtelier.Web/Views/Products/Index.cshtml");

test("product pages share one focused workspace navigation", () => {
    for (const controller of ["Products", "ProductImages", "ProductSetup"])
        assert.match(tabs, new RegExp('asp-controller="' + controller + '"'));
    assert.match(editor, /partial name="_ProductTabs" model="Model.ProductId"/);
    assert.match(gallery, /partial name="_ProductTabs" model="Model.Product.ProductId"/);
    assert.match(setup, /partial name="_ProductTabs" model="Model.Product.ProductId"/);
    assert.match(editor, /Save the product first, then add photos and measurements/);
    assert.match(list, /Manage/);
});

test("sizing navigation stays inside the existing page with no new writes", () => {
    for (const id of ["productSizes", "productMeasurements", "sizeChart"])
        assert.match(setup, new RegExp('id="' + id + '"'));
    assert.match(setup, /href="#sizeChart"/);
    assert.match(setup, /asp-antiforgery="true"/);
});
