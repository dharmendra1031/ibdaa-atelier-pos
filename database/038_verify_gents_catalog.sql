/* Read-only catalogue verification after 029+030 (fresh) or 037 (existing test).
   No real product photo can be inferred from the website's text catalogue. */
IF DB_NAME() <> N'IBDAA_TEST'
    THROW 51340, 'Verification is restricted to IBDAA_TEST.', 1;

DECLARE @WebsiteProducts TABLE (NameAr nvarchar(150), PublicPrice decimal(18,3));
INSERT INTO @WebsiteProducts (NameAr, PublicPrice) VALUES
(N'قندورة "سلطان"',23.000),(N'قفطان الزمرد',26.000),
(N'قندورة "أصيل"',25.000),(N'قندورة نسيج الغسق',24.000),
(N'جابادور "أمير الأناقة"',23.000),(N'جابادور الأزرق الكشخة',25.000),
(N'قفطان "نسمات الأفق"',24.000),(N'قندورة "هيبة صيف"',25.000),
(N'جلابة "نسيم كازا"',30.000),(N'جابادور "إبداع" المغربي',24.000),
(N'قندورة الهبة',23.000),(N'جلابية الحضور',30.000),
(N'جابادور "الأفق الأزرق"',26.000),
(N'عرض الصيف (٢قندورة+قفطان)',66.000),
(N'"أناقة الأب والابن" بلمسة مغربية',51.000),
(N'"نسمات الصيف" Summer Breeze',19.000);

IF EXISTS (
    SELECT 1 FROM @WebsiteProducts reference
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Products product
        WHERE product.NameAr = reference.NameAr
          AND product.IsActive = 1
          AND COALESCE(product.DiscountPrice, product.RegularPrice) = reference.PublicPrice
    )
)
    THROW 51341, 'One or more published gents products or prices are missing.', 1;

IF EXISTS (SELECT 1 FROM dbo.Products
   WHERE IsActive = 1
   AND Name IN (N'Classic Black Abaya', N'Embroidered Occasion Abaya',
                N'Custom Tailored Abaya', N'Evening Dress', N'Custom Dress',
                N'Everyday Jalabiya', N'Embroidered Jalabiya', N'Matching Scarf'))
    THROW 51342, 'Old ladies seed products remain active.', 1;

IF EXISTS (SELECT 1 FROM dbo.AlterationServices
    WHERE IsActive = 1 AND Description = N'Demo rate; confirm with atelier before use.')
    THROW 51343, 'Artificial alteration rates remain active.', 1;

SELECT product.ProductId, product.Name, product.NameAr,
       product.RegularPrice, product.DiscountPrice, category.Name AS Category,
       (SELECT COUNT(*) FROM dbo.ProductImages image
        WHERE image.ProductId = product.ProductId) AS OriginalPhotosUploaded
FROM dbo.Products product
JOIN dbo.ProductCategories category ON category.CategoryId = product.CategoryId
JOIN @WebsiteProducts reference ON reference.NameAr = product.NameAr
ORDER BY product.DisplayOrder, product.ProductId;
