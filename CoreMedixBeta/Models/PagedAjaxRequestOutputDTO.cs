using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models
{
    public class PagedAjaxRequestOutputDTO
    {
        public string draw { get; set; } = "1";
        public string recordsFiltered { get; set; } = string.Empty;
        public string recordsTotal { get; set; } = string.Empty;
        public string data { get; set; } = "[]";
    }
}