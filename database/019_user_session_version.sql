/* Approved TEST database only: invalidate active cookies after password reset. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51160, 'Set the exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
    THROW 51161, 'Users table does not exist.', 1;
IF COL_LENGTH(N'dbo.Users', N'AuthVersion') IS NOT NULL
    THROW 51162, 'AuthVersion column already exists.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;
ALTER TABLE dbo.Users ADD AuthVersion INT NOT NULL
    CONSTRAINT DF_Users_AuthVersion DEFAULT (1);
COMMIT TRANSACTION;
SELECT N'Numeric session version enabled.' AS Result;
