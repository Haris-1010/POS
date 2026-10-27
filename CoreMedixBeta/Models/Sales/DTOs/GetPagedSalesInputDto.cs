using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales.DTOs
{
    public class GetPagedSalesInputDto : PagedRequestInputDto
    {
        public string PaymentMode { get; set; }
        public string CustomerID { get; set; }
        public decimal TotalAmount { get; set; }
    }
}