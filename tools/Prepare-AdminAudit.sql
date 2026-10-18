-- Apply to MusicStoreAccounts. Independent of the catalog's EF migration.
-- Does not change users, passwords, roles, or existing audit rows.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.AdminRoleAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminRoleAudit
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AdminRoleAudit PRIMARY KEY,
        CreatedUtc datetime2 NOT NULL CONSTRAINT DF_AdminRoleAudit_CreatedUtc DEFAULT SYSUTCDATETIME(),
        ActorId nvarchar(128) NOT NULL,
        TargetId nvarchar(128) NOT NULL,
        Granted bit NOT NULL
    );
END;
COMMIT TRANSACTION;
