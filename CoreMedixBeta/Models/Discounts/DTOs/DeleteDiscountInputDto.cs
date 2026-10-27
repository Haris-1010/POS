using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Discounts.DTOs
{
    public class DeleteDiscountInputDto
    {
        [Required]
        public int ID { get; set; }
    }
}