/* Apply manually only to approved, backed-up TEST database. No client data seeded. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51080, 'Set exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.ShopSettings', N'U') IS NOT NULL
    THROW 51081, 'ShopSettings already exists.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.ShopSettings
(
    SettingsId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ShopSettings PRIMARY KEY,
    BusinessName NVARCHAR(150) NOT NULL,
    BusinessNameAr NVARCHAR(150) NOT NULL,
    Mobile NVARCHAR(25) NULL,
    Email NVARCHAR(150) NULL,
    Address NVARCHAR(500) NULL,
    AddressAr NVARCHAR(500) NULL,
    LogoPath NVARCHAR(260) NULL,
    CONSTRAINT CK_ShopSettings_Singleton CHECK (SettingsId = 1),
    CONSTRAINT CK_ShopSettings_Names CHECK
        (LEN(LTRIM(RTRIM(BusinessName))) > 0 AND LEN(LTRIM(RTRIM(BusinessNameAr))) > 0)
);
COMMIT TRANSACTION;
SELECT N'Shop settings schema created; no client data seeded.' AS Result;
