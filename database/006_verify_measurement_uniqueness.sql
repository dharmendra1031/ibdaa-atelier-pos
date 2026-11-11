/* Read-only verification in an approved isolated TEST database. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51033, 'Set the exact approved TEST database name.', 1;
IF (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MeasurementDefinitions')
    AND name IN (N'UX_MeasurementDefinitions_ProductName', N'UX_MeasurementDefinitions_CategoryName')) <> 2
    THROW 51034, 'Measurement uniqueness constraints are missing.', 1;
SELECT N'Measurement uniqueness constraints present' AS Result;
