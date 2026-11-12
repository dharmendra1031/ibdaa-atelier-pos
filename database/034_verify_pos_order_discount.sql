/* Read-only verification on the approved TEST database. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51313, 'Set the approved TEST database name.', 1;
IF COL_LENGTH(N'dbo.ShopSettings', 'EnablePosDiscount') IS NULL
   OR COL_LENGTH(N'dbo.Orders', 'Subtotal') IS NULL
   OR COL_LENGTH(N'dbo.Orders', 'DiscountAmount') IS NULL
    THROW 51314, 'POS discount columns are missing.', 1;
IF EXISTS (SELECT 1 FROM dbo.Orders
           WHERE DiscountAmount < 0 OR DiscountAmount > Subtotal OR Total <> Subtotal - DiscountAmount)
    THROW 51315, 'Order discount totals do not reconcile.', 1;
SELECT COUNT_BIG(*) AS OrdersChecked FROM dbo.Orders;
