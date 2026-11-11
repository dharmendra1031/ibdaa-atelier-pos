import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const sql = read("database/015_refunds.sql");
const service = read("src/IbdaaAtelier.Web/Services/RefundService.cs");
const controller = read("src/IbdaaAtelier.Web/Controllers/RefundsController.cs");
const payments = read("src/IbdaaAtelier.Web/Services/PaymentService.cs");
const orderPage = read("src/IbdaaAtelier.Web/Controllers/OrdersController.cs");

test("refund ledger uses numeric keys, one refund per numeric request", () => {
    assert.match(sql, /RequestId BIGINT IDENTITY\(1,1\)/);
    assert.match(sql, /RefundId BIGINT IDENTITY\(1,1\)/);
    assert.match(sql, /UNIQUE INDEX UX_OrderRefunds_Request/);
    assert.match(sql, /DB_NAME\(\) <> @ExpectedDatabase/);
    assert.doesNotMatch(sql, /GUID|UNIQUEIDENTIFIER|NEWID\(/i);
});

test("refunds are admin-only, CSRF-protected, audited and idempotent", () => {
    assert.match(controller, /Authorize\(Roles = "Admin"\)/);
    assert.match(controller, /ValidateAntiForgeryToken/);
    assert.match(service, /UPDLOCK, HOLDLOCK/);
    assert.match(service, /IsolationLevel\.Serializable/);
    assert.match(service, /request\.Fingerprint != fingerprint/);
    assert.match(service, /Payment\.Refund/);
    assert.match(service, /transaction\.CommitAsync/);
});

test("server rejects over-refunds for the original payment method", () => {
    assert.match(service, /p\.Method == form\.Method/);
    assert.match(service, /r\.Method == form\.Method/);
    assert.match(service, /form\.Amount > received - refunded/);
    assert.match(service, /form\.Amount != decimal\.Round\(form\.Amount, 3\)/);
    assert.match(service, /form\.Reason\.Length is < 10 or > 500/);
});

test("payment balance uses net receipts and order details display refunds", () => {
    assert.match(payments, /return received - refunded/);
    assert.match(orderPage, /var paid = grossPaid - refunded/);
    assert.match(orderPage, /Balance = order\.Status == "Cancelled" \? 0m/);
});
