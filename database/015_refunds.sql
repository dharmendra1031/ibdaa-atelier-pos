/* Approved TEST database only, manual migration after 013_payment_ledger.sql. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51120, 'Set the exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.RefundRequests', N'U') IS NOT NULL
 OR OBJECT_ID(N'dbo.OrderRefunds', N'U') IS NOT NULL
    THROW 51121, 'Refund ledger exists. Review existing migration.', 1;
IF OBJECT_ID(N'dbo.OrderPayments', N'U') IS NULL
    THROW 51122, 'Create payment ledger before refunds.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.RefundRequests
(
    RequestId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RefundRequests PRIMARY KEY,
    OrderId BIGINT NOT NULL REFERENCES dbo.Orders(OrderId),
    UserId INT NOT NULL REFERENCES dbo.Users(UserId),
    Fingerprint CHAR(64) NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_RefundRequests_Order ON dbo.RefundRequests(OrderId, RequestId);

CREATE TABLE dbo.OrderRefunds
(
    RefundId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderRefunds PRIMARY KEY,
    RequestId BIGINT NOT NULL REFERENCES dbo.RefundRequests(RequestId),
    OrderId BIGINT NOT NULL REFERENCES dbo.Orders(OrderId),
    Method NVARCHAR(20) NOT NULL,
    Amount DECIMAL(18,3) NOT NULL,
    Reason NVARCHAR(500) NOT NULL,
    ApprovedByUserId INT NOT NULL REFERENCES dbo.Users(UserId),
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_OrderRefunds_Method CHECK (Method IN
        (N'Cash', N'KNET', N'Card', N'BankTransfer', N'Other')),
    CONSTRAINT CK_OrderRefunds_Amount CHECK (Amount > 0),
    CONSTRAINT CK_OrderRefunds_Reason CHECK (LEN(LTRIM(RTRIM(Reason))) >= 10)
);
CREATE UNIQUE INDEX UX_OrderRefunds_Request ON dbo.OrderRefunds(RequestId);
CREATE INDEX IX_OrderRefunds_Order ON dbo.OrderRefunds(OrderId, RefundId);
CREATE INDEX IX_OrderRefunds_MethodDate ON dbo.OrderRefunds(Method, CreatedAtUtc);

COMMIT TRANSACTION;
SELECT N'Refund tables ready; no refund data seeded.' AS Result;
