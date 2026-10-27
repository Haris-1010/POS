using CoreMedixBeta.DbModels;
using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales
{
    [Schema("Sale")]
    [Table("SaleInvoices")]
    public class SaleInvoice
    {
        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; } = ServerConfig.DateTimeNow;
        public string Remarks { get; set; }
        public string CustomerID { get; set; }
        public string CustomerType { get; set; }

        [DontInsert]
        [DontUpdate]
        public string SearchCustomer { get; set; }
        public string SalePersonUsername { get; set; }
        public string ReferredByID { get; set; }
        public string ReferredByType { get; set; }

        [DontUpdate]
        public int ReferralOrderID { get; set; }

        [DontUpdate]
        public string CreatedBy { get; set; }

        [DontUpdate]
        public DateTime CreatedOn { get; set; }

        [DontInsert]
        public string LastModifiedBy { get; set; }

        [DontInsert]
        public DateTime? LastModifiedOn { get; set; }

        public string DeletedBy { get; set; }

        [DontInsert]
        public DateTime? DeletedOn { get; set; }

        public bool? IsDeleted { get; set; } = false;
    }
}