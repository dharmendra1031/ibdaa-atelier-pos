/* Read-only verification in approved TEST database only. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51042, 'Set the exact approved TEST database name.', 1;
IF COLUMNPROPERTY(OBJECT_ID(N'dbo.Customers'), 'CustomerId', 'IsIdentity') <> 1
    THROW 51043, 'Customers requires BIGINT IDENTITY.', 1;
SELECT COUNT_BIG(*) AS CustomerCount FROM dbo.Customers;
