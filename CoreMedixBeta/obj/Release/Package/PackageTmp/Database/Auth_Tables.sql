-- =============================================
-- Authentication & Authorization Tables
-- Pharmacy Management System (CoreMedixBeta)
-- Run this script in SQL Server Management Studio
-- against the PharmacyDB database
-- =============================================

-- Create Auth schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Auth')
BEGIN
    EXEC('CREATE SCHEMA Auth');
END
GO

-- =============================================
-- Table 1: Auth.Users
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users' AND schema_id = SCHEMA_ID('Auth'))
BEGIN
    CREATE TABLE Auth.Users (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL,
        PasswordHash NVARCHAR(256) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NULL,
        Phone NVARCHAR(20) NULL,
        Role NVARCHAR(20) NOT NULL DEFAULT 'User',
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(50) NULL,
        LastModifiedOn DATETIME NULL,
        LastModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedOn DATETIME NULL,
        DeletedBy NVARCHAR(50) NULL,
        CONSTRAINT UQ_Users_Username UNIQUE (Username)
    );
END
GO

-- =============================================
-- Table 2: Auth.Permissions
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Permissions' AND schema_id = SCHEMA_ID('Auth'))
BEGIN
    CREATE TABLE Auth.Permissions (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        PermissionKey NVARCHAR(100) NOT NULL,
        PermissionName NVARCHAR(100) NOT NULL,
        Module NVARCHAR(50) NOT NULL,
        CONSTRAINT UQ_Permissions_Key UNIQUE (PermissionKey)
    );
END
GO

-- =============================================
-- Table 3: Auth.UserPermissions
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserPermissions' AND schema_id = SCHEMA_ID('Auth'))
BEGIN
    CREATE TABLE Auth.UserPermissions (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        UserID INT NOT NULL,
        PermissionID INT NOT NULL,
        CONSTRAINT FK_UserPermissions_User FOREIGN KEY (UserID) REFERENCES Auth.Users(ID) ON DELETE CASCADE,
        CONSTRAINT FK_UserPermissions_Permission FOREIGN KEY (PermissionID) REFERENCES Auth.Permissions(ID) ON DELETE CASCADE,
        CONSTRAINT UQ_UserPermission UNIQUE (UserID, PermissionID)
    );
END
GO

-- =============================================
-- Seed Data: Permissions (16 permissions)
-- =============================================
IF NOT EXISTS (SELECT * FROM Auth.Permissions)
BEGIN
    -- Patient Module
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES
    ('Patient_View', 'View Patients', 'Patient'),
    ('Patient_Add', 'Add Patient', 'Patient'),
    ('Patient_Edit', 'Edit Patient', 'Patient'),
    ('Patient_Delete', 'Delete Patient', 'Patient'),
    -- Inventory Module
    ('Inventory_View', 'View Inventory', 'Inventory'),
    ('Inventory_Add', 'Add Item', 'Inventory'),
    ('Inventory_Edit', 'Edit Item', 'Inventory'),
    ('Inventory_Delete', 'Delete Item', 'Inventory'),
    -- Sales Module
    ('Sales_View', 'View Sales', 'Sales'),
    ('Sales_Add', 'Create Sale', 'Sales'),
    ('Sales_Edit', 'Edit Sale', 'Sales'),
    ('Sales_Delete', 'Delete Sale', 'Sales'),
    -- Purchases Module
    ('Purchases_View', 'View Purchases', 'Purchases'),
    ('Purchases_Add', 'Add Purchase', 'Purchases'),
    -- Reports Module
    ('Reports_View', 'View Reports', 'Reports'),
    -- Settings Module
    ('Settings_Manage', 'Manage Settings', 'Settings');
END
GO

-- =============================================
-- Seed Data: Default Admin User
-- Password: admin123 (SHA256 hashed)
-- Hash of "admin123": 240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9
-- =============================================
IF NOT EXISTS (SELECT * FROM Auth.Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Auth.Users (Username, PasswordHash, FullName, Email, Role, IsActive, CreatedOn, CreatedBy)
    VALUES ('admin', '240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9', 'System Admin', 'admin@pharmacy.com', 'Admin', 1, GETDATE(), 'System');

    -- Assign ALL permissions to admin
    DECLARE @AdminID INT = SCOPE_IDENTITY();
    INSERT INTO Auth.UserPermissions (UserID, PermissionID)
    SELECT @AdminID, ID FROM Auth.Permissions;
END
GO

PRINT 'Auth tables created successfully!';
PRINT 'Default admin user: admin / admin123';
GO
