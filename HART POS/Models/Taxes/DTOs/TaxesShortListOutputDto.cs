using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.Taxes.DTOs
{
    public class TaxesShortListOutputDto
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public decimal Percentage { get; set; }
        public string PaymentMode { get; set; }
    }
}