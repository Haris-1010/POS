using CoreMedixBeta.Models.Patient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace CoreMedixBeta.DbModels
{
    public static class Common
    {
        public static string SqlToFullDate(string sql_Date)
        {
            try
            {
                return Convert.ToDateTime(sql_Date).ToString("dddd, MMMM d, yyyy");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return sql_Date;
            }
        }

        public static string SqlToFullTime(string sql_Time)
        {
            try
            {
                return Convert.ToDateTime(sql_Time).ToShortTimeString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return sql_Time;
            }
        }

        /// <summary>
        /// This function will convert C# Date to SQL Date Format
        /// </summary>
        /// <param name="FullDate">C# Full Date String</param>
        /// <returns>A String of SQL Date Format</returns>
        public static string FullDateToSql(string FullDate)
        {
            return Convert.ToDateTime(FullDate).ToString("yyyy-MM-dd");
        }

        public static string FullDateTimeToSql(string DateTime)
        {
            return Convert.ToDateTime(DateTime).ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string FullTimeToSql(string Full_Time)
        {
            return Convert.ToDateTime(Full_Time).ToString("hh:mm:ss");
        }

        public static Dictionary<string, string> ConvertToDictionary(Object obj)
        {
            Dictionary<string, string> Params = new Dictionary<string, string>();
            foreach (System.Reflection.PropertyInfo pi in obj.GetType().GetProperties())
            {
                var k = pi.GetValue(obj);
                if (k != null)
                {
                    Params.Add(pi.Name, pi.GetValue(obj).ToString());
                }
            }
            return Params;
        }

        /// <summary>
        /// This Function will help to read data from API.
        /// </summary>
        /// <typeparam name="T">This is the type of object</typeparam>
        /// <param name="URL">Pass the URL of API here</param>
        /// <param name="Type"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static T ReadFromAPI<T>(string URL, string Type, Dictionary<string, string> Params = null)
        {
            using (HttpClient client = new HttpClient())
            {
                if (Type == "Get")
                {
                    if (Params != null)
                    {
                        URL += GetUrlFromDictionary(Params);
                    }
                    try
                    {
                        return Parser<T>(client.GetAsync(URL).Result);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return default(T);
                    }
                }
                else
                {
                    var lsit = Params.ToList();
                    var parameters = new FormUrlEncodedContent(lsit);
                    var response = client.PostAsync(URL, parameters).Result;
                    return Parser<T>(response);
                }
            }
        }

        public static string GetUrlFromDictionary(Dictionary<string, string> Params)
        {
            string URL = string.Empty;
            URL += "?";
            foreach (var element in Params)
            {
                URL += element.Key + "=" + HttpUtility.UrlEncode(element.Value) + "&";
            }
            URL = URL.Trim('&');
            return URL;
        }

        public static T Parser<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                var responseContent = response.Content;
                string responseString = responseContent.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<T>(responseString);
            }
            else
            {
                return default(T);
            }
        }

        private static string key = "AIzaSyCphzzAmKCopiVf0WuxyARw6Eu20JmUfkg";

        //public static string getToken(string username, string identity, string room)
        //{
        //    var accountSid = ConfigurationManager.AppSettings["TwilioAccountSid"];
        //    var apiKey = ConfigurationManager.AppSettings["TwilioApiKey"];
        //    var apiSecret = ConfigurationManager.AppSettings["TwilioApiSecret"];
        //    var grant = new Twilio.Jwt.AccessToken.VideoGrant();
        //    grant.Room = room;
        //    var grants = new HashSet<Twilio.Jwt.AccessToken.IGrant> { grant };
        //    var token = new Twilio.Jwt.AccessToken.Token(accountSid, apiKey, apiSecret, identity: identity, grants: grants).ToJwt();
        //    return token;
        //}

        public static string generate_room(string sp, string receiver)
        {
            return sp + "-" + receiver;
        }

        public static string generate_single_room(string receiver)
        {
            //receiver is a person whose room we are going to connect. It must be the same at both endpoints.
            return receiver;
        }

        //public static bool AddBookmark(string patient_id, string hp_id)
        //{
        //    Database_Helpers db = new Database_Helpers();
        //    db.values.Add("patient_id", patient_id);
        //    db.values.Add("hp_id", hp_id);
        //    return db.insert("bookmarks", db.values);

        //}

        //private static string GetUrl()
        //{
        //    const string API_URL = "https://www.googleapis.com/urlshortener/v1/url";

        //    if (string.IsNullOrWhiteSpace(key))
        //        return API_URL;
        //    else
        //        return string.Concat(API_URL, "?key=", key);
        //}

        //public static string BitlyIt(string strLongUrl)
        //{
        //    string apiKey = "R_8ecb6c2ad3324a63bc003dac82c60508";
        //    string user = "o_7ofd857op6";
        //    StringBuilder uri = new StringBuilder("http://api.bit.ly/shorten?");

        //    uri.Append("version=2.0.1");

        //    uri.Append("&format=xml");
        //    uri.Append("&longUrl=");
        //    uri.Append(HttpUtility.UrlEncode(strLongUrl));
        //    uri.Append("&login=");
        //    uri.Append(HttpUtility.UrlEncode(user));
        //    uri.Append("&apiKey=");
        //    uri.Append(HttpUtility.UrlEncode(apiKey));

        //    HttpWebRequest request = WebRequest.Create(uri.ToString()) as HttpWebRequest;
        //    request.Method = "GET";
        //    request.ContentType = "application/x-www-form-urlencoded";
        //    request.ServicePoint.Expect100Continue = false;
        //    request.ContentLength = 0;
        //    WebResponse objResponse = request.GetResponse();
        //    XmlDocument objXML = new XmlDocument();
        //    objXML.Load(objResponse.GetResponseStream());

        //    XmlNode nShortUrl = objXML.SelectSingleNode("//shortUrl");

        //    return nShortUrl.InnerText;
        //}

        //public static async Task<string> urlShorter(string url)
        //{
        //    if (!url.Contains("localhost"))
        //    {
        //        url = url.Trim();
        //        CuttlyAPIIntegration cuttlyAPIIntegration = new CuttlyAPIIntegration();
        //        //Models.Rebrandly.RebrandlyAPI rbrnd = new Models.Rebrandly.RebrandlyAPI();
        //        //string _url = await rbrnd.GetURLShortened(url);
        //        string _url = await cuttlyAPIIntegration.GenerateShortUrl(url);
        //        if (!string.IsNullOrWhiteSpace(_url))
        //            return _url;
        //        return url;
        //    }
        //    else
        //    {
        //        return url;
        //    }
        //}

        public static string TimeAgo(DateTime dt)
        {
            TimeSpan span = DateTime.Now - dt;
            if (span.Days > 365)
            {
                int years = (span.Days / 365);
                if (span.Days % 365 != 0)
                    years += 1;
                return String.Format("about {0} {1} ago",
                years, years == 1 ? "year" : "years");
            }
            if (span.Days > 30)
            {
                int months = (span.Days / 30);
                if (span.Days % 31 != 0)
                    months += 1;
                return String.Format("about {0} {1} ago",
                months, months == 1 ? "month" : "months");
            }
            if (span.Days > 0)
                return String.Format("about {0} {1} ago",
                span.Days, span.Days == 1 ? "day" : "days");
            if (span.Hours > 0)
                return String.Format("about {0} {1} ago",
                span.Hours, span.Hours == 1 ? "hour" : "hours");
            if (span.Minutes > 0)
                return String.Format("about {0} {1} ago",
                span.Minutes, span.Minutes == 1 ? "minute" : "minutes");
            if (span.Seconds > 5)
                return String.Format("about {0} seconds ago", span.Seconds);
            if (span.Seconds <= 5)
                return "just now";
            return string.Empty;
        }

        //public static bool removeBookmark(string patient_id, string hp_id)
        //{
        //    Database_Helpers db = new Database_Helpers();
        //    return db.delete("bookmarks", "where patient_id = '" + patient_id + "' and hp_id = '" + hp_id + "'");
        //}

        //public static bool bookmarked(string patient_id, string hp_id)
        //{
        //    Database_Helpers db = new Database_Helpers();
        //    string query = "select count(patient_id) from bookmarks where patient_id = '" + patient_id + "' and hp_id = '" + hp_id + "'";
        //    int i = db.get_scalar(query);
        //    if (i > 0)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }

        //}

        //public static bool ConvertToBool(string Original)
        //{
        //    string value = Original.ToLower();
        //    string[] truecases = new string[] { "yes", "on", "true", "1" };
        //    return truecases.Contains(value) ? true : false;
        //}

        //public static bool alreadysaved(string username, string url)
        //{
        //    int i = 0;
        //    Database_Helpers db = new Database_Helpers();
        //    string query = "select count(username)  from save_search where username = '" + username + "' and searchlink = '" + url + "'";
        //    i = db.get_scalar(query);

        //    if (i > 0)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        //public static bool AddSearch(string username, string search_name, string url)
        //{
        //    Database_Helpers db = new Database_Helpers();
        //    db.values.Add("username", username);
        //    db.values.Add("searchlink", url);
        //    db.values.Add("Name", search_name);
        //    return db.insert("save_search", db.values);
        //}

        //public static bool DeleteSearch(string username, string search)
        //{
        //    Database_Helpers db = new Database_Helpers();
        //    return db.delete("save_search", "where username = '" + username + "' and searchLink =  '" + search + "'");
        //}

        #region RANDOM STRINGS

        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "abcdefghjkmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ023456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string RandomDigits(int lenght)
        {
            const string chars = "123456789";
            return new string(Enumerable.Repeat(chars, lenght)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #endregion RANDOM STRINGS

        #region String Truncate

        public static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        #endregion String Truncate

        #region convert image to base 64

        public static string converttobase64(HttpPostedFileBase image)
        {
            string theFileName = Path.GetFileName(image.FileName);
            byte[] thePictureAsBytes = new byte[image.ContentLength];
            using (BinaryReader theReader = new BinaryReader(image.InputStream))
            {
                thePictureAsBytes = theReader.ReadBytes(image.ContentLength);
            }
            string thePictureDataAsString = Convert.ToBase64String(thePictureAsBytes);
            return thePictureDataAsString;
        }

        #endregion convert image to base 64

        public static bool IsValidEmail(string mail)
        {
            if (string.IsNullOrEmpty(mail))
            {
                return false;
            }
            return new EmailAddressAttribute().IsValid(mail);
        }

        public static int Years(DateTime start, DateTime end)
        {
            return (end.Year - start.Year - 1) +
                (((end.Month > start.Month) ||
                ((end.Month == start.Month) && (end.Day >= start.Day))) ? 1 : 0);
        }

        public static string RemoveSpecialCharacters(this string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Replace(" ", "");
        }

        public static string formatphonenumber(string ph)
        {
            string temp = ph.Substring(0, 1);

            if (temp == "0" && ph.Count() == 11)
            {
                ph = ph.Remove(0, 1);
                ph = "92" + ph;
            }
            else if (temp == "+" && ph.Count() == 13)
            {
                ph = ph.Remove(0, 1);
            }
            return ph;
        }

        /// <summary>
        /// used to convert 03480485244 into 923480485244 or vice versa
        /// </summary>
        /// <param name="PhoneNumber"></param>
        /// <returns></returns>
        public static string FormatPakistanNumber(string PhoneNumber)
        {
            try
            {
                if (PhoneNumber.StartsWith("03"))
                {
                    PhoneNumber = "92" + PhoneNumber.Substring(1);
                }
                else if (PhoneNumber.StartsWith("92"))
                {
                    PhoneNumber = PhoneNumber.Substring(2);
                    PhoneNumber = "0" + PhoneNumber;
                }
                else
                {
                    return string.Empty;
                }

                return PhoneNumber;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        //public static string EncryptDateTime(DateTime dateTime)
        //{
        //    try
        //    {
        //        return InstaCareSecurity.Encrypt(dateTime.ToString()).Replace("+", "--").Replace("/", "---");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        return string.Empty;
        //    }
        //}

        //public static bool AuthenticateURLAccess(string encodedString)
        //{
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(encodedString))
        //        {
        //            var decodedDatetime = InstaCareSecurity.Decrypt(encodedString);
        //            var ConvertedDateTime = Convert.ToDateTime(decodedDatetime);
        //            return ((DateTime.Now - ConvertedDateTime).TotalHours < 1) ? true : false;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        return false;
        //    }
        //}

        public static string FormatInternationalPhoneNumber(string Phone)
        {
            try
            {
                if (!string.IsNullOrEmpty(Phone))
                {
                    if (Phone.StartsWith("03"))
                    {
                        Phone = "92" + Phone.Substring(1, Phone.Length - 1);
                    }
                    else if (Phone.StartsWith("+"))
                    {
                        Phone = Phone.Replace("+", "");
                    }
                    return Phone;
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Phone;
            }
        }

        public static string AddDocument(HttpPostedFileBase file, string folder_name = "")
        {
            if (file != null)
            {
                string TimeAndDate = DateTime.Now.ToString("yyyyMMddhhmmss");
                string _filename = TimeAndDate;
                string extention = file.FileName.Split('.')[1];
                string FileAddress = "_" + _filename + "." + extention;

                if (string.IsNullOrEmpty(folder_name))
                {
                    folder_name = "MyDocuments".ToLower();
                }
                else
                {
                    folder_name = folder_name.ToLower();
                }
                string SaveLocation = ServerConfig.BlobPath + folder_name + "/" + FileAddress;
                BlobUploader.UploadTo(folder_name, FileAddress, file.InputStream);
                return SaveLocation;
            }
            else
            {
                return "";
            }
        }

        public static DataTableValuesDTO GetMappedDataTableValues(HttpContextBase httpContext)
        {
            try
            {
                if (httpContext?.Request == null)
                    return new DataTableValuesDTO();

                int.TryParse(httpContext.Request.Form["length"], out int pageSize);
                int.TryParse(httpContext.Request.Form["start"], out int offset);

                var searchParam = httpContext?.Request?.Form?.GetValues("search[value]");

                return new DataTableValuesDTO
                {
                    SearchString = (searchParam != null && searchParam.Length > 0) ? searchParam[0] : string.Empty,
                    PageSize = pageSize,
                    Offset = offset
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new DataTableValuesDTO();
            }
        }

        public static bool IsPhoneNumber(string username)
        {
            Regex regex = new Regex(@"^[+]?[0-9]*$", RegexOptions.Compiled);
            return regex.IsMatch(username);
        }

        //public static async Task<bool> SendWelcomeSMS(string phone, string name, string docUsername = "", string location_id = "")
        //{
        //    try
        //    {
        //        var clinicDetails = new ViewModels.InstaUser.ShortUser();
        //        string clinicName = "InstaCare";
        //        if (!string.IsNullOrEmpty(location_id))
        //        {
        //            clinicDetails = await ViewModels.InstaUser.ShortUser.GetAsync(location_id);
        //            clinicName = clinicDetails.Name;
        //        }

        //        string doctorName = "";
        //        if (!string.IsNullOrEmpty(docUsername))
        //        {
        //            var doctorDetails = await ViewModels.InstaUser.ShortUser.GetAsync(docUsername);
        //            doctorName = doctorDetails.Name;
        //        }

        //        string Message = $@"Dear " + name + ". Welcome to " + (!string.IsNullOrEmpty(doctorName) ? doctorName + ", " : "") + " " + clinicName + ". Download InstaCare App Now. https://s.instacare.pk/app .";

        //        if (clinicDetails != null && !string.IsNullOrEmpty(clinicDetails.Phone))
        //        {
        //            string profileLink = Repositories.UserRepository.GetInstaCareB2CDoctorProfileLink(docUsername);
        //            Message += " For appointment you can contact " + clinicDetails.Phone + "";
        //            if (!string.IsNullOrEmpty(profileLink))
        //            {
        //                Message += " or use following link " + profileLink;
        //            }
        //            Message += " .";
        //        }
        //        var smsOutput = await SMSSender.SendSMS(new SendSMSInputDTO
        //        {
        //            Phone = phone,
        //            Message = Message,
        //            LocationID = location_id,
        //            Username = clinicDetails.MasterUsername,
        //            MasterUsername = clinicDetails.MasterUsername
        //        });
        //        return smsOutput.IsSent;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        return false;
        //    }
        //}

        //public static string GetLocationIdFromSession()
        //{
        //    try
        //    {
        //        var user = HttpContext.Current?.Session?["User"] as SessionUser;
        //        return user?.location ?? string.Empty;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        return string.Empty;
        //    }
        //}

        //public static string GetMasterUsernameFromSession()
        //{
        //    try
        //    {
        //        var user = HttpContext.Current?.Session?["User"] as SessionUser;
        //        return user?.MasterUsername ?? string.Empty;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        return string.Empty;
        //    }
        //}
    }
}