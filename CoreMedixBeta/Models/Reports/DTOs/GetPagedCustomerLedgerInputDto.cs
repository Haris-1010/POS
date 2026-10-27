using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedCustomerLedgerInputDto : PagedRequestInputDto
    {
        public string CustomerID { get; set; }
        public string CustomerType { get; set; }
    }
}