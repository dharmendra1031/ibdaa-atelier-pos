/* TEST ONLY: status history remarks are persisted. Rollback before exit. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51150, 'Set exact approved TEST database name.', 1;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT dbo.Users(UserName, PasswordHash, Role)
    VALUES(N'ci_status_admin', N'not-a-login-password', N'Admin');
DECLARE @userId INT = SCOPE_IDENTITY();
INSERT dbo.Customers(Name, NameAr, Mobile)
    VALUES(N'Status Customer', N'عميل الحالة', N'29991111');
DECLARE @customerId BIGINT = SCOPE_IDENTITY();
INSERT dbo.OrderRequests(UserId, Fingerprint)
    VALUES(@userId, REPLICATE('E', 64));
DECLARE @requestId BIGINT = SCOPE_IDENTITY();
INSERT dbo.Orders(RequestId, CustomerId, CreatedByUserId,
    CustomerName, CustomerNameAr, CustomerMobile, DeliveryDate, Subtotal, Total)
    VALUES(@requestId, @customerId, @userId, N'Status Customer', N'عميل الحالة',
        N'29991111', DATEADD(DAY, 1, CAST(GETUTCDATE() AS DATE)), 0.000, 0.000);
DECLARE @orderId BIGINT = SCOPE_IDENTITY();
UPDATE dbo.Orders SET Status = N'Confirmed' WHERE OrderId = @orderId;
INSERT dbo.OrderStatusHistory(OrderId, UserId, OldStatus, NewStatus, Remarks)
    VALUES(@orderId, @userId, N'New', N'Confirmed', N'Customer order verified');
DECLARE @historyId BIGINT = SCOPE_IDENTITY();

IF NOT EXISTS (SELECT 1 FROM dbo.OrderStatusHistory
    WHERE HistoryId = @historyId AND OrderId = @orderId AND OldStatus = N'New'
    AND NewStatus = N'Confirmed' AND Remarks = N'Customer order verified')
    THROW 51151, 'Status remarks or history snapshot missing.', 1;
ROLLBACK TRANSACTION;
SELECT N'Status history smoke passed; fixtures rolled back.' AS Result;
