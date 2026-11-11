/* Read-only schema verification against the approved isolated TEST database. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51203, 'Set the exact approved TEST database name.', 1;
IF COLUMNPROPERTY(OBJECT_ID(N'dbo.CashRegisterSessions'), 'SessionId', 'IsIdentity') <> 1
    OR COLUMNPROPERTY(OBJECT_ID(N'dbo.CashExpenseRequests'), 'RequestId', 'IsIdentity') <> 1
    OR COLUMNPROPERTY(OBJECT_ID(N'dbo.CashExpenses'), 'ExpenseId', 'IsIdentity') <> 1
    THROW 51204, 'Cash register identity keys are missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.CashRegisterSessions')
    AND name = N'UX_CashRegisterSessions_Open')
    THROW 51205, 'Unique open-register index is missing.', 1;
IF COL_LENGTH(N'dbo.ShopSettings', N'RequireRegisterOpen') IS NULL
    OR COL_LENGTH(N'dbo.OrderPayments', N'RegisterSessionId') IS NULL
    OR COL_LENGTH(N'dbo.OrderRefunds', N'RegisterSessionId') IS NULL
    THROW 51206, 'Register configuration or payment linkage is missing.', 1;

SELECT COUNT_BIG(*) AS SessionCount FROM dbo.CashRegisterSessions;
SELECT COUNT_BIG(*) AS ExpenseCount FROM dbo.CashExpenses;
