using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Taxes.DTOs
{
    public class GetPagedTaxesOutputDto
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public double Percentage { get; set; }
        public string PaymentMode { get; set; }
    }
}