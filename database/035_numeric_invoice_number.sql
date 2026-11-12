/* Run manually against an approved, backed-up TEST database.
   Existing order IDs stay unchanged; no new invoice sequence is created. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51320, 'Set the exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL OR
   COLUMNPROPERTY(OBJECT_ID(N'dbo.Orders'), 'OrderId', 'IsIdentity') <> 1
    THROW 51321, 'Numeric order identity is required.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Orders') AND name = N'OrderNumber')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Orders') AND name = N'UX_Orders_Number')
        DROP INDEX UX_Orders_Number ON dbo.Orders;

    ALTER TABLE dbo.Orders DROP COLUMN OrderNumber;
END;

COMMIT TRANSACTION;
SELECT N'Numeric invoice numbering uses existing OrderId; no rows changed.' AS Result;
