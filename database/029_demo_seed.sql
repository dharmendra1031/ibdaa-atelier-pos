/* Demo data for the isolated IBDAA_TEST database. Prices are illustrative KWD, not approved shop rates. */
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'IBDAA_TEST' THROW 51100, 'Demo seed runs only in IBDAA_TEST.', 1;
IF EXISTS (SELECT 1 FROM dbo.Products) OR EXISTS (SELECT 1 FROM dbo.Customers)
    OR EXISTS (SELECT 1 FROM dbo.AlterationServices)
    THROW 51101, 'Demo data already exists; seed was not applied.', 1;

BEGIN TRANSACTION;

INSERT dbo.ProductCategories (Name, NameAr, DisplayOrder) VALUES
(N'Abayas', N'عبايات', 1), (N'Dresses', N'فساتين', 2),
(N'Jalabiyas', N'جلابيات', 3), (N'Accessories', N'إكسسوارات', 4);

INSERT dbo.Products (CategoryId, Name, NameAr, Description, ProductType, RegularPrice, DiscountPrice, DisplayOrder)
SELECT c.CategoryId, v.Name, v.NameAr, v.Description, v.ProductType, v.Price, v.Discount, v.SortOrder
FROM (VALUES
 (N'Abayas', N'Classic Black Abaya', N'عباية سوداء كلاسيكية', N'Everyday flowing abaya; demo item.', N'Ready-made', 38.000, NULL, 1),
 (N'Abayas', N'Embroidered Occasion Abaya', N'عباية مطرزة للمناسبات', N'Embroidered occasion style; demo item.', N'Ready-made', 72.000, 65.000, 2),
 (N'Abayas', N'Custom Tailored Abaya', N'عباية تفصيل حسب الطلب', N'Made to measure; demo starting price.', N'Made-to-measure', 55.000, NULL, 3),
 (N'Dresses', N'Evening Dress', N'فستان سهرة', N'Occasion dress; demo item.', N'Ready-made', 95.000, NULL, 1),
 (N'Dresses', N'Custom Dress', N'فستان تفصيل حسب الطلب', N'Made to measure; demo starting price.', N'Made-to-measure', 85.000, NULL, 2),
 (N'Jalabiyas', N'Everyday Jalabiya', N'جلابية يومية', N'Comfortable everyday style; demo item.', N'Ready-made', 32.000, NULL, 1),
 (N'Jalabiyas', N'Embroidered Jalabiya', N'جلابية مطرزة', N'Embroidered style; demo item.', N'Ready-made', 48.000, NULL, 2),
 (N'Accessories', N'Matching Scarf', N'وشاح متناسق', N'Lightweight scarf; demo item.', N'Ready-made', 9.500, NULL, 1)
) v(CategoryName, Name, NameAr, Description, ProductType, Price, Discount, SortOrder)
JOIN dbo.ProductCategories c ON c.Name = v.CategoryName;

INSERT dbo.ProductSizes (ProductId, Name, DisplayOrder)
SELECT p.ProductId, s.Name, s.SortOrder FROM dbo.Products p
CROSS JOIN (VALUES (N'S',1),(N'M',2),(N'L',3),(N'XL',4)) s(Name,SortOrder)
WHERE p.ProductType = N'Ready-made' AND p.Name <> N'Matching Scarf';

INSERT dbo.MeasurementDefinitions (CategoryId, Name, NameAr, Unit, IsRequired, DisplayOrder)
SELECT c.CategoryId, m.Name, m.NameAr, N'cm', 1, m.SortOrder
FROM dbo.ProductCategories c
CROSS JOIN (VALUES (N'Bust',N'الصدر',1),(N'Waist',N'الخصر',2),(N'Hips',N'الأرداف',3),(N'Length',N'الطول',4)) m(Name,NameAr,SortOrder)
WHERE c.Name IN (N'Abayas',N'Dresses',N'Jalabiyas');

INSERT dbo.ServiceCategories (Name, NameAr) VALUES
(N'Hemming', N'تقصير'), (N'Fit adjustments', N'تعديل المقاس'), (N'Repairs', N'إصلاحات');

INSERT dbo.AlterationServices (ServiceCategoryId, Name, NameAr, Description, Price, EstimatedMinutes)
SELECT c.ServiceCategoryId, v.Name, v.NameAr, N'Demo rate; confirm with atelier before use.', v.Price, v.Minutes
FROM (VALUES
 (N'Hemming',N'Dress hem',N'تقصير فستان',8.000,45),
 (N'Hemming',N'Abaya hem',N'تقصير عباية',6.000,30),
 (N'Fit adjustments',N'Waist adjustment',N'تعديل الخصر',10.000,60),
 (N'Fit adjustments',N'Sleeve adjustment',N'تعديل الأكمام',7.000,45),
 (N'Repairs',N'Zip replacement',N'تبديل السحاب',5.000,30),
 (N'Repairs',N'Seam repair',N'إصلاح الخياطة',3.500,20)
) v(CategoryName,Name,NameAr,Price,Minutes)
JOIN dbo.ServiceCategories c ON c.Name = v.CategoryName;

INSERT dbo.Customers (Name, NameAr, Mobile, Address, Notes) VALUES
(N'Demo Customer A', N'عميلة تجريبية أ', N'00000001', N'Demo address, Kuwait', N'Synthetic test record; no real customer.'),
(N'Demo Customer B', N'عميلة تجريبية ب', N'00000002', N'Demo address, Kuwait', N'Synthetic test record; no real customer.'),
(N'Demo Customer C', N'عميلة تجريبية ج', N'00000003', N'Demo address, Kuwait', N'Synthetic test record; no real customer.');

COMMIT TRANSACTION;
SELECT (SELECT COUNT(*) FROM dbo.Products) AS Products,
       (SELECT COUNT(*) FROM dbo.Customers) AS Customers,
       (SELECT COUNT(*) FROM dbo.AlterationServices) AS Services;
