using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Patient
{
    public class DataTableValuesDTO
    {
        public string SearchString { get; set; } = string.Empty;
        public int Offset { get; set; }
        public int PageSize { get; set; }
    }
}