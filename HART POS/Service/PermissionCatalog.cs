using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HARTPOS.Service
{
    /// <summary>
    /// Every permission the code checks with [AuthorizePermission] / HasPermission.
    ///
    /// On startup any that are missing from Auth.Permissions are added, so a fresh or
    /// older offline install doesn't depend on someone running Database/Auth_AllPermissions.sql.
    /// The order here is the order the Manage User page shows them in.
    /// </summary>
    public static class PermissionCatalog
    {
        public sealed class Entry
        {
            public string Key { get; }
            public string Name { get; }
            public string Module { get; }

            public Entry(string key, string name, string module)
            {
                Key = key;
                Name = name;
                Module = module;
            }
        }

        public static readonly IReadOnlyList<Entry> All = new List<Entry>
        {
            new Entry("Dashboard_View", "View Dashboard Figures", "Dashboard"),
            new Entry("Chatbot_Use", "Use POS Assistant (Chatbot)", "POS Assistant"),

            new Entry("Sales_View", "View Sales", "Sales"),
            new Entry("Sales_Add", "Create Sale", "Sales"),
            new Entry("Sales_Edit", "Edit / Return Sale", "Sales"),
            new Entry("Sales_Delete", "Delete Sale", "Sales"),

            new Entry("Patient_View", "View Customers", "Patient"),
            new Entry("Patient_Add", "Add Customer", "Patient"),
            new Entry("Patient_Edit", "Edit Customer", "Patient"),
            new Entry("Patient_Delete", "Delete Customer", "Patient"),

            new Entry("Inventory_View", "View Items", "Inventory"),
            new Entry("Inventory_Add", "Add / Import Items", "Inventory"),
            new Entry("Inventory_Edit", "Edit Item", "Inventory"),
            new Entry("Inventory_Delete", "Delete Item", "Inventory"),

            new Entry("Purchases_View", "View Purchases", "Purchases"),
            new Entry("Purchases_Add", "Add Purchase", "Purchases"),
            new Entry("Purchases_Edit", "Edit / Receive Purchase", "Purchases"),
            new Entry("Purchases_Delete", "Delete Purchase", "Purchases"),

            new Entry("Suppliers_View", "View Suppliers", "Suppliers"),
            new Entry("Suppliers_Add", "Add Supplier", "Suppliers"),
            new Entry("Suppliers_Edit", "Edit Supplier", "Suppliers"),
            new Entry("Suppliers_Delete", "Delete Supplier", "Suppliers"),

            new Entry("Manufacturers_View", "View Manufacturers", "Manufacturers"),
            new Entry("Manufacturers_Add", "Add Manufacturer", "Manufacturers"),
            new Entry("Manufacturers_Edit", "Edit Manufacturer", "Manufacturers"),
            new Entry("Manufacturers_Delete", "Delete Manufacturer", "Manufacturers"),

            new Entry("Brands_View", "View Brands", "Brands"),
            new Entry("Brands_Add", "Add Brand", "Brands"),
            new Entry("Brands_Edit", "Edit Brand", "Brands"),
            new Entry("Brands_Delete", "Delete Brand", "Brands"),

            new Entry("Reports_View", "View Reports", "Reports"),

            new Entry("Discounts_View", "View Discounts", "Discounts"),
            new Entry("Discounts_Add", "Add Discount", "Discounts"),
            new Entry("Discounts_Edit", "Edit Discount", "Discounts"),
            new Entry("Discounts_Delete", "Delete Discount", "Discounts"),

            new Entry("Taxes_View", "View Taxes", "Taxes"),
            new Entry("Taxes_Add", "Add Tax", "Taxes"),
            new Entry("Taxes_Edit", "Edit Tax", "Taxes"),
            new Entry("Taxes_Delete", "Delete Tax", "Taxes"),

            new Entry("InventorySettings_View", "View Settings", "Inventory Settings"),
            new Entry("InventorySettings_Manage", "Change Settings", "Inventory Settings"),

            new Entry("Branding_View", "View Branding", "Branding"),
            new Entry("Branding_Manage", "Change Branding", "Branding"),

            new Entry("BackupSettings_View", "View Backups", "Database Backup"),
            new Entry("BackupSettings_Manage", "Run / Manage Backups", "Database Backup"),
            new Entry("BackupSettings_Restore", "Restore Database", "Database Backup"),

            new Entry("Users_View", "View Users", "User Management"),
            new Entry("Users_Manage", "Add / Edit / Delete Users", "User Management"),
        };

        private static readonly Dictionary<string, int> OrderByKey =
            All.Select((e, i) => new { e.Key, i }).ToDictionary(x => x.Key, x => x.i, StringComparer.OrdinalIgnoreCase);

        public static bool IsKnown(string key) => key != null && OrderByKey.ContainsKey(key);

        /// <summary>Position for sorting; unknown keys go last.</summary>
        public static int OrderOf(string key) => key != null && OrderByKey.TryGetValue(key, out int i) ? i : int.MaxValue;

        private static volatile bool _seeded;
        private static readonly object SeedLock = new object();

        /// <summary>
        /// Adds missing permissions (never changes or removes existing rows). Safe to call
        /// often: after the first success it does nothing. Never throws.
        /// </summary>
        public static void EnsureSeeded()
        {
            if (_seeded)
                return;

            lock (SeedLock)
            {
                if (_seeded)
                    return;

                try
                {
                    string Sql(string v) => "N'" + v.Replace("'", "''") + "'";
                    string rows = string.Join(",\n", All.Select(e => $"({Sql(e.Key)}, {Sql(e.Name)}, {Sql(e.Module)})"));

                    string query = $@"
IF OBJECT_ID('Auth.Permissions') IS NOT NULL
INSERT INTO Auth.Permissions (PermissionKey, PermissionName, Module)
SELECT v.K, v.N, v.M
FROM (VALUES
{rows}
) AS v(K, N, M)
WHERE NOT EXISTS (SELECT 1 FROM Auth.Permissions p WHERE p.PermissionKey = v.K);";

                    DBHelper.ExecuteQuery(query);
                    _seeded = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("[PERMISSIONS] Seeding failed: " + ex.Message);
                }
            }
        }
    }
}
