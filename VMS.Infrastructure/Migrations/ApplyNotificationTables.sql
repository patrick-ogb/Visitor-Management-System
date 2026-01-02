-- Migration: AddNotificationTables
-- This script creates the notification-related tables: Notifications, NotificationEvents, and UserPresences

-- Create NotificationEvents table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NotificationEvents]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[NotificationEvents] (
        [Id] uniqueidentifier NOT NULL,
        [EventType] nvarchar(50) NOT NULL,
        [ReferenceId] int NOT NULL,
        [TargetUserId] int NOT NULL,
        [Processed] bit NOT NULL,
        [AdditionalData] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_NotificationEvents] PRIMARY KEY ([Id])
    );
    
    CREATE INDEX [IX_NotificationEvents_Processed] ON [dbo].[NotificationEvents] ([Processed]);
    CREATE INDEX [IX_NotificationEvents_ReferenceId] ON [dbo].[NotificationEvents] ([ReferenceId]);
    CREATE INDEX [IX_NotificationEvents_TargetUserId_Processed_CreatedAt] ON [dbo].[NotificationEvents] ([TargetUserId], [Processed], [CreatedAt]);
END
GO

-- Create Notifications table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] int NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [Message] nvarchar(500) NOT NULL,
        [IsRead] bit NOT NULL,
        [ReferenceId] int NULL,
        [DeliveryStatus] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_Notifications_ReferenceId] ON [dbo].[Notifications] ([ReferenceId]);
    CREATE INDEX [IX_Notifications_UserId_DeliveryStatus] ON [dbo].[Notifications] ([UserId], [DeliveryStatus]);
    CREATE INDEX [IX_Notifications_UserId_IsRead_CreatedAt] ON [dbo].[Notifications] ([UserId], [IsRead], [CreatedAt]);
END
GO

-- Create UserPresences table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserPresences]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserPresences] (
        [UserId] int NOT NULL,
        [IsOnline] bit NOT NULL,
        [LastHeartbeat] datetime2 NOT NULL,
        [ConnectionId] nvarchar(100) NULL,
        CONSTRAINT [PK_UserPresences] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_UserPresences_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END
GO

-- Add migration to history table
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20251224091512_AddNotificationTables')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20251224091512_AddNotificationTables', '8.0.0');
END
GO




