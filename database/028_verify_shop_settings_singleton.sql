/* Read-only TEST database singleton verification. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51223, 'Set the exact approved TEST database name.', 1;
IF COL_LENGTH(N'dbo.ShopSettings', N'SingletonFlag') IS NULL
    OR NOT EXISTS (SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ShopSettings')
        AND name = N'UX_ShopSettings_OneRow' AND is_unique = 1)
    THROW 51224, 'The one-row constraint is missing.', 1;
IF COLUMNPROPERTY(OBJECT_ID(N'dbo.ShopSettings'), 'SettingsId', 'IsIdentity') <> 1
    THROW 51225, 'Numeric identity key must be retained.', 1;
SELECT COUNT_BIG(*) AS ShopSettingsRowCount FROM dbo.ShopSettings;
