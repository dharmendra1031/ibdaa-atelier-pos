/* Manual migration. Apply only to the named and backed-up TEST database. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51100, 'Set the exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.OrderInvoiceSnapshots', N'U') IS NOT NULL
    THROW 51101, 'Invoice snapshot table already exists.', 1;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
    THROW 51102, 'Order schema must be installed first.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;
CREATE TABLE dbo.OrderInvoiceSnapshots
(
    InvoiceSnapshotId BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_OrderInvoiceSnapshots PRIMARY KEY,
    OrderId BIGINT NOT NULL
        CONSTRAINT FK_OrderInvoiceSnapshots_Order REFERENCES dbo.Orders(OrderId),
    BusinessName NVARCHAR(150) NOT NULL,
    BusinessNameAr NVARCHAR(150) NOT NULL,
    Address NVARCHAR(500) NULL,
    AddressAr NVARCHAR(500) NULL,
    Mobile NVARCHAR(25) NULL,
    Email NVARCHAR(150) NULL,
    LogoPath NVARCHAR(260) NULL,
    CONSTRAINT UX_OrderInvoiceSnapshots_Order UNIQUE (OrderId)
);
COMMIT TRANSACTION;
SELECT N'Immutable invoice branding snapshot schema ready.' AS Result;
