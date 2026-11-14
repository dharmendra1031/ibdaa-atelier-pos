/* TEST ONLY: refund constraints, rolled back before return. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51130, 'Set the exact approved TEST database name.', 1;
SET XACT_ABORT OFF;
BEGIN TRANSACTION;

INSERT dbo.Users(UserName, PasswordHash, Role)
    VALUES(N'ci_refund_admin', N'not-a-login-password', N'Admin');
DECLARE @userId INT = SCOPE_IDENTITY();
INSERT dbo.Customers(Name, NameAr, Mobile)
    VALUES(N'Refund Customer', N'عميل الاسترداد', N'29991000');
DECLARE @customerId BIGINT = SCOPE_IDENTITY();
INSERT dbo.OrderRequests(UserId, Fingerprint)
    VALUES(@userId, REPLICATE('B', 64));
DECLARE @orderRequestId BIGINT = SCOPE_IDENTITY();
INSERT dbo.Orders(RequestId, CustomerId, CreatedByUserId,
    CustomerName, CustomerNameAr, CustomerMobile, DeliveryDate, Subtotal, Total)
    VALUES(@orderRequestId, @customerId, @userId,
        N'Refund Customer', N'عميل الاسترداد', N'29991000',
        DATEADD(DAY, 1, CAST(GETUTCDATE() AS DATE)), 25.000, 25.000);
DECLARE @orderId BIGINT = SCOPE_IDENTITY();

INSERT dbo.PaymentRequests(OrderId, UserId, Fingerprint)
    VALUES(@orderId, @userId, REPLICATE('A', 64));
DECLARE @paymentRequestId BIGINT = SCOPE_IDENTITY();
INSERT dbo.PaymentBatches(RequestId, OrderId, Total)
    VALUES(@paymentRequestId, @orderId, 10.000);
DECLARE @batchId BIGINT = SCOPE_IDENTITY();
INSERT dbo.OrderPayments(PaymentBatchId, OrderId, Method, Amount, Tendered, Change, ReceivedByUserId)
    VALUES(@batchId, @orderId, N'Cash', 10.000, 10.000, 0.000, @userId);

INSERT dbo.RefundRequests(OrderId, UserId, Fingerprint)
    VALUES(@orderId, @userId, REPLICATE('C', 64));
DECLARE @refundRequestId BIGINT = SCOPE_IDENTITY();
INSERT dbo.OrderRefunds(RequestId, OrderId, Method, Amount, Reason, ApprovedByUserId)
    VALUES(@refundRequestId, @orderId, N'Cash', 4.000,
        N'Approved manual refund for garment alteration', @userId);

IF (SELECT SUM(Amount) FROM dbo.OrderPayments WHERE OrderId = @orderId)
    - (SELECT SUM(Amount) FROM dbo.OrderRefunds WHERE OrderId = @orderId) <> 6.000
    THROW 51131, 'Net receipts did not account for refund.', 1;

DECLARE @blocked BIT = 0;
BEGIN TRY
    INSERT dbo.OrderRefunds(RequestId, OrderId, Method, Amount, Reason, ApprovedByUserId)
        VALUES(@refundRequestId, @orderId, N'Cash', 4.000,
            N'Attempted duplicate manual refund for test order', @userId);
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
    SET @blocked = 1;
END CATCH;
IF @blocked <> 1
    THROW 51132, 'Duplicate refund request was accepted.', 1;

SET @blocked = 0;
INSERT dbo.RefundRequests(OrderId, UserId, Fingerprint)
    VALUES(@orderId, @userId, REPLICATE('D', 64));
DECLARE @invalidRequestId BIGINT = SCOPE_IDENTITY();
BEGIN TRY
    INSERT dbo.OrderRefunds(RequestId, OrderId, Method, Amount, Reason, ApprovedByUserId)
        VALUES(@invalidRequestId, @orderId, N'Cash', -1.000,
            N'Negative refund is not allowed in the ledger', @userId);
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 547 THROW;
    SET @blocked = 1;
END CATCH;
IF @blocked <> 1
    THROW 51133, 'Invalid negative refund was accepted.', 1;

ROLLBACK TRANSACTION;
SELECT N'Refund SQL constraints passed; fixtures rolled back.' AS Result;
