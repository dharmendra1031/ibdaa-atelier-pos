/* Isolated TEST database only. Fixture changes are rolled back. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51166, 'Set the exact approved TEST database name.', 1;

SET XACT_ABORT OFF;
BEGIN TRANSACTION;
INSERT dbo.Users(UserName, PasswordHash, Role)
    VALUES (N'ci_admin_access', N'test-not-a-real-password-hash', N'Admin');
DECLARE @userId INT = SCOPE_IDENTITY();

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @userId AND AuthVersion = 1)
    THROW 51167, 'New users should start with authentication version 1.', 1;

UPDATE dbo.Users SET PasswordHash = N'another-test-hash', AuthVersion = AuthVersion + 1
    WHERE UserId = @userId;
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @userId AND AuthVersion = 2)
    THROW 51168, 'Password reset must change the numeric session version.', 1;

DECLARE @blocked BIT = 0;
BEGIN TRY
    INSERT dbo.Users(UserName, PasswordHash, Role)
        VALUES (N'ci_admin_access', N'test-not-a-real-password-hash', N'Admin');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
    SET @blocked = 1;
END CATCH;
IF @blocked <> 1
    THROW 51169, 'Duplicate user name was accepted.', 1;

SET @blocked = 0;
BEGIN TRY
    INSERT dbo.Users(UserName, PasswordHash, Role)
        VALUES (N'ci_unsupported_role', N'test-not-a-real-password-hash', N'Owner');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 547 THROW;
    SET @blocked = 1;
END CATCH;
IF @blocked <> 1
    THROW 51170, 'Unsupported user role was accepted.', 1;

ROLLBACK TRANSACTION;
SELECT N'User identity/version/constraints smoke passed; fixtures rolled back.' AS Result;
