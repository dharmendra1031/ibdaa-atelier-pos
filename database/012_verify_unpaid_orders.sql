/* Read-only check in the approved TEST database only. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51063, 'Set exact approved TEST database name.', 1;

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.OrderRequests'), 'RequestId', 'IsIdentity') <> 1
 OR COLUMNPROPERTY(OBJECT_ID(N'dbo.Orders'), 'OrderId', 'IsIdentity') <> 1
 OR COLUMNPROPERTY(OBJECT_ID(N'dbo.OrderItems'), 'OrderItemId', 'IsIdentity') <> 1
 OR COLUMNPROPERTY(OBJECT_ID(N'dbo.OrderItemMeasurements'), 'OrderMeasurementId', 'IsIdentity') <> 1
 OR COLUMNPROPERTY(OBJECT_ID(N'dbo.OrderStatusHistory'), 'HistoryId', 'IsIdentity') <> 1
    THROW 51064, 'Order identity keys are missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Orders') AND name = N'UX_Orders_RequestId')
    THROW 51065, 'Unique order request index is missing.', 1;

SELECT COUNT_BIG(*) AS OrderCount FROM dbo.Orders;
SELECT COUNT_BIG(*) AS OrderItemCount FROM dbo.OrderItems;
