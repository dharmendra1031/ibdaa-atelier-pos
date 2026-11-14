/* Verify the print queue without modifying business records. */
DECLARE @ExpectedDatabase sysname = N'__SET_TEST_DATABASE_NAME__';
IF @ExpectedDatabase = N'__SET_TEST_DATABASE_NAME__' OR DB_NAME() <> @ExpectedDatabase
    THROW 51420, 'Set the approved database name.', 1;
IF OBJECT_ID(N'dbo.PrintTerminals', N'U') IS NULL OR OBJECT_ID(N'dbo.PrintJobs', N'U') IS NULL
    THROW 51421, 'Print queue tables are missing.', 1;
IF COLUMNPROPERTY(OBJECT_ID(N'dbo.PrintTerminals'), 'TerminalId', 'IsIdentity') <> 1 OR
   COLUMNPROPERTY(OBJECT_ID(N'dbo.PrintJobs'), 'PrintJobId', 'IsIdentity') <> 1
    THROW 51422, 'Print queue IDs must be numeric identities.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.PrintJobs') AND name = N'UQ_PrintJobs_InvoiceTerminal')
    THROW 51423, 'Print queue deduplication constraint is missing.', 1;
SELECT N'Print terminal and queue schema verified.' AS Result;
