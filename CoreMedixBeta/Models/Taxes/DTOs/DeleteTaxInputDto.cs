using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Taxes.DTOs
{
    public class DeleteTaxInputDto
    {
        [Required]
        public int ID { get; set; }
    }
}