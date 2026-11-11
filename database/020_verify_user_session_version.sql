/* Read-only TEST database verification. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51163, 'Set the exact approved TEST database name.', 1;
IF COL_LENGTH(N'dbo.Users', N'AuthVersion') IS NULL
    THROW 51164, 'AuthVersion column is missing.', 1;
IF EXISTS (SELECT 1 FROM dbo.Users WHERE AuthVersion < 1)
    THROW 51165, 'Invalid numeric session version found.', 1;
SELECT COUNT_BIG(*) AS UserCount FROM dbo.Users;
