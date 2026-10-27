using InstaCareEMR.Core.DBHelpers;

namespace CoreMedixBeta.Models
{
    [Schema("Manufacturer")]
    [Table("ManufacturerBrands")]
    public class ManufacturerBrands
    {
        [DontInsert, DontUpdate]
        public int? ID { get; set; }

        public string Title { get; set; }

        [DontUpdate]
        public string ManufacturerID { get; set; }
    }
}