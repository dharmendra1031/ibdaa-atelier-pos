/* Run manually only against the approved TEST database. Read-only verification. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51052, 'Set the exact approved TEST database name.', 1;
IF COLUMNPROPERTY(OBJECT_ID(N'dbo.ServiceCategories'), 'ServiceCategoryId', 'IsIdentity') <> 1
    OR COLUMNPROPERTY(OBJECT_ID(N'dbo.AlterationServices'), 'ServiceId', 'IsIdentity') <> 1
    THROW 51053, 'Service master IDENTITY keys are missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.AlterationServices') AND name = N'UX_AlterationServices_CategoryName')
    THROW 51054, 'Service deduplication index is missing.', 1;
SELECT COUNT_BIG(*) AS ServiceCategoryCount FROM dbo.ServiceCategories;
SELECT COUNT_BIG(*) AS AlterationServiceCount FROM dbo.AlterationServices;
