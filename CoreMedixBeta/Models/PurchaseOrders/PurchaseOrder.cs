using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.PurchaseOrders
{
    [Schema("Purchase"), Table("PurchaseOrders")]
    public class PurchaseOrder
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }

        public int SupplierID { get; set; }
        public string ReferenceNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public string BusinessLocation { get; set; }
        public string AttachmentUrl { get; set; }
        public decimal SubTotal { get; set; }

        [DontUpdate]
        public string CreatedBy { get; set; }

        [DontUpdate]
        public DateTime CreatedOn { get; set; }

        [DontInsert]
        public string LastModifiedBy { get; set; }

        [DontInsert]
        public DateTime? LastModifiedOn { get; set; }

        [DontInsert, DontUpdate]
        public string DeletedBy { get; set; }

        [DontInsert, DontUpdate]
        public DateTime? DeletedOn { get; set; }

        [DontUpdate]
        public bool? IsDeleted { get; set; } = false;

        [DontUpdate]
        public bool? IsPurchaseReceived { get; set; } = false;
    }
}