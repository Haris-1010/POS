using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class SearchItemsInputDto
    {
        public string SearchString { get; set; }
        public string SearchType { get; set; }
        public int ManufacturerID { get; set; }
        public int SupplierID { get; set; }
        public bool NearToFinish { get; set; }
        public bool NearToExpire { get; set; }
        public bool Finished { get; set; }
        public bool Expired { get; set; } 
    }
}