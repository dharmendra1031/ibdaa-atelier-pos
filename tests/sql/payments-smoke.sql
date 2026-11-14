/* TEST ONLY: ledger constraints; rolls back all fixtures. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51110, 'Set exact TEST database name.', 1;
SET XACT_ABORT OFF;
BEGIN TRANSACTION;

INSERT dbo.Users(UserName, PasswordHash, Role) VALUES(N'ci_payment_test', N'not-a-login-password', N'Admin');
DECLARE @userId INT = SCOPE_IDENTITY();
INSERT dbo.Customers(Name, NameAr, Mobile)
    VALUES(N'Payment Customer', N'عميل الدفع', N'29990000');
DECLARE @customerId BIGINT = SCOPE_IDENTITY();

INSERT dbo.OrderRequests(UserId, Fingerprint)
    VALUES(@userId, REPLICATE('B', 64));
DECLARE @orderRequestId BIGINT = SCOPE_IDENTITY();
INSERT dbo.Orders(RequestId, CustomerId, CreatedByUserId,
    CustomerName, CustomerNameAr, CustomerMobile, DeliveryDate, Subtotal, Total)
    VALUES(@orderRequestId, @customerId, @userId,
        N'Payment Customer', N'عميل الدفع', N'29990000',
        DATEADD(DAY, 1, CAST(GETUTCDATE() AS DATE)), 25.000, 25.000);
DECLARE @orderId BIGINT = SCOPE_IDENTITY();

INSERT dbo.PaymentRequests(OrderId, UserId, Fingerprint)
    VALUES(@orderId, @userId, REPLICATE('A', 64));
DECLARE @requestId BIGINT = SCOPE_IDENTITY();
INSERT dbo.PaymentBatches(RequestId, OrderId, Total) VALUES(@requestId, @orderId, 20.000);
DECLARE @batchId BIGINT = SCOPE_IDENTITY();
INSERT dbo.OrderPayments(PaymentBatchId, OrderId, Method, Amount, Tendered, Change, ReceivedByUserId)
    VALUES(@batchId, @orderId, N'Cash', 10.000, 12.000, 2.000, @userId);
INSERT dbo.OrderPayments(PaymentBatchId, OrderId, Method, Amount, Tendered, Change, ReceivedByUserId)
    VALUES(@batchId, @orderId, N'KNET', 10.000, 10.000, 0.000, @userId);

IF (SELECT SUM(Amount) FROM dbo.OrderPayments WHERE OrderId = @orderId) <> 20.000
    THROW 51111, 'Split payment ledger total mismatch.', 1;
IF (SELECT SUM(Change) FROM dbo.OrderPayments WHERE OrderId = @orderId) <> 2.000
    THROW 51112, 'Cash change calculation mismatch.', 1;

DECLARE @blocked BIT = 0;
BEGIN TRY
    INSERT dbo.PaymentBatches(RequestId, OrderId, Total)
        VALUES(@requestId, @orderId, 20.000);
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
    SET @blocked = 1;
END CATCH;
IF @blocked <> 1 THROW 51113, 'Duplicate payment batch request accepted.', 1;

SET @blocked = 0;
BEGIN TRY
    INSERT dbo.OrderPayments(PaymentBatchId, OrderId, Method, Amount, Tendered, Change, ReceivedByUserId)
        VALUES(@batchId, @orderId, N'KNET', 1.000, 2.000, 1.000, @userId);
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 547 THROW;
    SET @blocked = 1;
END CATCH;
IF @blocked <> 1 THROW 51114, 'Non-cash over-tender was accepted.', 1;

ROLLBACK TRANSACTION;
SELECT N'Payment SQL constraints passed; fixtures rolled back.' AS Result;
