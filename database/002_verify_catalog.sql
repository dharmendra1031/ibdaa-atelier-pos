-- Read-only check for 001_initial_catalog.sql.
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51010, 'Set the exact approved TEST database name.', 1;

IF OBJECT_ID(N'dbo.ProductCategories', N'U') IS NULL
 OR OBJECT_ID(N'dbo.Products', N'U') IS NULL
 OR OBJECT_ID(N'dbo.ProductImages', N'U') IS NULL
 OR OBJECT_ID(N'dbo.ProductSizes', N'U') IS NULL
 OR OBJECT_ID(N'dbo.MeasurementDefinitions', N'U') IS NULL
 OR OBJECT_ID(N'dbo.SizeChartValues', N'U') IS NULL
    THROW 51011, 'Missing catalogue table.', 1;

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.ProductCategories'), 'CategoryId', 'IsIdentity') <> 1
 OR COLUMNPROPERTY(OBJECT_ID(N'dbo.Products'), 'ProductId', 'IsIdentity') <> 1
 OR COLUMNPROPERTY(OBJECT_ID(N'dbo.ProductImages'), 'ProductImageId', 'IsIdentity') <> 1
 OR COLUMNPROPERTY(OBJECT_ID(N'dbo.ProductSizes'), 'SizeId', 'IsIdentity') <> 1
 OR COLUMNPROPERTY(OBJECT_ID(N'dbo.MeasurementDefinitions'), 'MeasurementId', 'IsIdentity') <> 1
 OR COLUMNPROPERTY(OBJECT_ID(N'dbo.SizeChartValues'), 'ChartValueId', 'IsIdentity') <> 1
    THROW 51012, 'Every primary key must use numeric SQL IDENTITY.', 1;

SELECT 'ProductCategories' AS TableName, COUNT_BIG(*) AS RowsFound FROM dbo.ProductCategories
UNION ALL SELECT 'Products', COUNT_BIG(*) FROM dbo.Products
UNION ALL SELECT 'ProductImages', COUNT_BIG(*) FROM dbo.ProductImages
UNION ALL SELECT 'ProductSizes', COUNT_BIG(*) FROM dbo.ProductSizes
UNION ALL SELECT 'MeasurementDefinitions', COUNT_BIG(*) FROM dbo.MeasurementDefinitions
UNION ALL SELECT 'SizeChartValues', COUNT_BIG(*) FROM dbo.SizeChartValues;
