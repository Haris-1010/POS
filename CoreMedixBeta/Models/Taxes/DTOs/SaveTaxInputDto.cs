using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Taxes.DTOs
{
    public class SaveTaxInputDto
    {
        public int ID { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public decimal Percentage { get; set; }

        [Required]
        public string PaymentMode { get; set; }

        [Required]
        public string LocationID { get; set; }

        [Required]
        public string MasterUsername { get; set; }

        public string LastModifiedBy { get; set; }
        public string LastModifiedOn { get; set; }
    }
}