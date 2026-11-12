/* Optional gents measurement labels for the website-based IBDAA_TEST seed.
   Product images, available sizes and size chart values are NOT published reliably
   at /products. Add verified images via Admin instead of inserting fake demo.png paths.
   All measurement fields are optional until the shop approves its tailoring form. */
SET XACT_ABORT ON;
IF DB_NAME() <> N'IBDAA_TEST'
    THROW 51200, 'Details script runs only in IBDAA_TEST.', 1;
IF (SELECT COUNT(*) FROM dbo.Products) <> 16
    OR NOT EXISTS (SELECT 1 FROM dbo.Products WHERE NameAr = N'قندورة "سلطان"')
    OR NOT EXISTS (SELECT 1 FROM dbo.Products WHERE NameAr = N'"نسمات الصيف" Summer Breeze')
    THROW 51201, 'Expected the 16-item gents demo seed; no data changed.', 1;

BEGIN TRANSACTION;

INSERT dbo.MeasurementDefinitions
    (CategoryId, Name, NameAr, Unit, IsRequired, DisplayOrder)
SELECT category.CategoryId, field.Name, field.NameAr, N'cm', 0, field.SortOrder
FROM dbo.ProductCategories category
CROSS JOIN (VALUES
    (N'Chest', N'الصدر', 1),
    (N'Shoulder', N'الكتف', 2),
    (N'Sleeve Length', N'طول الكم', 3),
    (N'Total Length', N'الطول الكلي', 4)
) field(Name, NameAr, SortOrder)
WHERE category.Name IN (N'Gandouras', N'Kaftans', N'Jabadors', N'Jalabiyas')
    AND NOT EXISTS (
        SELECT 1 FROM dbo.MeasurementDefinitions existing
        WHERE existing.CategoryId = category.CategoryId AND existing.Name = field.Name
    );

IF NOT EXISTS (SELECT 1 FROM dbo.ShopSettings)
    INSERT dbo.ShopSettings (BusinessName, BusinessNameAr)
    VALUES (N'IBDAA Atelier', N'إبداع أتيليه');

COMMIT TRANSACTION;

SELECT (SELECT COUNT(*) FROM dbo.Products) AS Products,
       (SELECT COUNT(*) FROM dbo.MeasurementDefinitions) AS OptionalMeasurementFields,
       (SELECT COUNT(*) FROM dbo.ProductImages) AS VerifiedProductImages,
       (SELECT COUNT(*) FROM dbo.SizeChartValues) AS ApprovedSizeChartValues;
