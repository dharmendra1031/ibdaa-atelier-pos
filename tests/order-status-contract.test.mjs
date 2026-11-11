import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const controller = read("src/IbdaaAtelier.Web/Controllers/OrderStatusController.cs");
const service = read("src/IbdaaAtelier.Web/Services/OrderStatusService.cs");
const sql = read("database/017_order_status_remarks.sql");
const view = read("src/IbdaaAtelier.Web/Views/Orders/Details.cshtml");

test("status and cancellation edits are admin-only and antiforgery protected", () => {
    assert.match(controller, /Authorize\(Roles = "Admin"\)/);
    assert.match(controller, /ValidateAntiForgeryToken/);
    assert.match(service, /IsolationLevel\.Serializable/);
    assert.match(service, /UPDLOCK, HOLDLOCK/);
    assert.match(service, /Order\.Cancel/);
    assert.match(service, /Order\.StatusChange/);
    assert.match(view, /asp-controller="OrderStatus"/);
    assert.match(view, /asp-antiforgery="true"/);
});

test("status transitions are sequential and terminal states cannot be changed", () => {
    for (const state of ["New", "Confirmed", "In Processing", "Ready for Delivery"])
        assert.ok(service.includes('"' + state + '" => ['));
    assert.match(service, /if \(!Next\(order\.Status\)\.Contains/);
    assert.doesNotMatch(service, /"Delivered" => \[/);
    assert.doesNotMatch(service, /"Cancelled" => \[/);
});

test("cancellation requires a reason and zero net receipts", () => {
    assert.match(service, /form\.Remarks\?\.Length \?\? 0/);
    assert.match(service, /if \(received != refunded\)/);
    assert.match(service, /Refund all received payments before cancelling/);
});

test("history stores remarks while preserving numeric history IDs", () => {
    assert.match(sql, /ADD Remarks NVARCHAR\(500\) NULL/);
    assert.match(sql, /DB_NAME\(\) <> @ExpectedDatabase/);
    assert.match(service, /OldStatus = previous/);
    assert.match(service, /NewStatus = form\.Status/);
    assert.match(service, /Remarks = form\.Remarks/);
});
