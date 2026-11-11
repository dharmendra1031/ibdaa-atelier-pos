/* Isolated TEST database only; run manually after 001 and 003. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51040, 'Set the exact approved TEST database name.', 1;
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL
    THROW 51041, 'Customers table already exists.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;
CREATE TABLE dbo.Customers
(
    CustomerId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL,
    NameAr NVARCHAR(120) NOT NULL,
    Mobile NVARCHAR(20) NOT NULL,
    AlternateMobile NVARCHAR(20) NULL,
    Address NVARCHAR(500) NULL,
    Notes NVARCHAR(1000) NULL,
    RegisteredAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Customers_Registered DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Customers_Names CHECK (
        LEN(LTRIM(RTRIM(Name))) > 0 AND LEN(LTRIM(RTRIM(NameAr))) > 0),
    CONSTRAINT CK_Customers_Mobile CHECK (LEN(Mobile) BETWEEN 8 AND 15)
);
CREATE UNIQUE INDEX UX_Customers_Mobile ON dbo.Customers(Mobile);
COMMIT TRANSACTION;
SELECT N'Customers schema ready, no customer data imported' AS Result;
