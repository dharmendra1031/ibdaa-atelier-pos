import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const read = file => readFileSync(new URL("../" + file, import.meta.url), "utf8");
const view = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
const css = read("src/IbdaaAtelier.Web/wwwroot/css/site.css");
const js = read("src/IbdaaAtelier.Web/wwwroot/js/pos.js");

test("POS retains two content areas with a wider responsive work surface", () => {
    assert.match(view, /class="col-lg-7"/);
    assert.match(view, /class="col-lg-5"/);
    assert.match(css, /\.app-layout \{ max-width: 1700px/);
    assert.match(css, /row-cols-xxl-4|pos-product-grid/);
});

test("missing uploaded photos are clearly marked rather than presented as product photos", () => {
    assert.match(view, /pos-missing-photo/);
    assert.match(view, /No photo yet/);
    assert.match(view, /product.ImagePath/);
    assert.doesNotMatch(view, /src="https?:\/\//);
});

test("save remains unavailable until a customer and cart are selected", () => {
    assert.match(view, /id="saveOrderButton"[^>]*disabled/);
    assert.match(js, /saveButton\.disabled = saving \|\| completed \|\| !customerId \|\| items\.length === 0/);
    assert.match(js, /saveButton\.disabled = true/);
    assert.match(js, /customerAbort\?\.abort\(\);\s*customerResults\.replaceChildren\(\);\s*renderCart\(\)/);
});

test("idempotent server-side unpaid order save remains unchanged", () => {
    const server = read("src/IbdaaAtelier.Web/Services/PosOrderService.cs");
    assert.match(server, /IsolationLevel\.Serializable/);
    assert.match(server, /UPDLOCK, HOLDLOCK/);
    assert.match(server, /request\.Fingerprint != fingerprint/);
});
