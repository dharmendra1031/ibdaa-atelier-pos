/* Read-only verification in approved TEST database. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51143, 'Set the exact approved TEST database name.', 1;
IF COL_LENGTH(N'dbo.OrderStatusHistory', N'Remarks') IS NULL
    THROW 51144, 'Status remarks column missing.', 1;
IF COLUMNPROPERTY(OBJECT_ID(N'dbo.OrderStatusHistory'), 'HistoryId', 'IsIdentity') <> 1
    THROW 51145, 'Status history must have a numeric identity.', 1;
SELECT COUNT_BIG(*) AS StatusEventCount FROM dbo.OrderStatusHistory;
