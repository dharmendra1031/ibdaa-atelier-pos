/* TEST ONLY: validates first insert, billing header fields and singleton enforcement. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51085, 'Isolated TEST database required.', 1;

SET XACT_ABORT OFF;
BEGIN TRANSACTION;

INSERT dbo.ShopSettings(BusinessName, BusinessNameAr, Mobile, Address, AddressAr)
VALUES (N'Test Atelier', N'مشغل تجريبي', N'12345678', N'Kuwait test address', N'عنوان تجريبي');
DECLARE @settingsId INT = SCOPE_IDENTITY();
IF @settingsId <> 1
    THROW 51086, 'Settings singleton must use first numeric identity.', 1;

UPDATE dbo.ShopSettings
SET BusinessName = N'Updated Test Atelier', Email = N'test@example.invalid'
WHERE SettingsId = @settingsId;

IF NOT EXISTS (SELECT 1 FROM dbo.ShopSettings
    WHERE SettingsId = 1 AND BusinessName = N'Updated Test Atelier'
    AND BusinessNameAr = N'مشغل تجريبي')
    THROW 51087, 'Settings update did not preserve bilingual details.', 1;

ROLLBACK TRANSACTION;
SELECT N'Shop settings insert/update smoke passed; all changes rolled back.' AS Result;
