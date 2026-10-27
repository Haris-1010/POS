using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems
{
    [Schema("Inventory")]
    [Table("InventoryItemVariationImages")]
    public class InventoryItemVariationImage
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }

        [DontUpdate]
        public int VariationID { get; set; }

        public string ImageName { get; set; }
        public string ImageURL { get; set; }

        [DontInsert, DontUpdate]
        public DateTime TimeStamp { get; set; }
    }
}