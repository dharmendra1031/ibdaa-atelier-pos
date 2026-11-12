import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const js = read("src/IbdaaAtelier.Web/wwwroot/js/pos.js");
const view = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
const service = read("src/IbdaaAtelier.Web/Services/PaymentService.cs");

const start = js.indexOf("    function parseMills(");
const end = js.indexOf("    function updatePaymentPreview()", start);
assert.ok(start >= 0 && end > start, "Existing payment calculator must be present");

const setup = [
    "const text = en => en;",
    "const paymentMethod = method;",
    "const paymentAmount = { value: fields.amount ?? '' };",
    "const boundInvoiceId = 1;",
    "const boundInvoiceBalance = balance;",
    "const savedOrder = null;",
    "const orderTotal = () => 0;",
    "const document = { getElementById: id => ({ value: fields[id] ?? '' }) };"
].join("\n");
const calculate = new Function("method", "fields", "balance",
    setup + "\n" + js.slice(start, end) + "\nreturn paymentLines();");

test("Cash received above balance applies only due and records change", () => {
    const result = calculate("Cash", { amount: "100.000" }, 9.5);
    assert.equal(result.total, 9500);
    assert.equal(result.change, 90500);
    assert.deepEqual(result.lines, [{ method: "Cash", amount: 9.5, tendered: 100 }]);

    const ten = calculate("Cash", { amount: "10" }, 9.5);
    assert.equal(ten.total, 9500);
    assert.equal(ten.change, 500);
});

test("Cash partial payment leaves a balance, without inventing change", () => {
    const result = calculate("Cash", { amount: "5.000" }, 9.5);
    assert.equal(result.total, 5000);
    assert.equal(result.due - result.total, 4500);
    assert.equal(result.change, 0);
    assert.deepEqual(result.lines, [{ method: "Cash", amount: 5, tendered: 5 }]);
});

test("Cash and digital methods reject zero, malformed or over-limit amounts", () => {
    assert.throws(() => calculate("Cash", { amount: "0" }, 9.5));
    assert.throws(() => calculate("Cash", { amount: "-1" }, 9.5));
    assert.throws(() => calculate("Cash", { amount: "1.0000" }, 9.5));
    assert.throws(() => calculate("KNET", { amount: "100" }, 9.5));
    assert.throws(() => calculate("Card", { amount: "100" }, 9.5));
    assert.equal(calculate("KNET", { amount: "5.000" }, 9.5).total, 5000);
});

test("Split keeps separate cash tender and prevents excess applied payment", () => {
    const result = calculate("Split", {
        posSplitCash: "2.000", posSplitCashReceived: "10.000",
        posSplitKnet: "7.500", posSplitWamd: ""
    }, 9.5);
    assert.equal(result.total, 9500);
    assert.equal(result.change, 8000);
    assert.equal(result.lines.length, 2);
    assert.throws(() => calculate("Split", {
        posSplitCash: "5", posSplitKnet: "5"
    }, 9.5));
});

test("Cash popup uses one focused, selected field and keeps the server ledger", () => {
    assert.match(view, /id="posPaymentAmountLabel"/);
    assert.match(view, /id="posPaymentAmount"/);
    assert.doesNotMatch(view, /id="posCashReceived"/);
    assert.match(js, /paymentDialog\.showModal\(\);\s*if \(paymentMethod === "Cash" && !paymentPayload\) \{\s*paymentAmount\.focus\(\);\s*paymentAmount\.select\(\);/);
    assert.match(js, /method === "Cash" && paymentDialog\.open && !paymentPayload/);
    assert.match(js, /const applied = Math\.min\(due, received\)/);
    assert.match(js, /add\("Cash", \(applied \/ 1000\)\.toFixed\(3\), paymentAmount\.value\)/);
    assert.doesNotMatch(js, /orderId: boundInvoiceId,\s*orderId: boundInvoiceId/);
    assert.match(service, /Change = tendered - line\.Amount/);
    assert.match(service, /request\.Fingerprint != fingerprint/);
});
