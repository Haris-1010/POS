using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.ReferralOrders.DTOs
{
    public class GetPagedOrdersOutputDto
    {
        public int ID { get; set; }
        public string PatientUsername { get; set; }
        public string PatientName { get; set; }
        public string PatientPhone { get; set; }
        public string MR_No { get; set; }
        public int PrescriptionID { get; set; }
        public string ReferredByName { get; set; }
        public string ReferredByID { get; set; }
        public string ReferredByType { get; set; }
        public string Source { get; set; }
        public string OrderByName { get; set; }
        public string OrderOn { get; set; }
        public bool IsConversionStarted { get; set; }
        public string ConversionStartedOn { get; set; }
        public string ConversionStartedByName { get; set; }
        public bool IsConvertedToInvoice { get; set; }
        public string ConvertedToInvoiceOn { get; set; }
        public string ConvertedToInvoiceByName { get; set; }
        public string InvoiceNo { get; set; }
        public bool IsOrderCancelled { get; set; }
        public string OrderCancelledOn { get; set; }
        public string OrderCancelledByName { get; set; }
    }
}