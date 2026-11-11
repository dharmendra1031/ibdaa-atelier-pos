/* Apply manually on an approved TEST database only after 011_unpaid_orders.sql. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51100, 'Set the exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.PaymentRequests', N'U') IS NOT NULL
 OR OBJECT_ID(N'dbo.PaymentBatches', N'U') IS NOT NULL
 OR OBJECT_ID(N'dbo.OrderPayments', N'U') IS NOT NULL
    THROW 51101, 'Payment tables already exist. Review previous migrations.', 1;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
    THROW 51102, 'Create the order ledger before payment tables.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.PaymentRequests
(
    RequestId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentRequests PRIMARY KEY,
    OrderId BIGINT NOT NULL REFERENCES dbo.Orders(OrderId),
    UserId INT NOT NULL REFERENCES dbo.Users(UserId),
    Fingerprint CHAR(64) NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_PaymentRequests_Order ON dbo.PaymentRequests(OrderId, RequestId);

CREATE TABLE dbo.PaymentBatches
(
    PaymentBatchId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentBatches PRIMARY KEY,
    RequestId BIGINT NOT NULL REFERENCES dbo.PaymentRequests(RequestId),
    OrderId BIGINT NOT NULL REFERENCES dbo.Orders(OrderId),
    Total DECIMAL(18,3) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_PaymentBatches_Total CHECK (Total > 0)
);
CREATE UNIQUE INDEX UX_PaymentBatches_Request ON dbo.PaymentBatches(RequestId);

CREATE TABLE dbo.OrderPayments
(
    PaymentId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderPayments PRIMARY KEY,
    PaymentBatchId BIGINT NOT NULL REFERENCES dbo.PaymentBatches(PaymentBatchId),
    OrderId BIGINT NOT NULL REFERENCES dbo.Orders(OrderId),
    Method NVARCHAR(20) NOT NULL,
    Amount DECIMAL(18,3) NOT NULL,
    Tendered DECIMAL(18,3) NOT NULL,
    Change DECIMAL(18,3) NOT NULL,
    ReceivedByUserId INT NOT NULL REFERENCES dbo.Users(UserId),
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_OrderPayments_Method CHECK (Method IN
        (N'Cash', N'KNET', N'Card', N'BankTransfer', N'Other')),
    CONSTRAINT CK_OrderPayments_Amounts CHECK
        (Amount > 0 AND Tendered >= Amount AND Change = Tendered - Amount AND
         (Method = N'Cash' OR (Tendered = Amount AND Change = 0)))
);
CREATE INDEX IX_OrderPayments_Order ON dbo.OrderPayments(OrderId, PaymentId);
CREATE INDEX IX_OrderPayments_MethodDate ON dbo.OrderPayments(Method, CreatedAtUtc);

COMMIT TRANSACTION;
SELECT N'Payment ledger ready; no payments seeded.' AS Result;
