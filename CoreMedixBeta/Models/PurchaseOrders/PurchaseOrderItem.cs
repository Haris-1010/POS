using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.PurchaseOrders
{
    [Schema("Purchase"), Table("PurchaseOrderItems")]
    public class PurchaseOrderItem
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }

        [DontUpdate]
        public int ItemID { get; set; }

        [DontUpdate]
        public int VariationID { get; set; }

        [DontUpdate]
        public int PurchaseOrderID { get; set; }

        public string Name { get; set; }
        public int Qty { get; set; }
        public double UnitCost { get; set; }
        public double UnitCostWithTax { get; set; }
        public double LineTotal { get; set; }
    }
}