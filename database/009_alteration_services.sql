/* IBDAA alteration service masters. Manual application in the approved TEST database only. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51050, 'Set the exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.ServiceCategories', N'U') IS NOT NULL
    OR OBJECT_ID(N'dbo.AlterationServices', N'U') IS NOT NULL
    THROW 51051, 'Service master tables already exist.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.ServiceCategories
(
    ServiceCategoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ServiceCategories PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    NameAr NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_ServiceCategories_IsActive DEFAULT (1),
    CONSTRAINT CK_ServiceCategories_Valid CHECK (
        LEN(LTRIM(RTRIM(Name))) > 0 AND LEN(LTRIM(RTRIM(NameAr))) > 0)
);
CREATE UNIQUE INDEX UX_ServiceCategories_Name ON dbo.ServiceCategories(Name);

CREATE TABLE dbo.AlterationServices
(
    ServiceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AlterationServices PRIMARY KEY,
    ServiceCategoryId INT NOT NULL,
    Name NVARCHAR(120) NOT NULL,
    NameAr NVARCHAR(120) NOT NULL,
    Description NVARCHAR(1000) NULL,
    DescriptionAr NVARCHAR(1000) NULL,
    Price DECIMAL(18,3) NOT NULL,
    EstimatedMinutes INT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_AlterationServices_IsActive DEFAULT (1),
    CONSTRAINT FK_AlterationServices_Category FOREIGN KEY (ServiceCategoryId)
        REFERENCES dbo.ServiceCategories(ServiceCategoryId),
    CONSTRAINT CK_AlterationServices_Price CHECK (Price >= 0),
    CONSTRAINT CK_AlterationServices_Minutes CHECK (EstimatedMinutes IS NULL OR EstimatedMinutes > 0),
    CONSTRAINT CK_AlterationServices_Name CHECK (
        LEN(LTRIM(RTRIM(Name))) > 0 AND LEN(LTRIM(RTRIM(NameAr))) > 0)
);
CREATE UNIQUE INDEX UX_AlterationServices_CategoryName
    ON dbo.AlterationServices(ServiceCategoryId, Name);
CREATE INDEX IX_AlterationServices_Active
    ON dbo.AlterationServices(IsActive, ServiceCategoryId, ServiceId);

COMMIT TRANSACTION;
SELECT N'Alteration service tables ready; no example prices seeded.' AS Result;
