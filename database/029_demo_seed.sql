/* Gents-only TEST catalogue based on https://www.ibdaaatelier.com/products
   Reviewed 2026-11-12. Prices come from the publicly listed KWD prices.
   English titles and POS category grouping are working translations.
   Stock, sizing, images and tailor-service rates are not verified.
   Run only in an empty IBDAA_TEST database. No production updates. */
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'IBDAA_TEST'
    THROW 51100, 'Gents demo seed runs only in IBDAA_TEST.', 1;
IF EXISTS (SELECT 1 FROM dbo.ProductCategories)
    OR EXISTS (SELECT 1 FROM dbo.Products)
    OR EXISTS (SELECT 1 FROM dbo.ProductImages)
    OR EXISTS (SELECT 1 FROM dbo.ProductSizes)
    OR EXISTS (SELECT 1 FROM dbo.MeasurementDefinitions)
    OR EXISTS (SELECT 1 FROM dbo.SizeChartValues)
    OR EXISTS (SELECT 1 FROM dbo.Customers)
    OR EXISTS (SELECT 1 FROM dbo.ServiceCategories)
    OR EXISTS (SELECT 1 FROM dbo.AlterationServices)
    THROW 51101, 'Catalogue or demo records already exist; no data was changed.', 1;

BEGIN TRANSACTION;

-- Practical POS groupings by garment type; the public website filters may differ.
INSERT dbo.ProductCategories (Name, NameAr, DisplayOrder) VALUES
(N'Gandouras', N'القنادير', 1),
(N'Kaftans', N'القفاطين', 2),
(N'Jabadors', N'الجابادور', 3),
(N'Jalabiyas', N'الجلابيات', 4),
(N'Sets and Offers', N'الأطقم والعروض', 5);

-- Original Arabic titles and public catalogue prices. Price columns: regular, discount.
INSERT dbo.Products
    (CategoryId, Name, NameAr, Description, ProductType,
     RegularPrice, DiscountPrice, DisplayOrder)
SELECT category.CategoryId, item.Name, item.NameAr,
    N'Website catalogue reference. Confirm current price, availability and product options before live use.',
    N'Ready-made', item.RegularPrice, item.DiscountPrice, item.SortOrder
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
JOIN dbo.ProductCategories category ON category.Name = item.CategoryName;

-- Service categories only: the website does not publish approved alteration prices.
INSERT dbo.ServiceCategories (Name, NameAr) VALUES
    (N'Length Alterations', N'تعديلات الطول'),
    (N'Fit Adjustments', N'تعديلات المقاس'),
    (N'Repairs', N'الإصلاحات');

-- Synthetic customer records are not actual website customers.
INSERT dbo.Customers (Name, NameAr, Mobile, Address, Notes) VALUES
    (N'Demo Customer A', N'عميل تجريبي أ', N'00000001', N'Demo address, Kuwait', N'Synthetic test record; not a real customer.'),
    (N'Demo Customer B', N'عميل تجريبي ب', N'00000002', N'Demo address, Kuwait', N'Synthetic test record; not a real customer.'),
    (N'Demo Customer C', N'عميل تجريبي ج', N'00000003', N'Demo address, Kuwait', N'Synthetic test record; not a real customer.');

COMMIT TRANSACTION;

SELECT (SELECT COUNT(*) FROM dbo.Products) AS Products,
       (SELECT COUNT(*) FROM dbo.ProductCategories) AS Categories,
       (SELECT COUNT(*) FROM dbo.Customers) AS Customers,
       (SELECT COUNT(*) FROM dbo.AlterationServices) AS PricedAlterationServices;
