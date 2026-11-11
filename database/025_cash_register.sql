/*
 * Single-shop cash register sessions, expense requests and payment linkage.
 * Apply only to a backed-up, approved isolated TEST database after 023.
 */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51200, 'Set the exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.CashRegisterSessions', N'U') IS NOT NULL
    OR COL_LENGTH(N'dbo.ShopSettings', N'RequireRegisterOpen') IS NOT NULL
    THROW 51201, 'Cash register migration has already been applied.', 1;
IF OBJECT_ID(N'dbo.ShopSettings', N'U') IS NULL
    OR OBJECT_ID(N'dbo.OrderPayments', N'U') IS NULL
    OR OBJECT_ID(N'dbo.OrderRefunds', N'U') IS NULL
    THROW 51202, 'Apply the earlier ShopSettings, payment and refund migrations first.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.CashRegisterSessions
(
    SessionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashRegisterSessions PRIMARY KEY,
    OpenedByUserId INT NOT NULL,
    OpeningCash DECIMAL(18,3) NOT NULL,
    OpenedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_CashRegisterOpened DEFAULT SYSUTCDATETIME(),
    ClosedAtUtc DATETIME2 NULL,
    ClosedByUserId INT NULL,
    CountedCash DECIMAL(18,3) NULL,
    ExpectedCash DECIMAL(18,3) NULL,
    Difference DECIMAL(18,3) NULL,
    ApprovedByUserId INT NULL,
    ApprovedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_CashRegisterOpenedUser FOREIGN KEY (OpenedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_CashRegisterClosedUser FOREIGN KEY (ClosedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_CashRegisterApprovedUser FOREIGN KEY (ApprovedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_CashRegisterOpening CHECK (OpeningCash >= 0),
    CONSTRAINT CK_CashRegisterClosing CHECK (
        (ClosedAtUtc IS NULL AND ClosedByUserId IS NULL AND CountedCash IS NULL
            AND ExpectedCash IS NULL AND Difference IS NULL) OR
        (ClosedAtUtc IS NOT NULL AND ClosedByUserId IS NOT NULL AND CountedCash >= 0
            AND CountedCash IS NOT NULL AND ExpectedCash IS NOT NULL AND Difference IS NOT NULL
            AND Difference = CountedCash - ExpectedCash)),
    CONSTRAINT CK_CashRegisterApproval CHECK (
        (ApprovedAtUtc IS NULL AND ApprovedByUserId IS NULL) OR
        (ApprovedAtUtc IS NOT NULL AND ApprovedByUserId IS NOT NULL AND ClosedAtUtc IS NOT NULL))
);
CREATE UNIQUE INDEX UX_CashRegisterSessions_Open
    ON dbo.CashRegisterSessions(ClosedAtUtc) WHERE ClosedAtUtc IS NULL;

CREATE TABLE dbo.CashExpenseRequests
(
    RequestId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashExpenseRequests PRIMARY KEY,
    SessionId BIGINT NOT NULL,
    UserId INT NOT NULL,
    Fingerprint CHAR(64) NULL,
    CONSTRAINT FK_ExpenseRequestSession FOREIGN KEY (SessionId)
        REFERENCES dbo.CashRegisterSessions(SessionId),
    CONSTRAINT FK_ExpenseRequestUser FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
CREATE TABLE dbo.CashExpenses
(
    ExpenseId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashExpenses PRIMARY KEY,
    RequestId BIGINT NOT NULL,
    SessionId BIGINT NOT NULL,
    RecordedByUserId INT NOT NULL,
    Amount DECIMAL(18,3) NOT NULL,
    Reason NVARCHAR(500) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_CashExpenseTime DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CashExpenseRequest FOREIGN KEY (RequestId) REFERENCES dbo.CashExpenseRequests(RequestId),
    CONSTRAINT FK_CashExpenseSession FOREIGN KEY (SessionId) REFERENCES dbo.CashRegisterSessions(SessionId),
    CONSTRAINT FK_CashExpenseUser FOREIGN KEY (RecordedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_CashExpenseAmount CHECK (Amount > 0),
    CONSTRAINT CK_CashExpenseReason CHECK (LEN(LTRIM(RTRIM(Reason))) >= 10)
);
CREATE UNIQUE INDEX UX_CashExpenses_Request ON dbo.CashExpenses(RequestId);
CREATE INDEX IX_CashExpenses_Session ON dbo.CashExpenses(SessionId);

ALTER TABLE dbo.OrderPayments ADD RegisterSessionId BIGINT NULL;
ALTER TABLE dbo.OrderRefunds ADD RegisterSessionId BIGINT NULL;

ALTER TABLE dbo.OrderPayments ADD CONSTRAINT FK_OrderPayments_Register
    FOREIGN KEY (RegisterSessionId) REFERENCES dbo.CashRegisterSessions(SessionId);
ALTER TABLE dbo.OrderRefunds ADD CONSTRAINT FK_OrderRefunds_Register
    FOREIGN KEY (RegisterSessionId) REFERENCES dbo.CashRegisterSessions(SessionId);

CREATE INDEX IX_OrderPayments_Register ON dbo.OrderPayments(RegisterSessionId, Method);
CREATE INDEX IX_OrderRefunds_Register ON dbo.OrderRefunds(RegisterSessionId, Method);

ALTER TABLE dbo.ShopSettings ADD RequireRegisterOpen BIT NOT NULL
    CONSTRAINT DF_ShopSettings_RequireRegister DEFAULT (0);

COMMIT TRANSACTION;
SELECT N'Cash register schema ready; optional opening policy defaults to OFF.' AS Result;
