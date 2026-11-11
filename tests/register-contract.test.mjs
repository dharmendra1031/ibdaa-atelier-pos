import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const schema = read("database/025_cash_register.sql");
const service = read("src/IbdaaAtelier.Web/Services/CashRegisterService.cs");
const controller = read("src/IbdaaAtelier.Web/Controllers/RegisterController.cs");
const payments = read("src/IbdaaAtelier.Web/Services/PaymentService.cs");
const refunds = read("src/IbdaaAtelier.Web/Services/RefundService.cs");

test("register and cash expense records use numeric SQL identities and a single open session", () => {
    assert.match(schema, /SessionId BIGINT IDENTITY\(1,1\)/);
    assert.match(schema, /ExpenseId BIGINT IDENTITY\(1,1\)/);
    assert.match(schema, /RequestId BIGINT IDENTITY\(1,1\)/);
    assert.match(schema, /UX_CashRegisterSessions_Open/);
    assert.match(schema, /UX_CashExpenses_Request/);
    assert.match(schema, /DB_NAME\(\) <> @ExpectedDatabase/);
    assert.doesNotMatch(schema, /UNIQUEIDENTIFIER|NEWID\s*\(/i);
});

test("cash closing uses persisted linked transactions, refunds, expenses and an explicit variance", () => {
    assert.match(service, /OpeningCash \+ cashPayments - cashRefunds - expenses/);
    assert.match(service, /countedCash - expected/);
    assert.match(service, /ClosedAtUtc = DateTime\.UtcNow/);
    assert.match(service, /if \(session\.CountedCash != countedCash\)/);
    assert.match(service, /ApprovedByUserId = userId/);
    assert.match(payments, /payment\.RegisterSessionId = register\?\.SessionId/);
    assert.match(refunds, /RegisterSessionId = register\?\.SessionId/);
});

test("register expense actions require Admin, CSRF protection and numeric request retries", () => {
    assert.match(controller, /Authorize\(Roles = "Admin,Cashier"\)/);
    assert.match(controller, /\[HttpPost, Authorize\(Roles = "Admin"\), ValidateAntiForgeryToken\]/);
    assert.match(service, /request\.Fingerprint != fingerprint/);
    assert.match(service, /Existing|existing is not null/);
    assert.match(controller, /register\.CloseAsync/);
});
