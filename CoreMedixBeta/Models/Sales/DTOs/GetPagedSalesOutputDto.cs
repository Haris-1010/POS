using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales.DTOs
{
    public class GetPagedSalesOutputDto
    {
        public int ID { get; set; }
        public string InvoiceNo { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string InvoiceDate { get; set; }
        public string SalePersonName { get; set; }
        public string DoctorName { get; set; }
        public decimal NetTotal { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}