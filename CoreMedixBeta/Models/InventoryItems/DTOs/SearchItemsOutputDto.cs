using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class SearchItemsOutputDto
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public int VariationID { get; set; }
        public string VariationName { get; set; }
        public string UniqueID { get; set; }
        public string ExpiryStatus { get; set; }
        public int Quantity { get; set; }
        public int SKU { get; set; }
        public int FormationID { get; set; }
        public int UnitID { get; set; }
        public int CategoryID { get; set; }
        public int SubCategoryID { get; set; }
        public string UPC { get; set; }
        public int OpeningStock { get; set; }
        public int ReorderPoint { get; set; }
        public decimal? OpeningStockRate { get; set; }
        public string Batch { get; set; } 
        public string Formation { get; set; }
        public decimal? Weight { get; set; }
        public int WeightUnitID { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal? Tax { get; set; }
        public bool IsReturnable { get; set; }
        public string Description { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? Margin { get; set; }
        public decimal? MRP { get; set; }
    }
}