using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales
{
    [Schema("Sale")]
    [Table("SaleInvoiceItems")]
    public class SaleInvoiceItem
    {
        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public string Name { get; set; }
        public int InvoiceID { get; set; }
        public int ItemID { get; set; }
        public int VariationID { get; set; }
        public int Qty { get; set; }
        public decimal UnitCost { get; set; }
        public string DiscountType { get; set; }
        public decimal Discount { get; set; }
        public int DiscountID { get; set; }
        public decimal ItemTax { get; set; }
        public decimal UnitCostBeforeTax { get; set; }
        public decimal UnitSalePriceInclusiveTax { get; set; }
        public decimal LineTotal { get; set; }
        public string TransactionStatus { get; set; } = "INSERTED";
        public int RetQty { get; set; }
    }
}