/* Isolated TEST DB only: schema constraints and numeric gallery IDs. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51080, 'Set the exact approved TEST database name.', 1;

SET XACT_ABORT OFF;
BEGIN TRANSACTION;
INSERT dbo.ProductCategories(Name, NameAr) VALUES(N'CI Gallery', N'معرض الاختبار');
DECLARE @categoryId INT = SCOPE_IDENTITY();
INSERT dbo.Products(CategoryId, Name, NameAr, ProductType, RegularPrice)
    VALUES(@categoryId, N'Gallery test garment', N'ملابس الاختبار', N'Ready-made', 18.000);
DECLARE @productId INT = SCOPE_IDENTITY();

INSERT dbo.ProductImages(ProductId, ImagePath, IsPrimary, DisplayOrder)
    VALUES(@productId, N'/uploads/products/1/1.jpg', 1, 0);
DECLARE @firstId INT = SCOPE_IDENTITY();
INSERT dbo.ProductImages(ProductId, ImagePath, IsPrimary, DisplayOrder)
    VALUES(@productId, N'/uploads/products/1/2.jpg', 0, 1);
DECLARE @secondId INT = SCOPE_IDENTITY();

IF @firstId <= 0 OR @secondId <= @firstId
    THROW 51081, 'Image IDs must be increasing numeric identities.', 1;

DECLARE @blocked BIT = 0;
BEGIN TRY
    INSERT dbo.ProductImages(ProductId, ImagePath, IsPrimary, DisplayOrder)
        VALUES(@productId, N'/uploads/products/1/3.jpg', 1, 2);
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
    SET @blocked = 1;
END CATCH;

IF @blocked <> 1
    THROW 51082, 'Duplicate primary photograph was accepted.', 1;

UPDATE dbo.ProductImages SET IsPrimary = 0 WHERE ProductImageId = @firstId;
UPDATE dbo.ProductImages SET IsPrimary = 1 WHERE ProductImageId = @secondId;

IF (SELECT COUNT(*) FROM dbo.ProductImages WHERE ProductId = @productId AND IsPrimary = 1) <> 1
    THROW 51083, 'Primary image reassignment failed.', 1;

ROLLBACK TRANSACTION;
SELECT N'Gallery SQL constraints passed; fixture rolled back.' AS Result;
