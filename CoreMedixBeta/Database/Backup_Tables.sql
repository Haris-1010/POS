-- =============================================
-- Automatic Database Backup & Restore Tables
-- Pharmacy Management System (CoreMedixBeta)
-- Run this script in SQL Server Management Studio
-- against the POS database (see Web.config connection string)
-- Safe to run multiple times (idempotent).
--
-- Schema/column names below match the live code path exactly:
--   Models\BackupSettings\BackupSetting.cs        -> Settings.BackupSettings
--   Models\BackupSettings\BackupHistoryEntry.cs    -> Settings.BackupHistory
--   Models\BackupSettings\BackupAuditLogEntry.cs   -> Settings.BackupAuditLog
--   Repositories\BackupSettingsRepository.cs       -> all the SQL text above
--   Controllers\BackupSettingsController.cs        -> the permission keys below
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Settings')
    EXEC('CREATE SCHEMA Settings');
GO

-- =============================================
-- Table 1: Settings.BackupSettings (singleton row)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BackupSettings' AND schema_id = SCHEMA_ID('Settings'))
BEGIN
    CREATE TABLE Settings.BackupSettings (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        IsEnabled BIT NOT NULL DEFAULT 0,
        FrequencyMinutes INT NOT NULL DEFAULT 1440
            CONSTRAINT CK_BackupSettings_Frequency CHECK (FrequencyMinutes BETWEEN 1440 AND 525600), -- 1 day .. 365 days (UI offers day/month presets, not minutes)
        BackupFolderPath NVARCHAR(500) NOT NULL,
        RetentionDays INT NOT NULL DEFAULT 30
            CONSTRAINT CK_BackupSettings_Retention CHECK (RetentionDays BETWEEN 0 AND 3650),
        LastBackupTime DATETIME NULL,
        NextBackupTime DATETIME NULL,
        LastStatus NVARCHAR(20) NOT NULL DEFAULT 'Disabled',
        LastErrorMessage NVARCHAR(1000) NULL,
        LastModifiedBy NVARCHAR(100) NULL,
        LastModifiedOn DATETIME NULL
    );
END
GO

-- Migration: widen the frequency range from the original 5-10080 minutes (minutes/hours UI)
-- to 1440-525600 minutes (1 day .. 365 days, day/month UI). Clamp any existing row into the
-- new range first so adding the constraint back can't fail on old data.
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_BackupSettings_Frequency' AND parent_object_id = OBJECT_ID('Settings.BackupSettings'))
BEGIN
    UPDATE Settings.BackupSettings SET FrequencyMinutes = 1440 WHERE FrequencyMinutes < 1440;
    UPDATE Settings.BackupSettings SET FrequencyMinutes = 525600 WHERE FrequencyMinutes > 525600;
    ALTER TABLE Settings.BackupSettings DROP CONSTRAINT CK_BackupSettings_Frequency;
    ALTER TABLE Settings.BackupSettings ADD CONSTRAINT CK_BackupSettings_Frequency CHECK (FrequencyMinutes BETWEEN 1440 AND 525600);
END
GO

-- =============================================
-- Table 2: Settings.BackupHistory
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BackupHistory' AND schema_id = SCHEMA_ID('Settings'))
BEGIN
    CREATE TABLE Settings.BackupHistory (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        BackupFileName NVARCHAR(300) NOT NULL,
        BackupFilePath NVARCHAR(500) NOT NULL,
        BackupStartTime DATETIME NOT NULL,
        BackupEndTime DATETIME NULL,
        FileSizeBytes BIGINT NULL,
        Status NVARCHAR(20) NOT NULL,          -- Running | Success | Failed | Deleted
        ErrorMessage NVARCHAR(1000) NULL,
        TriggerType NVARCHAR(30) NOT NULL,     -- Scheduled | Manual | PreRestore | ScheduledTask | Auto
        CreatedBy NVARCHAR(100) NULL
    );

    CREATE NONCLUSTERED INDEX IX_BackupHistory_StartTime
        ON Settings.BackupHistory (BackupStartTime DESC, ID DESC)
        INCLUDE (Status, BackupFileName);

    CREATE NONCLUSTERED INDEX IX_BackupHistory_Status
        ON Settings.BackupHistory (Status);
END
GO

-- =============================================
-- Table 3: Settings.BackupAuditLog
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BackupAuditLog' AND schema_id = SCHEMA_ID('Settings'))
BEGIN
    CREATE TABLE Settings.BackupAuditLog (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        ActivityType NVARCHAR(50) NOT NULL,    -- SettingsChange | FolderTest | Backup | Restore | Delete | Download | Cleanup
        Status NVARCHAR(20) NOT NULL,          -- Success | Failed | Warning
        Details NVARCHAR(1000) NULL,
        PerformedBy NVARCHAR(100) NULL,
        PerformedOn DATETIME NOT NULL DEFAULT GETDATE()
    );

    CREATE NONCLUSTERED INDEX IX_BackupAuditLog_PerformedOn
        ON Settings.BackupAuditLog (PerformedOn DESC);
END
GO

-- =============================================
-- Permission seeds (Admin bypasses permission
-- checks automatically; these rows let Admin
-- delegate access to other roles). Keys match
-- [AuthorizePermission] attributes in
-- Controllers\BackupSettingsController.cs exactly.
-- =============================================
IF OBJECT_ID('Auth.Permissions', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'BackupSettings_View')
        INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module)
        VALUES ('BackupSettings_View', 'View Database Backup Settings', 'Database Backup');

    IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'BackupSettings_Manage')
        INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module)
        VALUES ('BackupSettings_Manage', 'Manage Database Backups', 'Database Backup');

    IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'BackupSettings_Restore')
        INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module)
        VALUES ('BackupSettings_Restore', 'Restore Database From Backup', 'Database Backup');
END
GO

PRINT 'Database Backup tables and permissions created successfully!';
GO
