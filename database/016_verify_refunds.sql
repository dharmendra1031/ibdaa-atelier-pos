/* Approved TEST database only. Read-only check. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51123, 'Set the exact approved TEST database name.', 1;
IF COLUMNPROPERTY(OBJECT_ID(N'dbo.RefundRequests'), 'RequestId', 'IsIdentity') <> 1
 OR COLUMNPROPERTY(OBJECT_ID(N'dbo.OrderRefunds'), 'RefundId', 'IsIdentity') <> 1
    THROW 51124, 'Numeric refund keys missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.OrderRefunds') AND name = N'UX_OrderRefunds_Request')
    THROW 51125, 'Refund idempotency index missing.', 1;

SELECT COUNT_BIG(*) AS RefundCount FROM dbo.OrderRefunds;
