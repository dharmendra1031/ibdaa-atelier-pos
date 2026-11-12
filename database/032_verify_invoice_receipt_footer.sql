/* Read-only verification on the approved TEST database. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51303, 'Set the approved TEST database name.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.ShopSettings') AND name = N'ReceiptFooter' AND max_length = 1000)
    THROW 51304, 'Shop receipt footer column missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.OrderInvoiceSnapshots') AND name = N'ReceiptFooter' AND max_length = 1000)
    THROW 51305, 'Invoice snapshot receipt footer column missing.', 1;
SELECT N'Invoice footer schema verified.' AS Result;
