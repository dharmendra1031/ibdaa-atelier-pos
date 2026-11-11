import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const sql = read("database/011_unpaid_orders.sql");
const service = read("src/IbdaaAtelier.Web/Services/PosOrderService.cs");
const builder = read("src/IbdaaAtelier.Web/Services/OrderLineBuilder.cs");
const controller = read("src/IbdaaAtelier.Web/Controllers/PosController.cs");
const pos = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
const js = read("src/IbdaaAtelier.Web/wwwroot/js/pos.js");

test("order/request/item/measurement/status keys are numeric SQL IDENTITY", () => {
    for (const name of ["RequestId", "OrderId", "OrderItemId", "OrderMeasurementId", "HistoryId"])
        assert.match(sql, new RegExp(name + " BIGINT IDENTITY\\(1,1\\)"));
    assert.match(sql, /DB_NAME\(\) <> @ExpectedDatabase/);
    assert.match(sql, /UNIQUE INDEX UX_Orders_RequestId/);
    assert.match(sql, /OrderNumber AS/);
    assert.doesNotMatch(sql, /NEWID\(|UNIQUEIDENTIFIER|MAX\(OrderId\)/i);
});

test("product and service lines keep separate FKs and original details", () => {
    assert.match(sql, /CK_OrderItems_Type/);
    assert.match(sql, /FK_OrderItems_Product/);
    assert.match(sql, /FK_OrderItems_Service/);
    assert.match(sql, /UQ_OrderMeasurements_ItemDefinition/);
    assert.match(builder, /product\.DiscountPrice \?\? product\.RegularPrice/);
    assert.match(builder, /Name = definition\.Name/);
    assert.match(builder, /IsRequired && !submitted\.Any/);
    assert.match(builder, /decimal\.Round\(measure\.Value, 2\)/);
});

test("server uses database row locking, request fingerprint and an atomic order save", () => {
    assert.match(service, /IsolationLevel\.Serializable/);
    assert.match(service, /UPDLOCK, HOLDLOCK/);
    assert.match(service, /SHA256\.HashData/);
    assert.match(service, /request\.Fingerprint != fingerprint/);
    assert.match(service, /OrderStatusChange/);
    assert.match(service, /transaction\.CommitAsync/);
    assert.match(service, /CustomerName = customer\.Name/);
});

test("POS is customer-first and only saves an unpaid order at this stage", () => {
    assert.match(controller, /Authorize\(Roles = "Admin,Cashier"\)/);
    assert.equal((controller.match(/ValidateAntiForgeryToken/g) || []).length, 2);
    assert.match(pos, /col-lg-7/);
    assert.match(pos, /col-lg-5/);
    assert.match(pos, /Save unpaid order/);
    assert.match(js, /if \(!customerId\)/);
    assert.match(js, /sessionStorage\.setItem\(pendingKey/);
    assert.match(js, /Changing customer clears the cart/);
    assert.doesNotMatch(controller, /DbSet<Payment>|SavePayment|Refund/);
});
