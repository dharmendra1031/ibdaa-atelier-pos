import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const sql = read("database/013_payment_ledger.sql");
const service = read("src/IbdaaAtelier.Web/Services/PaymentService.cs");
const controller = read("src/IbdaaAtelier.Web/Controllers/PaymentsController.cs");
const page = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
const pos = read("src/IbdaaAtelier.Web/wwwroot/js/pos.js");

test("payment batches use numeric SQL identities and unique request IDs", () => {
    for (const name of ["RequestId", "PaymentBatchId", "PaymentId"])
        assert.match(sql, new RegExp(name + " BIGINT IDENTITY\\(1,1\\)"));
    assert.match(sql, /UNIQUE INDEX UX_PaymentBatches_Request/);
    assert.match(sql, /DB_NAME\(\) <> @ExpectedDatabase/);
    assert.doesNotMatch(sql, /GUID|NEWID\(|UNIQUEIDENTIFIER/i);
});

test("payment batch is atomic, replays same fingerprint and locks the order", () => {
    assert.match(service, /IsolationLevel\.Serializable/);
    assert.match(service, /UPDLOCK, HOLDLOCK/);
    assert.match(service, /SHA256\.HashData/);
    assert.match(service, /request\.Fingerprint != fingerprint/);
    assert.match(service, /order\.Total - paid/);
    assert.match(service, /amount > balance/);
    assert.match(service, /transaction\.CommitAsync/);
});

test("KWD three-decimal amounts and cash change are server-calculated", () => {
    assert.match(sql, /Amount DECIMAL\(18,3\)/);
    assert.match(sql, /Change = Tendered - Amount/);
    assert.match(service, /decimal\.Round\(line\.Amount, 3\)/);
    assert.match(service, /Change = tendered - line\.Amount/);
    assert.match(service, /line\.Method != "Cash"/);
    assert.match(service, /new PaymentBatch/);
    assert.match(service, /Payment\.Receive/);
});

test("cashier-only payment collection requires antiforgery", () => {
    assert.match(controller, /Authorize\(Roles = "Cashier"\)/);
    assert.match(controller, /ValidateAntiForgeryToken/);
    assert.match(page, /asp-antiforgery="true"/);
    assert.match(page, /id="posPaymentDialog"/);
    assert.match(page, /data-payment-record-url=/);
    assert.match(pos, /body: JSON\.stringify\(paymentPayload\)/);
    assert.match(controller, /public async Task<IActionResult> Record/);
});
