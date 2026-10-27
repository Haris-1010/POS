using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems
{
    [Schema("Inventory")]
    [Table("ItemAttributes")]
    public class ItemAttribute
    {
        public const int MinTitleLength = 2;
        public const int MaxTitleLength = 30;

        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public string Title { get; set; }
        public string AttributeType { get; set; }

        [DontUpdate]
        public DateTime? TimeStamp { get; set; }
    }
}