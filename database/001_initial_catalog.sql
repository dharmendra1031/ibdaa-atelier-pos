/*
  IBDAA Atelier POS - catalogue foundation.
  Run manually on a newly created, backed-up TEST database only.
  No seed products are added and the live website is not contacted.
*/
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';

IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51000, 'Set the exact approved TEST database name.', 1;

IF OBJECT_ID(N'dbo.ProductCategories', N'U') IS NOT NULL
    OR OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL
    OR OBJECT_ID(N'dbo.ProductImages', N'U') IS NOT NULL
    OR OBJECT_ID(N'dbo.ProductSizes', N'U') IS NOT NULL
    OR OBJECT_ID(N'dbo.MeasurementDefinitions', N'U') IS NOT NULL
    OR OBJECT_ID(N'dbo.SizeChartValues', N'U') IS NOT NULL
    THROW 51001, 'Catalogue tables already exist. Inspect the schema before re-running.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.ProductCategories
(
    CategoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductCategories PRIMARY KEY,
    ParentCategoryId INT NULL,
    Name NVARCHAR(100) NOT NULL,
    NameAr NVARCHAR(100) NULL,
    Description NVARCHAR(1000) NULL,
    DescriptionAr NVARCHAR(1000) NULL,
    DisplayOrder INT NOT NULL CONSTRAINT DF_ProductCategories_Order DEFAULT (0),
    IsActive BIT NOT NULL CONSTRAINT DF_ProductCategories_Active DEFAULT (1),
    CONSTRAINT FK_ProductCategories_Parent FOREIGN KEY (ParentCategoryId)
        REFERENCES dbo.ProductCategories(CategoryId),
    CONSTRAINT CK_ProductCategories_Valid CHECK
        (LEN(LTRIM(RTRIM(Name))) > 0 AND DisplayOrder >= 0
         AND (ParentCategoryId IS NULL OR ParentCategoryId <> CategoryId))
);
CREATE UNIQUE INDEX UX_ProductCategories_ParentName
    ON dbo.ProductCategories(ParentCategoryId, Name);

CREATE TABLE dbo.Products
(
    ProductId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    ProductCode AS (CONVERT(VARCHAR(4), 'PRD-') + CONVERT(VARCHAR(12), ProductId)) PERSISTED,
    CategoryId INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    NameAr NVARCHAR(150) NULL,
    Description NVARCHAR(2000) NULL,
    DescriptionAr NVARCHAR(2000) NULL,
    ProductType NVARCHAR(60) NOT NULL CONSTRAINT DF_Products_Type DEFAULT (N'Ready-made'),
    RegularPrice DECIMAL(18,3) NOT NULL,
    DiscountPrice DECIMAL(18,3) NULL,
    DisplayOrder INT NOT NULL CONSTRAINT DF_Products_Order DEFAULT (0),
    IsActive BIT NOT NULL CONSTRAINT DF_Products_Active DEFAULT (1),
    CONSTRAINT FK_Products_Category FOREIGN KEY (CategoryId)
        REFERENCES dbo.ProductCategories(CategoryId),
    CONSTRAINT CK_Products_Valid CHECK
        (LEN(LTRIM(RTRIM(Name))) > 0 AND LEN(LTRIM(RTRIM(ProductType))) > 0
         AND DisplayOrder >= 0 AND RegularPrice >= 0
         AND (DiscountPrice IS NULL OR DiscountPrice BETWEEN 0 AND RegularPrice))
);
CREATE UNIQUE INDEX UX_Products_Code ON dbo.Products(ProductCode);
CREATE INDEX IX_Products_Catalog ON dbo.Products(CategoryId, IsActive, DisplayOrder, ProductId);

CREATE TABLE dbo.ProductImages
(
    ProductImageId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductImages PRIMARY KEY,
    ProductId INT NOT NULL,
    ImagePath NVARCHAR(260) NOT NULL,
    IsPrimary BIT NOT NULL CONSTRAINT DF_ProductImages_Primary DEFAULT (0),
    DisplayOrder INT NOT NULL CONSTRAINT DF_ProductImages_Order DEFAULT (0),
    CONSTRAINT FK_ProductImages_Product FOREIGN KEY (ProductId)
        REFERENCES dbo.Products(ProductId),
    CONSTRAINT CK_ProductImages_Valid CHECK
        (LEN(LTRIM(RTRIM(ImagePath))) > 0 AND DisplayOrder >= 0)
);
CREATE UNIQUE INDEX UX_ProductImages_Primary
    ON dbo.ProductImages(ProductId) WHERE IsPrimary = 1;
