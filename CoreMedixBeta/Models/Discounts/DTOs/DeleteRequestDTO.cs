using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Discounts.DTOs
{
    public class DeleteRequestDTO
    {
        [Required]
        public int ID { get; set; }

        public string DeletedBy { get; set; }
    }
}