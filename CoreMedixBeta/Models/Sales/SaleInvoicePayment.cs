using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales
{
    [Schema("Sale")]
    [Table("SaleInvoicePayments")]
    public class SaleInvoicePayment
    {
        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public int InvoiceID { get; set; }
        public DateTime Date { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentMode { get; set; }
        public string AttachmentURL { get; set; }
        public string TransactionType { get; set; } = "Payment";
    }
}