using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.ReferralOrders.DTOs
{
    public class GetPagedOrdersInputDto : PagedRequestInputDto
    {
        public string Status { get; set; }
        public string ReferredByID { get; set; }
        public string ReferredByType { get; set; }
        public string GroupByColumn { get; set; }
    }
}