-- =============================================
-- Automatic Database Backup & Restore Tables
-- Pharmacy Management System (CoreMedixBeta)
-- Run this script in SQL Server Management Studio
-- against the POS database (see Web.config connection string)
-- Safe to run multiple times (idempotent).
-- =============================================

-- =============================================
-- Table 1: dbo.BackupSettings (singleton row)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BackupSettings' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.BackupSettings (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        IsEnabled BIT NOT NULL DEFAULT 0,
        IntervalMinutes INT NOT NULL DEFAULT 60
            CONSTRAINT CK_BackupSettings_Interval CHECK (IntervalMinutes BETWEEN 1 AND 10080),
        BackupFolder NVARCHAR(400) NOT NULL,
        RetentionDays INT NOT NULL DEFAULT 30
            CONSTRAINT CK_BackupSettings_Retention CHECK (RetentionDays BETWEEN 0 AND 3650),
        LastSuccessTime DATETIME NULL,
        LastAttemptTime DATETIME NULL,
        LastStatus NVARCHAR(20) NOT NULL DEFAULT 'Never Run',
        LastError NVARCHAR(1000) NULL,
        LastBackupFile NVARCHAR(300) NULL,
        UpdatedBy NVARCHAR(100) NULL,
        UpdatedOn DATETIME NULL
    );
END
GO

-- Forward compatibility: add any missing columns on pre-existing installs
IF COL_LENGTH('dbo.BackupSettings', 'LastAttemptTime') IS NULL
    ALTER TABLE dbo.BackupSettings ADD LastAttemptTime DATETIME NULL;
IF COL_LENGTH('dbo.BackupSettings', 'LastStatus') IS NULL
    ALTER TABLE dbo.BackupSettings ADD LastStatus NVARCHAR(20) NOT NULL DEFAULT 'Never Run';
IF COL_LENGTH('dbo.BackupSettings', 'LastError') IS NULL
    ALTER TABLE dbo.BackupSettings ADD LastError NVARCHAR(1000) NULL;
IF COL_LENGTH('dbo.BackupSettings', 'LastBackupFile') IS NULL
    ALTER TABLE dbo.BackupSettings ADD LastBackupFile NVARCHAR(300) NULL;
IF COL_LENGTH('dbo.BackupSettings', 'UpdatedBy') IS NULL
    ALTER TABLE dbo.BackupSettings ADD UpdatedBy NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.BackupSettings', 'UpdatedOn') IS NULL
    ALTER TABLE dbo.BackupSettings ADD UpdatedOn DATETIME NULL;
GO

-- =============================================
-- Table 2: dbo.BackupHistory
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BackupHistory' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.BackupHistory (
        ID BIGINT IDENTITY(1,1) PRIMARY KEY,
        FileName NVARCHAR(300) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        DatabaseName NVARCHAR(200) NOT NULL,
        BackupType NVARCHAR(20) NOT NULL,     -- Scheduled | Manual | PreRestore
        Status NVARCHAR(20) NOT NULL,         -- Running | Success | Failed
        FileSizeBytes BIGINT NULL,
        StartedOn DATETIME NOT NULL,
        CompletedOn DATETIME NULL,
        ErrorMessage NVARCHAR(1000) NULL,
        CreatedBy NVARCHAR(100) NOT NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedOn DATETIME NULL,
        DeletedReason NVARCHAR(50) NULL,      -- Manual | Retention
        RestoredOn DATETIME NULL,
        RestoredBy NVARCHAR(100) NULL
    );

    CREATE NONCLUSTERED INDEX IX_BackupHistory_StartedOn
        ON dbo.BackupHistory (StartedOn DESC, ID DESC)
        INCLUDE (Status, FileName, IsDeleted);

    CREATE NONCLUSTERED INDEX IX_BackupHistory_Status
        ON dbo.BackupHistory (Status, IsDeleted);
END
GO

IF COL_LENGTH('dbo.BackupHistory', 'RestoredOn') IS NULL
    ALTER TABLE dbo.BackupHistory ADD RestoredOn DATETIME NULL;
IF COL_LENGTH('dbo.BackupHistory', 'RestoredBy') IS NULL
    ALTER TABLE dbo.BackupHistory ADD RestoredBy NVARCHAR(100) NULL;
GO

-- =============================================
-- Permission seeds (Admin bypasses permission
-- checks automatically; these rows let Admin
-- delegate access to other roles)
-- =============================================
IF OBJECT_ID('Auth.Permissions', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'DatabaseBackup_View')
        INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module)
        VALUES ('DatabaseBackup_View', 'View Database Backup Settings', 'Database Backup');

    IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'DatabaseBackup_Manage')
        INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module)
        VALUES ('DatabaseBackup_Manage', 'Manage Database Backups', 'Database Backup');
END
GO

PRINT 'Database Backup tables and permissions created successfully!';
GO
