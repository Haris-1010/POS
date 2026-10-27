using CoreMedixBeta.DbModels;
using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services.Description;

namespace CoreMedixBeta.Repositories
{
    [Schema("Branding")]
    [Table("Branding")]
    public class BrandingRepository
    {
        [DontUpdate]
        public string sms_masking { get; set; }

        public string logo { get; set; }
        public string headerImage { get; set; }
        public string WaterMarkImage { get; set; }
        public string show_graphics { get; set; }
        public string logo_text { get; set; }
        public string footer_text { get; set; }
        public string phone { get; set; }
        public string address { get; set; }
        public string primary_color { get; set; }
        public string Location { get; set; }
        public string DoctorUsername { get; set; }
        public string Type { get; set; }
        public string MaskingType { get; set; }
        public string BrandingSource { get; set; }

        public BrandingRepository()
        {
            this.sms_masking = string.Empty;
            this.logo = string.Empty;
            this.headerImage = string.Empty;
            this.WaterMarkImage = string.Empty;
            this.show_graphics = "False";
            this.footer_text = string.Empty;
            this.logo_text = string.Empty;
            this.phone = string.Empty;
            this.MaskingType = string.Empty;
            this.address = string.Empty;
            this.primary_color = "#090650";
            this.Location = string.Empty;
        }

        public static BrandingRepository get(string Location)
        {
            try
            {
                string query = "Select * from Branding.Branding where Location='" + Location + "' and Type='Primary' ";
                List<BrandingRepository> temp = new List<BrandingRepository>();

                temp = DBHelper.GetList<BrandingRepository>(query);
                return temp.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new BrandingRepository();
            }
        }

        public BrandingRepository GetLocationBranding(string locationID)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>();
                dbValues.Add("@LocationID", locationID);
                string query = @"SELECT * FROM Branding.Branding WHERE Location = @LocationID AND DoctorUsername = '' AND Type = 'Primary'";
                return DBHelper.Get<BrandingRepository>(query, string.Empty, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new BrandingRepository();
            }
        }

