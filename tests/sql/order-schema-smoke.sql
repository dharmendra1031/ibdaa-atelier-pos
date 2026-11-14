/* TEST ONLY. All fixture rows are rolled back before the script ends. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51070, 'Isolated test database required.', 1;
SET XACT_ABORT OFF;
BEGIN TRANSACTION;

INSERT dbo.Users(UserName, PasswordHash, Role) VALUES(N'ci_schema_test', N'not-a-login-password', N'Admin');
DECLARE @userId INT = SCOPE_IDENTITY();
INSERT dbo.Customers(Name, NameAr, Mobile) VALUES(N'Test Customer', N'عميل اختبار', N'12345678');
DECLARE @customerId BIGINT = SCOPE_IDENTITY();
INSERT dbo.ProductCategories(Name, NameAr) VALUES(N'Test Dress', N'فستان اختبار');
DECLARE @categoryId INT = SCOPE_IDENTITY();
INSERT dbo.Products(CategoryId, Name, NameAr, ProductType, RegularPrice)
VALUES(@categoryId, N'Test Garment', N'ثوب اختبار', N'Ready-made', 25.000);
DECLARE @productId INT = SCOPE_IDENTITY();
INSERT dbo.ProductSizes(ProductId, Name) VALUES(@productId, N'M');
DECLARE @sizeId INT = SCOPE_IDENTITY();
INSERT dbo.MeasurementDefinitions(ProductId, Name, NameAr, Unit, IsRequired)
VALUES(@productId, N'Chest', N'الصدر', N'cm', 1);
DECLARE @measurementId INT = SCOPE_IDENTITY();

INSERT dbo.ServiceCategories(Name, NameAr) VALUES(N'Alterations test', N'تعديلات اختبار');
DECLARE @serviceCategoryId INT = SCOPE_IDENTITY();
INSERT dbo.AlterationServices(ServiceCategoryId, Name, NameAr, Price)
VALUES(@serviceCategoryId, N'Hem test', N'تقصير اختبار', 2.500);
DECLARE @serviceId INT = SCOPE_IDENTITY();

INSERT dbo.OrderRequests(UserId, Fingerprint) VALUES(@userId, REPLICATE('A', 64));
DECLARE @requestId BIGINT = SCOPE_IDENTITY();
INSERT dbo.Orders(RequestId, CustomerId, CreatedByUserId,
    CustomerName, CustomerNameAr, CustomerMobile, DeliveryDate, Subtotal, Total)
VALUES(@requestId, @customerId, @userId,
    N'Test Customer', N'عميل اختبار', N'12345678',
    DATEADD(DAY, 1, CAST(GETUTCDATE() AS DATE)), 27.500, 27.500);
DECLARE @orderId BIGINT = SCOPE_IDENTITY();
INSERT dbo.OrderInvoiceSnapshots(OrderId, BusinessName, BusinessNameAr, Mobile, LogoPath)
VALUES (@orderId, N'Old IBDAA Branding', N'اسم المتجر القديم', N'12345678',
    N'/uploads/shop/logo-historical.png');
IF NOT EXISTS (SELECT 1 FROM dbo.OrderInvoiceSnapshots WHERE OrderId = @orderId
    AND BusinessName = N'Old IBDAA Branding')
    THROW 51106, 'Order branding snapshot was not preserved.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderId = @orderId)
    THROW 51071, 'Numeric invoice ID must match the saved order identity.', 1;

INSERT dbo.OrderItems(OrderId, ItemType, ProductId, SizeId, ProductCode,
    Name, NameAr, SizeName, Quantity, UnitPrice, LineTotal)
VALUES(@orderId, N'Product', @productId, @sizeId, N'PRD-1',
    N'Test Garment', N'ثوب اختبار', N'M', 1, 25.000, 25.000);
DECLARE @itemId BIGINT = SCOPE_IDENTITY();
INSERT dbo.OrderItemMeasurements(OrderItemId, MeasurementId, Name, NameAr, Unit, Value)
VALUES(@itemId, @measurementId, N'Chest', N'الصدر', N'cm', 102.00);
INSERT dbo.OrderItems(OrderId, ItemType, ServiceId, Name, NameAr, Quantity, UnitPrice, LineTotal)
VALUES(@orderId, N'Service', @serviceId, N'Hem test', N'تقصير اختبار', 1, 2.500, 2.500);
INSERT dbo.OrderStatusHistory(OrderId, UserId, NewStatus) VALUES(@orderId, @userId, N'New');

DECLARE @blocked BIT = 0;
BEGIN TRY
    INSERT dbo.Orders(RequestId, CustomerId, CreatedByUserId,
        CustomerName, CustomerNameAr, CustomerMobile, DeliveryDate, Subtotal, Total)
    VALUES(@requestId, @customerId, @userId,
        N'Test Customer', N'عميل اختبار', N'12345678',
        DATEADD(DAY, 1, CAST(GETUTCDATE() AS DATE)), 27.500, 27.500);
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
    SET @blocked = 1;
END CATCH;
IF @blocked <> 1 THROW 51072, 'Duplicate order request was accepted.', 1;

IF (SELECT COUNT(*) FROM dbo.OrderItemMeasurements WHERE OrderItemId = @itemId) <> 1
    THROW 51073, 'Per-item measurement was not recorded.', 1;

ROLLBACK TRANSACTION;
SELECT N'Unpaid-order schema smoke passed; all test rows rolled back.' AS Result;
