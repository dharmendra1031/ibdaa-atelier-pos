import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const js = read("src/IbdaaAtelier.Web/wwwroot/js/pos.js");
const view = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
const css = read("src/IbdaaAtelier.Web/wwwroot/css/site.css");

test("POS offers two catalogue modes, retaining exactly two main checkout columns", () => {
    assert.match(view, /id="catalogueProducts"/);
    assert.match(view, /id="catalogueServices"[^>]*hidden/);
    assert.match(view, /id="showProducts"/);
    assert.match(view, /id="showServices"/);
    assert.match(view, /class="col-lg-7"/);
    assert.match(view, /class="col-lg-5"/);
    assert.match(js, /function switchCatalogue\(showServices\)/);
    assert.match(css, /#catalogueProducts\[hidden\]/);
});

test("cart lines are editable and edit preserves relevant size and measurements", () => {
    assert.match(js, /function editService\(index\)/);
    assert.match(js, /openProduct\(line\.itemId, index\)/);
    assert.match(js, /const current = index >= 0 \? items\[index\] : null/);
    assert.match(js, /current\.measurements\?\.find/);
    assert.match(js, /items\[editingIndex\] = nextItem/);
    assert.match(js, /items\[editingIndex\] = \{/);
    assert.match(js, /input\.value = String\(previous\.value\)/);
});

test("customer label is restored after refresh without early use of text helper", () => {
    const helper = js.indexOf("const text =");
    const saved = js.indexOf("sessionStorage.getItem(cartKey)");
    assert.ok(helper > 0 && helper < saved);
    assert.match(js, /customerLabel\.textContent = text\("Selected: "/);
});

test("order service and monetary contract are unchanged by UI refactor", () => {
    const order = read("src/IbdaaAtelier.Web/Services/PosOrderService.cs");
    assert.match(order, /UPDLOCK, HOLDLOCK/);
    assert.match(order, /transaction\.CommitAsync/);
    assert.match(js, /if \(!customerId\)/);
    assert.match(view, /Save unpaid order/);
});
