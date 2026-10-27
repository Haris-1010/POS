using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class ManageItemInputDto : PagedRequestInputDto
    {
        public int ItemID { get; set; } = 0;
        public int ManufacturerID { get; set; } = 0;
    }
}