using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems
{
    [Schema("Inventory")]
    [Table("InventoryItemVariations")]
    public class InventoryItemVariation
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }
        [DontUpdate]
        public int ItemID { get; set; } 
        public string VariationName { get; set; }
        public int SKU { get; set; }
        public int FormationID { get; set; }
        public int UnitID { get; set; }
        public int CategoryID { get; set; }
        public int SubCategoryID { get; set; }
        public string UPC { get; set; }
        public int OpeningStock { get; set; }
        public int ReorderPoint { get; set; }
        public decimal? OpeningStockRate { get; set; }
        public int NoOfPillsInPack { get; set; } = 1;
        public string Batch { get; set; } 
        public decimal? Weight { get; set; }
        public int WeightUnitID { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal? Tax { get; set; }
        public bool IsReturnable { get; set; }
        public string Description { get; set; }
        public string AlternateItems { get; set; }

        [DontUpdate]
        public string UniqueID { get; set; }

        public decimal SellingPrice { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? Margin { get; set; }
        public decimal? MRP { get; set; }

        [DontUpdate]
        public string CreatedBy { get; set; }

        [DontUpdate]
        public DateTime CreatedOn { get; set; }

        [DontInsert]
        public string LastModifiedBy { get; set; }

        [DontInsert]
        public DateTime? LastModifiedOn { get; set; }
    }
}