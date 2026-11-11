/* Apply manually only to the approved backed-up TEST database after 001_initial_catalog.sql. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51030, 'Set the exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.MeasurementDefinitions', N'U') IS NULL
    THROW 51031, 'MeasurementDefinitions must exist.', 1;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name IN
    (N'UX_MeasurementDefinitions_ProductName', N'UX_MeasurementDefinitions_CategoryName')
    AND object_id = OBJECT_ID(N'dbo.MeasurementDefinitions'))
    THROW 51032, 'Measurement unique indexes may already exist.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;
CREATE UNIQUE INDEX UX_MeasurementDefinitions_ProductName
    ON dbo.MeasurementDefinitions(ProductId, Name) WHERE ProductId IS NOT NULL;
CREATE UNIQUE INDEX UX_MeasurementDefinitions_CategoryName
    ON dbo.MeasurementDefinitions(CategoryId, Name) WHERE CategoryId IS NOT NULL;
COMMIT TRANSACTION;
