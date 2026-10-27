using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Purchases
{
    [Schema("Purchase"), Table("PurchaseItems")]
    public class PurchaseItem
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }

        [DontUpdate]
        public int ItemID { get; set; }

        [DontUpdate]
        public int VariationID { get; set; }

        [DontUpdate]
        public int PurchaseID { get; set; }

        public string Name { get; set; }
        public int Qty { get; set; }
        public int BonusQty { get; set; }
        public decimal ItemTax { get; set; }
        public decimal UnitCost { get; set; }
        public decimal UnitCostWithTax { get; set; }
        public decimal LineTotal { get; set; }
    }
}