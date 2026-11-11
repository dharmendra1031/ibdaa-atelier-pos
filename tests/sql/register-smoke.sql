/* TEST ONLY. Cash register constraints are verified inside a rolled-back transaction. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51210, 'Approved isolated TEST database required.', 1;

SET XACT_ABORT OFF;
BEGIN TRANSACTION;

INSERT dbo.Users(UserName, PasswordHash, Role)
VALUES (N'ci_cash_register', N'not-a-login-password', N'Admin');
DECLARE @userId INT = SCOPE_IDENTITY();

INSERT dbo.CashRegisterSessions(OpenedByUserId, OpeningCash)
VALUES (@userId, 5.000);
DECLARE @sessionId BIGINT = SCOPE_IDENTITY();

DECLARE @blocked BIT = 0;
BEGIN TRY
    INSERT dbo.CashRegisterSessions(OpenedByUserId, OpeningCash)
    VALUES (@userId, 10.000);
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
    SET @blocked = 1;
END CATCH;
IF @blocked <> 1
    THROW 51211, 'More than one register was opened.', 1;

INSERT dbo.CashExpenseRequests(SessionId, UserId, Fingerprint)
VALUES (@sessionId, @userId, REPLICATE('E', 64));
DECLARE @requestId BIGINT = SCOPE_IDENTITY();

INSERT dbo.CashExpenses(RequestId, SessionId, RecordedByUserId, Amount, Reason)
VALUES (@requestId, @sessionId, @userId, 1.000,
    N'Approved test delivery cash expense');

SET @blocked = 0;
BEGIN TRY
    INSERT dbo.CashExpenses(RequestId, SessionId, RecordedByUserId, Amount, Reason)
    VALUES (@requestId, @sessionId, @userId, 1.000,
        N'Duplicate expense for the same request');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
    SET @blocked = 1;
END CATCH;
IF @blocked <> 1
    THROW 51212, 'Duplicate cash expense request was accepted.', 1;

UPDATE dbo.CashRegisterSessions
SET ClosedAtUtc = SYSUTCDATETIME(), ClosedByUserId = @userId,
    CountedCash = 4.500, ExpectedCash = 4.000, Difference = 0.500
WHERE SessionId = @sessionId;

IF NOT EXISTS (SELECT 1 FROM dbo.CashRegisterSessions
    WHERE SessionId = @sessionId AND Difference = 0.500 AND ClosedByUserId = @userId)
    THROW 51213, 'Cash closing values were not persisted.', 1;

ROLLBACK TRANSACTION;
SELECT N'Cash register constraints passed; all fixture rows were rolled back.' AS Result;
