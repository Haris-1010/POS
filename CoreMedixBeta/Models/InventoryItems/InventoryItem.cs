using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems
{
    [Schema("Inventory")]
    [Table("InventoryItems")]
    public class InventoryItem
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }

        public string Name { get; set; }
        public string ProductType { get; set; }
        public string MedicineSalt { get; set; }
        public int? BrandID { get; set; }
        public int ManufacturerID { get; set; }
        public string SupplierID { get; set; }

        [DontUpdate]
        public string CreatedBy { get; set; }

        [DontUpdate]
        public DateTime CreatedOn { get; set; }

        [DontInsert]
        public string LastModifiedBy { get; set; }

        [DontInsert]
        public DateTime? LastModifiedOn { get; set; }

        [DontUpdate, DontInsert]
        public string DeletedBy { get; set; }

        [DontUpdate, DontInsert]
        public DateTime? DeletedOn { get; set; }

        [DontUpdate]
        public bool? IsDeleted { get; set; } = false;
    }
}