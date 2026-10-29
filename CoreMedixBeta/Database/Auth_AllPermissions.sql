-- ============================================
-- ADD ALL MISSING PERMISSIONS
-- Run this script in SQL Server Management Studio
-- ============================================

USE [PharmacyDB]
GO

-- Patient (already exist, skip if already there)
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Patient_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Patient_View', 'View Patients', 'Patient')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Patient_Add')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Patient_Add', 'Add Patient', 'Patient')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Patient_Edit')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Patient_Edit', 'Edit Patient', 'Patient')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Patient_Delete')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Patient_Delete', 'Delete Patient', 'Patient')

-- Inventory (already exist)
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Inventory_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Inventory_View', 'View Inventory', 'Inventory')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Inventory_Add')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Inventory_Add', 'Add Item', 'Inventory')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Inventory_Edit')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Inventory_Edit', 'Edit Item', 'Inventory')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Inventory_Delete')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Inventory_Delete', 'Delete Item', 'Inventory')

-- Sales (already exist)
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Sales_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Sales_View', 'View Sales', 'Sales')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Sales_Add')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Sales_Add', 'Create Sale', 'Sales')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Sales_Edit')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Sales_Edit', 'Edit Sale', 'Sales')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Sales_Delete')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Sales_Delete', 'Delete Sale', 'Sales')

-- Purchases (existing: Purchases_View, Purchases_Add)
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Purchases_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Purchases_View', 'View Purchases', 'Purchases')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Purchases_Add')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Purchases_Add', 'Add Purchase', 'Purchases')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Purchases_Edit')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Purchases_Edit', 'Edit Purchase', 'Purchases')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Purchases_Delete')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Purchases_Delete', 'Delete Purchase', 'Purchases')

-- Reports (already exist)
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Reports_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Reports_View', 'View Reports', 'Reports')

-- Settings (already exist)
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Settings_Manage')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Settings_Manage', 'Manage Settings', 'Settings')

-- ============================================
-- NEW MODULES BELOW
-- ============================================

-- Brands
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Brands_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Brands_View', 'View Brands', 'Brands')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Brands_Add')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Brands_Add', 'Add Brand', 'Brands')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Brands_Edit')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Brands_Edit', 'Edit Brand', 'Brands')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Brands_Delete')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Brands_Delete', 'Delete Brand', 'Brands')

-- Manufacturers
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Manufacturers_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Manufacturers_View', 'View Manufacturers', 'Manufacturers')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Manufacturers_Add')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Manufacturers_Add', 'Add Manufacturer', 'Manufacturers')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Manufacturers_Edit')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Manufacturers_Edit', 'Edit Manufacturer', 'Manufacturers')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Manufacturers_Delete')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Manufacturers_Delete', 'Delete Manufacturer', 'Manufacturers')

-- Suppliers
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Suppliers_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Suppliers_View', 'View Suppliers', 'Suppliers')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Suppliers_Add')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Suppliers_Add', 'Add Supplier', 'Suppliers')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Suppliers_Edit')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Suppliers_Edit', 'Edit Supplier', 'Suppliers')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Suppliers_Delete')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Suppliers_Delete', 'Delete Supplier', 'Suppliers')

-- Discounts
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Discounts_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Discounts_View', 'View Discounts', 'Discounts')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Discounts_Add')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Discounts_Add', 'Add Discount', 'Discounts')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Discounts_Edit')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Discounts_Edit', 'Edit Discount', 'Discounts')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Discounts_Delete')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Discounts_Delete', 'Delete Discount', 'Discounts')

-- Taxes
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Taxes_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Taxes_View', 'View Taxes', 'Taxes')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Taxes_Add')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Taxes_Add', 'Add Tax', 'Taxes')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Taxes_Edit')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Taxes_Edit', 'Edit Tax', 'Taxes')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Taxes_Delete')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Taxes_Delete', 'Delete Tax', 'Taxes')

-- Inventory Settings
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'InventorySettings_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('InventorySettings_View', 'View Inventory Settings', 'Inventory Settings')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'InventorySettings_Manage')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('InventorySettings_Manage', 'Manage Inventory Settings', 'Inventory Settings')

-- Referral Orders
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'ReferralOrders_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('ReferralOrders_View', 'View Referral Orders', 'Referral Orders')

-- Branding
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Branding_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Branding_View', 'View Branding', 'Branding')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Branding_Manage')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Branding_Manage', 'Manage Branding', 'Branding')

-- Database Backup (keys match [AuthorizePermission] in BackupSettingsController.cs)
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'BackupSettings_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('BackupSettings_View', 'View Database Backup Settings', 'Database Backup')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'BackupSettings_Manage')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('BackupSettings_Manage', 'Manage Database Backups', 'Database Backup')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'BackupSettings_Restore')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('BackupSettings_Restore', 'Restore Database From Backup', 'Database Backup')

-- User Management (Admin assigns to users)
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Users_View')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Users_View', 'View Users', 'User Management')
IF NOT EXISTS (SELECT 1 FROM Auth.Permissions WHERE PermissionKey = 'Users_Manage')
    INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module) VALUES ('Users_Manage', 'Manage Users', 'User Management')

GO

PRINT 'All permissions added successfully!'
PRINT 'Total permissions: '
SELECT COUNT(*) AS TotalPermissions FROM Auth.Permissions
GO
