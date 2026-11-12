/* Approved IBDAA_TEST data-only repair, preserving all existing orders and IDs.
   Replaces the active ladies demo catalogue with official IBDAA gents products.
   Check the affected rows with SELECT before running. BACK UP IBDAA_TEST first.
   NOT for production, and do not re-seed identities or change historical orders.
   Source: https://www.ibdaaatelier.com/products (2026-11-12). */
SET XACT_ABORT ON;
IF DB_NAME() <> N'IBDAA_TEST'
    THROW 51330, 'Run only against the backed-up IBDAA_TEST database.', 1;
IF OBJECT_ID(N'dbo.Orders',N'U') IS NULL OR OBJECT_ID(N'dbo.OrderItems',N'U') IS NULL
    OR OBJECT_ID(N'dbo.Products',N'U') IS NULL
    OR OBJECT_ID(N'dbo.ProductCategories',N'U') IS NULL
    OR OBJECT_ID(N'dbo.AlterationServices',N'U') IS NULL
    OR OBJECT_ID(N'dbo.ServiceCategories',N'U') IS NULL
    THROW 51331, 'Required catalogue or invoice tables are missing.', 1;

BEGIN TRANSACTION;

-- Only retire precisely matching sample products. Do not delete IDs or invoices.
UPDATE dbo.Products SET IsActive = 0
WHERE (Name = N'Classic Black Abaya' AND NameAr = N'عباية سوداء كلاسيكية')
   OR (Name = N'Embroidered Occasion Abaya' AND NameAr = N'عباية مطرزة للمناسبات')
   OR (Name = N'Custom Tailored Abaya' AND NameAr = N'عباية تفصيل حسب الطلب')
   OR (Name = N'Evening Dress' AND NameAr = N'فستان سهرة')
   OR (Name = N'Custom Dress' AND NameAr = N'فستان تفصيل حسب الطلب')
   OR (Name = N'Everyday Jalabiya' AND NameAr = N'جلابية يومية')
   OR (Name = N'Embroidered Jalabiya' AND NameAr = N'جلابية مطرزة')
   OR (Name = N'Matching Scarf' AND NameAr = N'وشاح متناسق');

-- The old alteration prices were fabricated; leave references for old invoices.
UPDATE dbo.AlterationServices SET IsActive = 0
WHERE Description = N'Demo rate; confirm with atelier before use.'
  AND Name IN (N'Dress hem', N'Abaya hem', N'Waist adjustment',
               N'Sleeve adjustment', N'Zip replacement', N'Seam repair');

INSERT dbo.ProductCategories (Name, NameAr, DisplayOrder)
SELECT category.Name, category.NameAr, category.SortOrder
FROM (VALUES
    (N'Gandouras', N'القنادير', 1),
    (N'Kaftans', N'القفاطين', 2),
    (N'Jabadors', N'الجابادور', 3),
    (N'Jalabiyas', N'الجلابيات', 4),
    (N'Sets and Offers', N'الأطقم والعروض', 5)
) category(Name, NameAr, SortOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductCategories existing WHERE existing.Name = category.Name);

INSERT dbo.Products (CategoryId, Name, NameAr, Description, ProductType,
                     RegularPrice, DiscountPrice, DisplayOrder)
SELECT category.CategoryId, item.Name, item.NameAr,
       CAST(NULL AS nvarchar(2000)), N'Ready-made',
       item.RegularPrice, item.DiscountPrice, item.SortOrder
