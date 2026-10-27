using CoreMedixBeta.Models.Brandings;
using CoreMedixBeta.Models.Brandings.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.DbModels
{
    public class BasePrintVM
    {
        public string CurrencySymbol { get; set; }
        public PrintBranding Branding { get; set; }
        public List<GetPrintBrandingCustomFieldOutputDto> PrintBrandingCustomFieldsList { get; set; }
    }
}