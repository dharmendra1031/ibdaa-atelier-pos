/* Apply manually to a backed-up, approved TEST database only. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51310, 'Set the approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.ShopSettings', N'U') IS NULL OR OBJECT_ID(N'dbo.Orders', N'U') IS NULL
    THROW 51311, 'Shop settings and orders are required.', 1;
IF COL_LENGTH(N'dbo.ShopSettings', 'EnablePosDiscount') IS NOT NULL
   OR COL_LENGTH(N'dbo.Orders', 'Subtotal') IS NOT NULL
   OR COL_LENGTH(N'dbo.Orders', 'DiscountAmount') IS NOT NULL
    THROW 51312, 'POS discount migration has already been applied or needs review.', 1;

SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
ALTER TABLE dbo.ShopSettings ADD EnablePosDiscount BIT NOT NULL
    CONSTRAINT DF_ShopSettings_EnablePosDiscount DEFAULT (0);
ALTER TABLE dbo.Orders ADD Subtotal DECIMAL(18,3) NOT NULL
    CONSTRAINT DF_Orders_Subtotal DEFAULT (0);
ALTER TABLE dbo.Orders ADD DiscountAmount DECIMAL(18,3) NOT NULL
    CONSTRAINT DF_Orders_DiscountAmount DEFAULT (0);
GO
UPDATE dbo.Orders SET Subtotal = Total;
ALTER TABLE dbo.Orders ADD CONSTRAINT CK_Orders_DiscountAmount
    CHECK (DiscountAmount >= 0 AND DiscountAmount <= Subtotal AND Total = Subtotal - DiscountAmount);
COMMIT TRANSACTION;
SELECT N'POS discount settings and order totals added.' AS Result;