CREATE INDEX IX_ProductImages_Product ON dbo.ProductImages(ProductId, DisplayOrder, ProductImageId);

CREATE TABLE dbo.ProductSizes
(
    SizeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductSizes PRIMARY KEY,
    ProductId INT NOT NULL,
    Name NVARCHAR(30) NOT NULL,
    DisplayOrder INT NOT NULL CONSTRAINT DF_ProductSizes_Order DEFAULT (0),
    IsActive BIT NOT NULL CONSTRAINT DF_ProductSizes_Active DEFAULT (1),
    CONSTRAINT FK_ProductSizes_Product FOREIGN KEY (ProductId)
        REFERENCES dbo.Products(ProductId),
    CONSTRAINT UQ_ProductSizes_Name UNIQUE (ProductId, Name),
    CONSTRAINT CK_ProductSizes_Valid CHECK
        (LEN(LTRIM(RTRIM(Name))) > 0 AND DisplayOrder >= 0)
);

CREATE TABLE dbo.MeasurementDefinitions
(
    MeasurementId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MeasurementDefinitions PRIMARY KEY,
    CategoryId INT NULL,
    ProductId INT NULL,
    Name NVARCHAR(100) NOT NULL,
    NameAr NVARCHAR(100) NULL,
    Unit NVARCHAR(20) NOT NULL CONSTRAINT DF_Measurements_Unit DEFAULT (N'cm'),
    IsRequired BIT NOT NULL CONSTRAINT DF_Measurements_Required DEFAULT (0),
    DisplayOrder INT NOT NULL CONSTRAINT DF_Measurements_Order DEFAULT (0),
    IsActive BIT NOT NULL CONSTRAINT DF_Measurements_Active DEFAULT (1),
    CONSTRAINT FK_Measurements_Category FOREIGN KEY (CategoryId)
        REFERENCES dbo.ProductCategories(CategoryId),
    CONSTRAINT FK_Measurements_Product FOREIGN KEY (ProductId)
        REFERENCES dbo.Products(ProductId),
    CONSTRAINT CK_Measurements_Scope CHECK
        ((CategoryId IS NOT NULL AND ProductId IS NULL) OR
         (CategoryId IS NULL AND ProductId IS NOT NULL)),
    CONSTRAINT CK_Measurements_Valid CHECK
        (LEN(LTRIM(RTRIM(Name))) > 0 AND LEN(LTRIM(RTRIM(Unit))) > 0 AND DisplayOrder >= 0)
);
CREATE INDEX IX_Measurements_Category ON dbo.MeasurementDefinitions(CategoryId, IsActive, DisplayOrder);
CREATE INDEX IX_Measurements_Product ON dbo.MeasurementDefinitions(ProductId, IsActive, DisplayOrder);

CREATE TABLE dbo.SizeChartValues
(
    ChartValueId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SizeChartValues PRIMARY KEY,
    SizeId INT NOT NULL,
    MeasurementId INT NOT NULL,
    Value DECIMAL(9,2) NOT NULL,
    CONSTRAINT FK_SizeCharts_Size FOREIGN KEY (SizeId)
        REFERENCES dbo.ProductSizes(SizeId),
    CONSTRAINT FK_SizeCharts_Measurement FOREIGN KEY (MeasurementId)
        REFERENCES dbo.MeasurementDefinitions(MeasurementId),
    CONSTRAINT UQ_SizeCharts_SizeMeasurement UNIQUE (SizeId, MeasurementId),
    CONSTRAINT CK_SizeCharts_NonNegative CHECK (Value >= 0)
);

COMMIT TRANSACTION;
SELECT DB_NAME() AS DatabaseName, N'Catalogue schema created - no data imported' AS Result;
