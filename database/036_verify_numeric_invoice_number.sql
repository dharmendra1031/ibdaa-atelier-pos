/* Read-only numeric invoice numbering verification on approved TEST database. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51322, 'Set the exact approved TEST database name.', 1;
IF COLUMNPROPERTY(OBJECT_ID(N'dbo.Orders'), 'OrderId', 'IsIdentity') <> 1
    THROW 51323, 'Numeric invoice identity is missing.', 1;
IF EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Orders') AND name = N'OrderNumber')
    THROW 51324, 'Legacy prefixed invoice column still exists.', 1;

SELECT COUNT_BIG(*) AS InvoicesChecked, MIN(OrderId) AS FirstInvoiceId,
    MAX(OrderId) AS LastInvoiceId FROM dbo.Orders;
