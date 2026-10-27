using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class GetPagedItemVariationsOutputDto
    {
        public int ID { get; set; }
        public int ItemID { get; set; }
        public string VariationName { get; set; }
        public int SKU { get; set; }
        public string Formation { get; set; }
        public string Unit { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string UPC { get; set; }
        public int OpeningStock { get; set; }
        public int NoOfPillsInPack { get; set; }
        public int ReorderPoint { get; set; }
        public decimal? OpeningStockRate { get; set; }
        public string Batch { get; set; } 
        public decimal? Weight { get; set; }
        public string WeightUnit { get; set; }
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