using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class GetPagedItemsOutputDto
    {
        public string UniqueID { get; set; }
        public int ID { get; set; }
        public int VariationID { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public string ManufacturerName { get; set; }
        public string SupplierName { get; set; }
        public int TotalVariations { get; set; }
        public int NearToExpireVariations { get; set; }
        public int NearToFinishVariations { get; set; }
        public int NewArrivals { get; set; }
        public int VariationStockQty { get; set; }
        public int ManufacturerID { get; set; }
    }
}