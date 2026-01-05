-- Script to manually add missing columns to fix the schema mismatch
-- Run this script directly against your database if migrations fail

-- Add OnePortalId to Enterprises table (if not exists)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Enterprises]') AND name = 'OnePortalId')
BEGIN
    ALTER TABLE [Enterprises] ADD [OnePortalId] int NULL;
    CREATE INDEX [IX_Enterprises_OnePortalId] ON [Enterprises] ([OnePortalId]);
END
GO

-- Add missing columns to GuestInvitations table (if not exists)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GuestInvitations]') AND name = 'EnterpriseUserId')
BEGIN
    ALTER TABLE [GuestInvitations] ADD [EnterpriseUserId] int NULL;
    CREATE INDEX [IX_GuestInvitations_EnterpriseUserId] ON [GuestInvitations] ([EnterpriseUserId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GuestInvitations]') AND name = 'HostEmail')
BEGIN
    ALTER TABLE [GuestInvitations] ADD [HostEmail] nvarchar(200) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GuestInvitations]') AND name = 'HostName')
BEGIN
    ALTER TABLE [GuestInvitations] ADD [HostName] nvarchar(200) NOT NULL DEFAULT '';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GuestInvitations]') AND name = 'HostType')
BEGIN
    ALTER TABLE [GuestInvitations] ADD [HostType] int NOT NULL DEFAULT 0;
END
GO

-- Make HostId nullable (if it's not already)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GuestInvitations]') AND name = 'HostId' AND is_nullable = 0)
BEGIN
    ALTER TABLE [GuestInvitations] ALTER COLUMN [HostId] int NULL;
END
GO

-- Migrate existing data: populate HostName and HostEmail from ApplicationUser (if HostName is empty)
UPDATE gi
SET 
    gi.HostName = COALESCE(au.FirstName + ' ' + au.LastName, 'Unknown'),
    gi.HostEmail = au.Email,
    gi.HostType = 0
FROM GuestInvitations gi
INNER JOIN AspNetUsers au ON gi.HostId = au.Id
WHERE gi.HostName = '' OR gi.HostName IS NULL;
GO

-- Set default HostName for any records that couldn't be migrated
UPDATE GuestInvitations
SET HostName = 'Unknown', HostType = 0
WHERE HostName = '' OR HostName IS NULL;
GO










