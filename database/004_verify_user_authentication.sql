/* Read-only check against the approved test database only. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51022, 'Set the exact approved TEST database name.', 1;

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.Users'), 'UserId', 'IsIdentity') <> 1
 OR COLUMNPROPERTY(OBJECT_ID(N'dbo.AuditLogs'), 'AuditId', 'IsIdentity') <> 1
    THROW 51023, 'Numeric SQL IDENTITY keys are required.', 1;

SELECT UserId, UserName, Role, IsActive, CreatedAtUtc FROM dbo.Users;
SELECT COUNT_BIG(*) AS AuditCount FROM dbo.AuditLogs;