        public bool UpdateSMSMasking(string locationID, string smsMasking, string phone, string doctorUsername, string type)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@LocationID", locationID },
                    { "@SMSMasking", smsMasking },
                    { "@DoctorUsername", string.Empty },
                    { "@Type", type },
                    { "@Phone", phone }
                };

                string selectQuery = @"SELECT COUNT(*) FROM Branding.Branding
                               WHERE Location = @LocationID AND Type = 'Primary' AND BrandingSource = 'Primary'";

                int count = Convert.ToInt32(DBHelper.GetScaler(selectQuery, dbValues));

                if (count == 0)
                {
                    string insertQuery = @"INSERT INTO Branding.Branding (Location, Type, BrandingSource,phone, sms_masking,DoctorUsername,MaskingType)
                                   VALUES (@LocationID,'Primary','Primary',@Phone,@SMSMasking,@DoctorUsername,@Type)";
                    return DBHelper.ExecuteQuery(insertQuery, dbValues);
                }
                else
                {
                    string updateQuery = @"UPDATE Branding.Branding SET sms_masking = @SMSMasking , MaskingType = @Type
                                   WHERE Location = @LocationID AND Type = 'Primary' AND BrandingSource = 'Primary'";
                    return DBHelper.ExecuteQuery(updateQuery, dbValues);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public bool update_branding(string location, string Type = "", string Username = "", string BrandingSource = "")
        {
            string checkquery = "select Count(*) from Branding.Branding where Location='" + location + "' and Type = '" + Type + "' ";

            if (Type == "Secondary")
            {
                checkquery += " and DoctorUsername = '" + Username + "'";
            }

            int status = DBHelper.GetScaler(checkquery);
            string procquery = string.Empty;

            if (status > 0)
            {
                DBHelper db = new DBHelper();
                db.values.Add("@Location", location);
                db.values.Add("@Username", Username);
                if (this.Type == "Primary")
                {
                    this.BrandingSource = "Primary";
                    this.DoctorUsername = "";
                    return DBHelper.Update(this, "where Location=@Location and Type = 'Primary'", db.values);
                }
                else
                {
                    this.BrandingSource = BrandingSource;
                    this.Type = "Secondary";
                    this.DoctorUsername = Username;
                    return DBHelper.Update(this, "where Location=@Location and DoctorUsername = @Username and Type='Secondary' ", db.values);
                }
            }
            else
            {
                if (this.Type == "Primary")
                {
                    this.DoctorUsername = "";
                    this.BrandingSource = "Primary";
                }
                else
                {
                    this.Type = "Secondary";
                    this.BrandingSource = "Secondary";
                    this.DoctorUsername = Username;
                }
                return DBHelper.Insert(this);
            }
        }

        public static string get_sms_masking(string Location = "")
        {
            try
            {
                DBHelper db = new DBHelper();
                db.values.Add("@LocationId", Location);
                string query = "select top 1 sms_masking from Branding.Branding where Location=@LocationId and sms_masking != '' and sms_masking is not null ";
                return DBHelper.GetSingleColumn(query, db.values);
            }
            catch (Exception)
            {
                return "PharmaX";
            }
        }

        //Apply Masking Code Taken from InstaCare.pk
        public bool applymasking(string serviceprovider, string masking, string masking_username, string masking_password)
        {
            bool temp = false;
            try
            {
                string base_url = ServerConfig.sms_masking_url(masking);
                string checkquery = "select Count(*) from Branding.Branding where Location='" + serviceprovider + "'";
                int status = DBHelper.GetScaler(checkquery);
                string procquery = string.Empty;
                if (status > 0)
                {
                    string query = "Update Branding.Branding SET sms_masking='" + masking + "',sms_masking_url='" + base_url + "' where Location='" + serviceprovider + "'";
                    temp = DBHelper.ExecuteQuery(query);
                }
                else
                {
                    BrandingRepository br = new BrandingRepository();
                    br.Location = serviceprovider;
                    br.sms_masking = masking;
                    //br.sms_masking_url = base_url;
                    temp = br.update_branding(serviceprovider, "", masking_username);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return temp;
        }

        public bool UpdateBrandingSource(string DoctorUsername, string LocationId, string Source)
        {
            try
            {
                DBHelper db = new DBHelper();
                db.values.Add("@DoctorUsername", DoctorUsername);
                db.values.Add("@LocationId", LocationId);
                db.values.Add("@BrandingSource", Source);
                string query = "update Branding.Branding set BrandingSource=@BrandingSource where Location=@LocationId and DoctorUsername=@DoctorUsername";
                return DBHelper.ExecuteQuery(query, db.values);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public static string GetBrandingSource(string DoctorUsername, string LocationId)
        {
            try
            {
                string query = "select BrandingSource from Branding.Branding where Location='" + LocationId + "' and DoctorUsername='" + DoctorUsername + "' ";
                string result = DBHelper.GetSingleColumn(query);
                return !string.IsNullOrEmpty(result) ? result : "Primary";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "Primary";
            }
        }

        public static BrandingRepository GetDoctorBasedBranding(string DoctorUsername, string LocationId)
        {
            try
            {
                string query = "Select * from Branding.Branding where Location='" + LocationId + "' and DoctorUsername='" + DoctorUsername + "' ";
                var temp = DBHelper.Get<BrandingRepository>(query);
                return temp;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public BrandingRepository GetBranding(string LocationId, string DoctorUsername, string Source)
        {
            try
            {
                string query = "select * from Branding.Branding where Location='" + LocationId + "' ";
                if (Source == "Primary")
                {
                    query += " and Type='Primary' ";
                    return DBHelper.Get<BrandingRepository>(query);
                }
                else
                {
                    query += " and DoctorUsername ='" + DoctorUsername + "' and Type='Secondary' ";
                    return DBHelper.Get<BrandingRepository>(query);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}