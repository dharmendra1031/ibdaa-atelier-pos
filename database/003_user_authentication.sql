/*
 * IBDAA Atelier: numeric users and audit records.
 * Apply only to an approved, backed-up isolated TEST database after 001_initial_catalog.sql.
 */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51020, 'Set the exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
    OR OBJECT_ID(N'dbo.AuditLogs', N'U') IS NOT NULL
    THROW 51021, 'Security tables already exist; migration not applied twice.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.Users
(
    UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    UserName NVARCHAR(60) NOT NULL,
    PasswordHash NVARCHAR(512) NOT NULL,
    Role NVARCHAR(20) NOT NULL CONSTRAINT DF_Users_Role DEFAULT(N'Cashier'),
    IsActive BIT NOT NULL CONSTRAINT DF_Users_Active DEFAULT(1),
    FailedLoginCount INT NOT NULL CONSTRAINT DF_Users_Failed DEFAULT(0),
    LockedUntilUtc DATETIME2 NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Users_Created DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Cashier')),
    CONSTRAINT CK_Users_Failed CHECK (FailedLoginCount BETWEEN 0 AND 4),
    CONSTRAINT CK_Users_ValidName CHECK (LEN(LTRIM(RTRIM(UserName))) > 0)
);
CREATE UNIQUE INDEX UX_Users_UserName ON dbo.Users(UserName);

CREATE TABLE dbo.AuditLogs
(
    AuditId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
    UserId INT NULL,
    Action NVARCHAR(80) NOT NULL,
    EntityName NVARCHAR(60) NOT NULL,
    EntityId BIGINT NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_Created DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT FK_AuditLogs_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
CREATE INDEX IX_AuditLogs_UserCreated ON dbo.AuditLogs(UserId, CreatedAtUtc DESC);

COMMIT TRANSACTION;
SELECT N'Security tables created. No credentials seeded.' AS Result;
