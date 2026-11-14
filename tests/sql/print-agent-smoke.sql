/* CI-only queue smoke. All test data is rolled back. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51430, 'Isolated test database required.', 1;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT dbo.PrintTerminals (Name, TokenHash)
VALUES (N'CI Printer', REPLICATE('A', 64));
DECLARE @TerminalId INT = SCOPE_IDENTITY();
IF @TerminalId <= 0 THROW 51431, 'Terminal ID is not numeric identity.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.PrintTerminals WHERE TerminalId = @TerminalId AND IsActive = 1)
    THROW 51432, 'Terminal provisioning failed.', 1;
ROLLBACK TRANSACTION;
SELECT N'Print terminal provisioning smoke passed.' AS Result;
