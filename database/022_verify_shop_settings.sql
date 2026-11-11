/* Read-only TEST database verification. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51082, 'Set exact approved TEST database name.', 1;

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.ShopSettings'), 'SettingsId', 'IsIdentity') <> 1
    THROW 51083, 'SettingsId must be numeric IDENTITY.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ShopSettings') AND name = N'CK_ShopSettings_Singleton')
    THROW 51084, 'Shop settings singleton constraint missing.', 1;

SELECT COUNT_BIG(*) AS SettingsRowCount FROM dbo.ShopSettings;
