using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Brandings.DTOs
{
    public class SavePrintBrandingInputDto
    {
        public int ID { get; set; }

        [Required]
        public string BusinessName { get; set; }

        [Required]
        public string BusinessPhone { get; set; }

        [DataType(DataType.EmailAddress)]
        public string BusinessEmail { get; set; }

        public string Logo { get; set; }
        public string HeaderImg { get; set; }
        public string HeaderText { get; set; }
        public string FooterImg { get; set; }
        public string FooterText { get; set; }

        [Required]
        public string LocationID { get; set; }

        [Required]
        public string MasterUsername { get; set; }

        public string LastModifiedBy { get; set; }
        public List<PrintBrandingImagesDto> BrandingImages { get; set; }
    }

    public class PrintBrandingImagesDto
    {
        public string ImageName { get; set; }
        public string ImageURL { get; set; }
        public PrintBrandingImagesType Type { get; set; }
    }

    public enum PrintBrandingImagesType
    {
        FOOTER,
        HEADER,
        LOGO
    }
}