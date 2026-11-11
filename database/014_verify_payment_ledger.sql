/* Approved TEST database only; read-only verification. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51103, 'Set the exact approved TEST database name.', 1;
IF COLUMNPROPERTY(OBJECT_ID(N'dbo.PaymentRequests'), 'RequestId', 'IsIdentity') <> 1
 OR COLUMNPROPERTY(OBJECT_ID(N'dbo.PaymentBatches'), 'PaymentBatchId', 'IsIdentity') <> 1
 OR COLUMNPROPERTY(OBJECT_ID(N'dbo.OrderPayments'), 'PaymentId', 'IsIdentity') <> 1
    THROW 51104, 'Payment identity keys missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PaymentBatches') AND name = N'UX_PaymentBatches_Request')
    THROW 51105, 'Unique payment request constraint missing.', 1;

SELECT COUNT_BIG(*) AS PaymentBatchCount FROM dbo.PaymentBatches;
SELECT COUNT_BIG(*) AS PaymentEntryCount FROM dbo.OrderPayments;
