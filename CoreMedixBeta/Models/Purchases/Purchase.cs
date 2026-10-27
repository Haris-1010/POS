using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Purchases
{
    [Schema("Purchase"), Table("Purchases")]
    public class Purchase
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }

        [DontUpdate]
        public int PurchaseOrderID { get; set; }

        [DontUpdate]
        public DateTime PurchaseReceivedAt { get; set; }

        public double SubTotal { get; set; }
        public string DiscountType { get; set; }
        public double DiscountAmount { get; set; }
        public double SubTotalAfterDiscount { get; set; }
        public int TaxID { get; set; }
        public double SubtotalAfterTax { get; set; }
        public double PaidAmount { get; set; }
        public double RemainingAmount { get; set; }
        public string ShippingDetails { get; set; }
        public string AdditionalNotes { get; set; }

        [DontUpdate]
        public string CreatedBy { get; set; }

        [DontUpdate]
        public DateTime CreatedOn { get; set; }

        [DontInsert]
        public string LastModifiedBy { get; set; }

        [DontInsert]
        public DateTime? LastModifiedOn { get; set; }
    }
}