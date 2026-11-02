using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.InventoryItems.DTOs
{
    public class GetAllAttributesOutputDto
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}