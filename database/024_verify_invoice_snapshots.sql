/* Read-only invoice snapshot verification in isolated TEST database. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51103, 'Set the exact approved TEST database name.', 1;
IF COLUMNPROPERTY(OBJECT_ID(N'dbo.OrderInvoiceSnapshots'),
    'InvoiceSnapshotId', 'IsIdentity') <> 1
    THROW 51104, 'Invoice snapshot IDENTITY key missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.OrderInvoiceSnapshots')
    AND name = N'UX_OrderInvoiceSnapshots_Order')
    THROW 51105, 'Invoice snapshot order uniqueness missing.', 1;
SELECT COUNT_BIG(*) AS InvoiceSnapshotCount FROM dbo.OrderInvoiceSnapshots;
