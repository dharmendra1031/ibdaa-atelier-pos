/* Apply to an approved, backed-up database before deploying the matching code. */
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.ShopSettings', N'U') IS NULL
    THROW 51370, 'ShopSettings is required.', 1;
IF COL_LENGTH(N'dbo.ShopSettings', N'OpenInvoicePdfInNewTab') IS NOT NULL
    THROW 51371, 'Invoice print mode is already installed.', 1;

ALTER TABLE dbo.ShopSettings ADD OpenInvoicePdfInNewTab bit NOT NULL
    CONSTRAINT DF_ShopSettings_OpenInvoicePdfInNewTab DEFAULT (1);
