using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Purchases
{
    [Schema("Purchase")]
    [Table("PurchasePayments")]
    public class PurchasePayment
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }

        public DateTime Date { get; set; }
        public double PaidAmount { get; set; }
        public string PaymentMode { get; set; }

        [DontUpdate]
        public int PurchaseID { get; set; }

        public string AttachmentURL { get; set; }
    }
}