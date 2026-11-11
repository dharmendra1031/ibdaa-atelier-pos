/* Approved TEST database only. Add audit remarks to existing order status history. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51140, 'Set the exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.OrderStatusHistory', N'U') IS NULL
    THROW 51141, 'Order status history table does not exist.', 1;
IF COL_LENGTH(N'dbo.OrderStatusHistory', N'Remarks') IS NOT NULL
    THROW 51142, 'Order status remarks already exist.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;
ALTER TABLE dbo.OrderStatusHistory ADD Remarks NVARCHAR(500) NULL;
COMMIT TRANSACTION;
SELECT N'Order status history remarks enabled.' AS Result;