FROM (VALUES
    (N'Gandouras', N'Sultan Gandoura', N'قندورة "سلطان"', 23.000, CAST(NULL AS decimal(18,3)), 1),
    (N'Kaftans', N'Emerald Kaftan', N'قفطان الزمرد', 26.000, NULL, 2),
    (N'Gandouras', N'Aseel Gandoura', N'قندورة "أصيل"', 25.000, NULL, 3),
    (N'Gandouras', N'Dusk Weave Gandoura', N'قندورة نسيج الغسق', 24.000, NULL, 4),
    (N'Jabadors', N'Prince of Elegance Jabador', N'جابادور "أمير الأناقة"', 23.000, NULL, 5),
    (N'Jabadors', N'Blue Kashkha Jabador', N'جابادور الأزرق الكشخة', 25.000, NULL, 6),
    (N'Kaftans', N'Horizon Breeze Kaftan', N'قفطان "نسمات الأفق"', 24.000, NULL, 7),
    (N'Gandouras', N'Summer Prestige Gandoura', N'قندورة "هيبة صيف"', 25.000, NULL, 8),
    (N'Jalabiyas', N'Casa Breeze Jalabiya', N'جلابة "نسيم كازا"', 30.000, NULL, 9),
    (N'Jabadors', N'Ibdaa Moroccan Jabador', N'جابادور "إبداع" المغربي', 24.000, NULL, 10),
    (N'Gandouras', N'Al Hiba Gandoura', N'قندورة الهبة', 23.000, NULL, 11),
    (N'Jalabiyas', N'Presence Jalabiya', N'جلابية الحضور', 30.000, NULL, 12),
    (N'Jabadors', N'Blue Horizon Jabador', N'جابادور "الأفق الأزرق"', 26.000, NULL, 13),
    (N'Sets and Offers', N'Summer Offer (2 Gandouras + Kaftan)', N'عرض الصيف (٢قندورة+قفطان)', 75.000, 66.000, 14),
    (N'Sets and Offers', N'Father and Son Moroccan Set', N'"أناقة الأب والابن" بلمسة مغربية', 51.000, NULL, 15),
    (N'Sets and Offers', N'Summer Breeze', N'"نسمات الصيف" Summer Breeze', 19.000, NULL, 16)
) item(CategoryName, Name, NameAr, RegularPrice, DiscountPrice, SortOrder)
JOIN dbo.ProductCategories category ON category.Name = item.CategoryName
WHERE NOT EXISTS (SELECT 1 FROM dbo.Products existing WHERE existing.NameAr = item.NameAr);

-- Old ladies-only categories disappear from POS if they have no active products.
UPDATE category SET IsActive = 0
FROM dbo.ProductCategories category
WHERE ((category.Name = N'Abayas' AND category.NameAr = N'عبايات')
    OR (category.Name = N'Dresses' AND category.NameAr = N'فساتين')
    OR (category.Name = N'Accessories' AND category.NameAr = N'إكسسوارات'))
    AND NOT EXISTS (
        SELECT 1 FROM dbo.Products activeProduct
        WHERE activeProduct.CategoryId = category.CategoryId AND activeProduct.IsActive = 1
    );

-- Remove only unreferenced synthetic customers. Referenced customer IDs survive.
DELETE customer FROM dbo.Customers customer
WHERE customer.Name IN (N'Demo Customer A',N'Demo Customer B',N'Demo Customer C')
  AND customer.Mobile IN (N'00000001',N'00000002',N'00000003')
  AND customer.Notes = N'Synthetic test record; no real customer.'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Orders history WHERE history.CustomerId = customer.CustomerId
  );

COMMIT TRANSACTION;

SELECT COUNT(*) AS ActiveWebsiteProducts FROM dbo.Products
WHERE IsActive = 1 AND NameAr IN (
    N'قندورة "سلطان"', N'قفطان الزمرد', N'قندورة "أصيل"',
    N'قندورة نسيج الغسق', N'جابادور "أمير الأناقة"',
    N'جابادور الأزرق الكشخة', N'قفطان "نسمات الأفق"',
    N'قندورة "هيبة صيف"', N'جلابة "نسيم كازا"',
    N'جابادور "إبداع" المغربي', N'قندورة الهبة',
    N'جلابية الحضور', N'جابادور "الأفق الأزرق"',
    N'عرض الصيف (٢قندورة+قفطان)',
    N'"أناقة الأب والابن" بلمسة مغربية',
    N'"نسمات الصيف" Summer Breeze'
);
SELECT COUNT(*) AS ReferencedSampleCustomersKept FROM dbo.Customers
WHERE Name LIKE N'Demo Customer %' AND Mobile LIKE N'0000000%';
