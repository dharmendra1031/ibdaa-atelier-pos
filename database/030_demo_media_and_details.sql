/* Local IBDAA_TEST demo data only. Image files live under wwwroot/uploads/products/<id>/demo.png. */
SET XACT_ABORT ON;
IF DB_NAME() <> N'IBDAA_TEST' THROW 51200, 'Demo script runs only in IBDAA_TEST.', 1;
IF (SELECT COUNT(*) FROM dbo.Products WHERE ProductId BETWEEN 1 AND 8) <> 8
    THROW 51201, 'Expected eight demo products with IDs 1-8.', 1;

BEGIN TRANSACTION;

INSERT dbo.ProductImages (ProductId, ImagePath, IsPrimary, DisplayOrder)
SELECT p.ProductId, CONCAT(N'/uploads/products/', p.ProductId, N'/demo.png'), 1, 0
FROM dbo.Products p
WHERE p.ProductId BETWEEN 1 AND 8
  AND NOT EXISTS (SELECT 1 FROM dbo.ProductImages i WHERE i.ProductId = p.ProductId);

INSERT dbo.SizeChartValues (SizeId, MeasurementId, Value)
SELECT s.SizeId, m.MeasurementId,
    CASE m.Name
        WHEN N'Bust' THEN CHOOSE(s.DisplayOrder, 88, 94, 100, 106)
        WHEN N'Waist' THEN CHOOSE(s.DisplayOrder, 70, 76, 82, 88)
        WHEN N'Hips' THEN CHOOSE(s.DisplayOrder, 96, 102, 108, 114)
        WHEN N'Length' THEN CHOOSE(s.DisplayOrder, 138, 140, 142, 144)
    END
FROM dbo.ProductSizes s
JOIN dbo.Products p ON p.ProductId = s.ProductId
JOIN dbo.MeasurementDefinitions m ON m.CategoryId = p.CategoryId
WHERE s.DisplayOrder BETWEEN 1 AND 4
  AND m.Name IN (N'Bust', N'Waist', N'Hips', N'Length')
  AND NOT EXISTS (SELECT 1 FROM dbo.SizeChartValues v
                  WHERE v.SizeId = s.SizeId AND v.MeasurementId = m.MeasurementId);

IF NOT EXISTS (SELECT 1 FROM dbo.ShopSettings)
    INSERT dbo.ShopSettings (BusinessName, BusinessNameAr)
    VALUES (N'IBDAA Atelier', N'إبداع أتيليه');

COMMIT TRANSACTION;
SELECT (SELECT COUNT(*) FROM dbo.ProductImages) AS ProductImages,
       (SELECT COUNT(*) FROM dbo.SizeChartValues) AS SizeChartValues,
       (SELECT COUNT(*) FROM dbo.ShopSettings) AS ShopSettings;
