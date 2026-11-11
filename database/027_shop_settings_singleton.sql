/*
 * Prevent an irreversible SettingsId=1 lockout after a rolled-back first insert.
 * Keep numeric IDENTITY keys and enforce the one-row rule independently of ID value.
 * Approved backed-up TEST database only. No production execution.
 */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51220, 'Set the exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.ShopSettings', N'U') IS NULL
    THROW 51221, 'ShopSettings must exist before this migration.', 1;
IF COL_LENGTH(N'dbo.ShopSettings', N'SingletonFlag') IS NOT NULL
    THROW 51222, 'ShopSettings singleton migration has already been applied.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

ALTER TABLE dbo.ShopSettings DROP CONSTRAINT CK_ShopSettings_Singleton;
ALTER TABLE dbo.ShopSettings ADD SingletonFlag BIT NOT NULL
    CONSTRAINT DF_ShopSettings_SingletonFlag DEFAULT (1);
GO
ALTER TABLE dbo.ShopSettings ADD CONSTRAINT CK_ShopSettings_SingletonFlag
    CHECK (SingletonFlag = 1);
GO
CREATE UNIQUE INDEX UX_ShopSettings_OneRow ON dbo.ShopSettings(SingletonFlag);

COMMIT TRANSACTION;
SELECT N'Shop settings singleton is independent of numeric IDENTITY counter.' AS Result;
