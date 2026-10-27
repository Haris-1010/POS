using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Branding
{
    public class BrandingShortVM
    {
        public string logo { get; set; }
        public string logo_text { get; set; }
        public string phone { get; set; }
        public string address { get; set; }
        public string footer_text { get; set; }
        public string show_graphics { get; set; }
        public string Type { get; set; }
        public string DoctorUsername { get; set; }

        public static List<BrandingShortVM> GetBrandings(string LocationId, string DoctorUsername)
        {
            try
            {
                DBHelper db = new DBHelper();
                db.values.Add("@LocationId", LocationId);
                db.values.Add("@DoctorUsername", DoctorUsername);
                string query = @"select b.logo,b.logo_text,b.phone,b.address,b.footer_text,b.show_graphics,b.Type,b.DoctorUsername from
                Branding.Branding b where (b.Location=@LocationId and Type='Primary') OR
                (b.Location = @LocationId and b.Type = 'Secondary' and b.DoctorUsername = @DoctorUsername) ";

                return DBHelper.GetList<BrandingShortVM>(query, "", db.values);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}