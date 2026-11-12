/* Apply manually only to an approved, backed-up TEST database after scripts 001, 003, 007, 009. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51060, 'Set exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL OR OBJECT_ID(N'dbo.OrderRequests', N'U') IS NOT NULL
    THROW 51061, 'Order tables exist. Review previous migrations.', 1;
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL OR OBJECT_ID(N'dbo.AlterationServices', N'U') IS NULL
    THROW 51062, 'Apply customer and alteration masters migrations first.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;
CREATE TABLE dbo.OrderRequests
(
    RequestId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderRequests PRIMARY KEY,
    UserId INT NOT NULL REFERENCES dbo.Users(UserId),
    Fingerprint CHAR(64) NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_OrderRequests_User ON dbo.OrderRequests(UserId, CreatedAtUtc DESC);

CREATE TABLE dbo.Orders
(
    OrderId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    RequestId BIGINT NOT NULL REFERENCES dbo.OrderRequests(RequestId),
    CustomerId BIGINT NOT NULL REFERENCES dbo.Customers(CustomerId),
    CreatedByUserId INT NOT NULL REFERENCES dbo.Users(UserId),
    CustomerName NVARCHAR(120) NOT NULL,
    CustomerNameAr NVARCHAR(120) NOT NULL,
    CustomerMobile NVARCHAR(20) NOT NULL,
    DeliveryDate DATE NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT N'New',
    Total DECIMAL(18,3) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Orders_Total CHECK (Total >= 0),
    CONSTRAINT CK_Orders_Status CHECK (Status IN
        (N'New', N'Confirmed', N'In Processing', N'Ready for Delivery', N'Delivered', N'Cancelled'))
);
CREATE UNIQUE INDEX UX_Orders_RequestId ON dbo.Orders(RequestId);
CREATE INDEX IX_Orders_Customer ON dbo.Orders(CustomerId, CreatedAtUtc DESC);
CREATE INDEX IX_Orders_Delivery ON dbo.Orders(DeliveryDate, Status);

CREATE TABLE dbo.OrderItems
(
    OrderItemId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY,
    OrderId BIGINT NOT NULL REFERENCES dbo.Orders(OrderId),
    ItemType NVARCHAR(10) NOT NULL,
    ProductId INT NULL REFERENCES dbo.Products(ProductId),
    ServiceId INT NULL REFERENCES dbo.AlterationServices(ServiceId),
    SizeId INT NULL REFERENCES dbo.ProductSizes(SizeId),
    ProductCode NVARCHAR(16) NOT NULL CONSTRAINT DF_OrderItems_Code DEFAULT N'',
    Name NVARCHAR(150) NOT NULL,
    NameAr NVARCHAR(150) NOT NULL,
    SizeName NVARCHAR(30) NULL,
    Notes NVARCHAR(1000) NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,3) NOT NULL,
    LineTotal DECIMAL(18,3) NOT NULL,
    CONSTRAINT CK_OrderItems_Type CHECK (
        (ItemType = N'Product' AND ProductId IS NOT NULL AND ServiceId IS NULL) OR
        (ItemType = N'Service' AND ProductId IS NULL AND ServiceId IS NOT NULL AND SizeId IS NULL)),
    CONSTRAINT CK_OrderItems_Amount CHECK (Quantity > 0 AND UnitPrice >= 0 AND LineTotal >= 0)
);
CREATE INDEX IX_OrderItems_Order ON dbo.OrderItems(OrderId, OrderItemId);

CREATE TABLE dbo.OrderItemMeasurements
(
    OrderMeasurementId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItemMeasurements PRIMARY KEY,
    OrderItemId BIGINT NOT NULL REFERENCES dbo.OrderItems(OrderItemId),
    MeasurementId INT NOT NULL REFERENCES dbo.MeasurementDefinitions(MeasurementId),
    Name NVARCHAR(100) NOT NULL,
    NameAr NVARCHAR(100) NOT NULL,
    Unit NVARCHAR(20) NOT NULL,
    Value DECIMAL(9,2) NOT NULL,
    CONSTRAINT UQ_OrderMeasurements_ItemDefinition UNIQUE (OrderItemId, MeasurementId),
    CONSTRAINT CK_OrderMeasurements_Value CHECK (Value >= 0)
);

CREATE TABLE dbo.OrderStatusHistory
(
    HistoryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderStatusHistory PRIMARY KEY,
    OrderId BIGINT NOT NULL REFERENCES dbo.Orders(OrderId),
    UserId INT NOT NULL REFERENCES dbo.Users(UserId),
    OldStatus NVARCHAR(30) NULL,
    NewStatus NVARCHAR(30) NOT NULL,
    ChangedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_OrderStatusHistory_Order ON dbo.OrderStatusHistory(OrderId, HistoryId);

COMMIT TRANSACTION;
SELECT N'Unpaid order schema ready. No orders or payments created.' AS Result;
