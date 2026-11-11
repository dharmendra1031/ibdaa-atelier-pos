import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const customer = read("src/IbdaaAtelier.Web/Controllers/CustomersController.cs");
const phones = read("src/IbdaaAtelier.Web/Models/Masters/PhoneNumbers.cs");
const sql = read("database/007_customer_masters.sql");
const audit = read("database/003_user_authentication.sql");

test("customer IDs are numeric BIGINT identity, with unique normalized mobile", () => {
    assert.match(sql, /CustomerId BIGINT IDENTITY\(1,1\)/);
    assert.match(sql, /UX_Customers_Mobile/);
    assert.match(audit, /EntityId BIGINT NULL/);
    assert.doesNotMatch(sql, /NEWID\(|UNIQUEIDENTIFIER/);
});

test("mobile search and duplicate checks are enforced server-side", () => {
    assert.match(customer, /Authorize\(Roles = "Admin,Cashier"\)/);
    assert.match(customer, /ValidateAntiForgeryToken/);
    assert.match(customer, /PhoneNumbers.Normalize/);
    assert.match(customer, /c\.Mobile == mobile && c\.CustomerId != form\.CustomerId/);
    assert.match(phones, /digits\.StartsWith\("965"/);
});

test("customer creation and edit are audited and never hard-delete", () => {
    assert.match(customer, /BeginTransactionAsync/);
    assert.match(customer, /Customer\.Create/);
    assert.match(customer, /Customer\.Edit/);
    assert.doesNotMatch(customer, /ExecuteDeleteAsync|\.Remove\(/);
});
