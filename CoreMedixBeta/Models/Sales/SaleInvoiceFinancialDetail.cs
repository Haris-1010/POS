using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales
{
    [Schema("Sale")]
    [Table("SaleInvoiceFinancialDetails")]
    public class SaleInvoiceFinancialDetail
    {
        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public int InvoiceID { get; set; }
        public decimal SubTotal { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal SubtotalAfterDiscount { get; set; }
        public int? TaxID { get; set; }
        public decimal SubtotalAfterTax { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public string AdditionalNotes { get; set; }
    }
}