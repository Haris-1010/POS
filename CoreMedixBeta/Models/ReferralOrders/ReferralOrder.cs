using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.ReferralOrders
{
    [Schema("ReferralOrder"), Table("ReferralOrders")]
    public class ReferralOrder
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }

        [DontUpdate]
        public string PatientUsername { get; set; }

        [DontUpdate]
        public int PrescriptionID { get; set; }

        [DontUpdate]
        public int PrescriptionStyleID { get; set; }

        [DontUpdate]
        public string ReferredByID { get; set; }

        [DontUpdate]
        public string ReferredByType { get; set; }

        [DontUpdate]
        public string Source { get; set; }

        [DontUpdate]
        public string OrderBy { get; set; }

        [DontUpdate]
        public DateTime OrderOn { get; set; }

        [DontInsert, DontUpdate]
        public bool IsConversionStarted { get; set; }

        [DontInsert, DontUpdate]
        public DateTime? ConversionStartedOn { get; set; }

        [DontInsert, DontUpdate]
        public string ConversionStartedBy { get; set; }

        [DontInsert, DontUpdate]
        public bool IsConvertedToInvoice { get; set; }

        [DontInsert, DontUpdate]
        public DateTime? ConvertedToInvoiceOn { get; set; }

        [DontInsert, DontUpdate]
        public string ConvertedToInvoiceBy { get; set; }

        [DontInsert, DontUpdate]
        public int? InvoiceID { get; set; }

        [DontInsert, DontUpdate]
        public bool IsOrderCancelled { get; set; }

        [DontInsert, DontUpdate]
        public DateTime? OrderCancelledOn { get; set; }

        [DontInsert, DontUpdate]
        public string OrderCancelledBy { get; set; }
    }
}