using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales.DTOs
{
    public class DeleteSaleInputDto
    {
        public int ID { get; set; }
        public string DeletedBy { get; set; }
    }
}