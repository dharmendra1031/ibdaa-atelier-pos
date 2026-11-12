/* Manual migration for an approved, backed-up TEST database only. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51300, 'Set the approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.ShopSettings', N'U') IS NULL OR OBJECT_ID(N'dbo.OrderInvoiceSnapshots', N'U') IS NULL
    THROW 51301, 'Shop settings and invoice snapshot tables are required.', 1;
IF COL_LENGTH(N'dbo.ShopSettings', 'ReceiptFooter') IS NOT NULL
   OR COL_LENGTH(N'dbo.OrderInvoiceSnapshots', 'ReceiptFooter') IS NOT NULL
    THROW 51302, 'Receipt footer migration has already been applied.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;
ALTER TABLE dbo.ShopSettings ADD ReceiptFooter NVARCHAR(500) NULL;
ALTER TABLE dbo.OrderInvoiceSnapshots ADD ReceiptFooter NVARCHAR(500) NULL;
COMMIT TRANSACTION;
SELECT N'Invoice footer settings and historical snapshot columns added.' AS Result;
