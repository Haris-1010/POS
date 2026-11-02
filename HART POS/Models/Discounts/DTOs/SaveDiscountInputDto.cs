using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.Discounts.DTOs
{
    public class SaveDiscountInputDto
    {
        public int ID { get; set; }

        [Required]
        public string DiscountName { get; set; }

        [Required]
        public string DiscountType { get; set; }

        [Required]
        public decimal DiscountValue { get; set; }

        public bool IsActive { get; set; }
    }
}