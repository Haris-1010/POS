using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales.DTOs
{
    public class GetSalesInfoOutputDto
    {
        public int TotalInvoices { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
    }
}