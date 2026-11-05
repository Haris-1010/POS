using InstaCareEMR.Core.DBHelpers;
using System;

namespace HARTPOS.Service
{
    /// <summary>
    /// Small, safe database changes applied on startup, so every offline install picks
    /// them up without someone running a script by hand. Each step checks first, so
    /// running it again does nothing.
    /// </summary>
    public static class SchemaUpdates
    {
        public static void Run()
        {
            // Items: Manufacturer is optional (NULL = no manufacturer).
            Try(@"
IF COLUMNPROPERTY(OBJECT_ID('Inventory.InventoryItems'), 'ManufacturerID', 'AllowsNull') = 0
    ALTER TABLE Inventory.InventoryItems ALTER COLUMN ManufacturerID INT NULL;

-- The column has a foreign key to Manufacturer.Manufacturers, so no manufacturer must be NULL (never 0).
IF OBJECT_ID('Inventory.DF_InventoryItems_ManufacturerID') IS NOT NULL
    ALTER TABLE Inventory.InventoryItems DROP CONSTRAINT DF_InventoryItems_ManufacturerID;");

            // Referral Orders is not used: remove its permission (user assignments go with it).
            Try(@"
IF OBJECT_ID('Auth.Permissions') IS NOT NULL
BEGIN
    DELETE UP FROM Auth.UserPermissions UP
    INNER JOIN Auth.Permissions P ON P.ID = UP.PermissionID
    WHERE P.PermissionKey = 'ReferralOrders_View';
    DELETE FROM Auth.Permissions WHERE PermissionKey = 'ReferralOrders_View';
END");

            // Items: Type is optional.
            Try(@"
IF COLUMNPROPERTY(OBJECT_ID('Inventory.InventoryItems'), 'ProductType', 'AllowsNull') = 0
    ALTER TABLE Inventory.InventoryItems ALTER COLUMN ProductType NVARCHAR(100) NULL;");
        }

        private static void Try(string sql)
        {
            try
            {
                DBHelper.ExecuteQuery(sql);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[SCHEMA] Update failed: " + ex.Message);
            }
        }
    }
}
