/* Apply only to an approved, backed-up database after 040. Replace the test database placeholder. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51410, 'Set the exact approved database name before applying the print-agent migration.', 1;
IF OBJECT_ID(N'dbo.PrintJobs', N'U') IS NOT NULL OR OBJECT_ID(N'dbo.PrintTerminals', N'U') IS NOT NULL
    THROW 51411, 'Print agent queue is already installed.', 1;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
    THROW 51412, 'Install the order schema first.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.PrintTerminals
(
    TerminalId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PrintTerminals PRIMARY KEY,
    Name NVARCHAR(80) NOT NULL,
    TokenHash CHAR(64) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_PrintTerminals_Active DEFAULT (1),
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PrintTerminals_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UX_PrintTerminals_TokenHash UNIQUE(TokenHash)
);

CREATE TABLE dbo.PrintJobs
(
    PrintJobId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PrintJobs PRIMARY KEY,
    TerminalId INT NOT NULL,
    OrderId BIGINT NOT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_PrintJobs_Status DEFAULT ('Pending'),
    Attempts INT NOT NULL CONSTRAINT DF_PrintJobs_Attempts DEFAULT (0),
    ErrorMessage NVARCHAR(300) NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PrintJobs_Created DEFAULT SYSUTCDATETIME(),
    StartedAtUtc DATETIME2 NULL,
    FinishedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_PrintJobs_Terminal FOREIGN KEY (TerminalId) REFERENCES dbo.PrintTerminals(TerminalId),
    CONSTRAINT FK_PrintJobs_Order FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId),
    CONSTRAINT UQ_PrintJobs_InvoiceTerminal UNIQUE (TerminalId, OrderId),
    CONSTRAINT CK_PrintJobs_Status CHECK (Status IN ('Pending', 'Processing', 'Completed', 'Failed')),
    CONSTRAINT CK_PrintJobs_Attempts CHECK (Attempts >= 0)
);

CREATE INDEX IX_PrintJobs_Pending ON dbo.PrintJobs(TerminalId, Status, PrintJobId);
COMMIT TRANSACTION;
