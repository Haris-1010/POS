using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class GetPagedInventoryItemsInputDto : AjaxRequestInputDTO
    {
        public int SKU { get; set; }
        public string UPC { get; set; }
        public string UniqueID { get; set; }
        public string ProductType { get; set; }
        public int CategoryID { get; set; }
        public int ManufacturerID { get; set; }
        public int SupplierID { get; set; }
        public int BrandID { get; set; }
        public string FilterType { get; set; }
    }
}