using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Web;

namespace CoreMedixBeta.DbModels
{
    public static class ServerConfig
    {
        private static readonly MemoryCache _cache = MemoryCache.Default;
        public static Func<string> GetMasterUsernameCallback { get; set; }
        public static Func<string> GetLocationIDCallback { get; set; }

        public static string server_path = ConfigurationManager.AppSettings["ServerPath"].ToString();
        public static string appVersion = ConfigurationManager.AppSettings["AppVersion"].ToString();
        public static int AccessCountFailedLimit = Convert.ToInt32(ConfigurationManager.AppSettings["AccessCountFailedLimit"]);

        public static string apiserver = ConfigurationManager.AppSettings["apiserver"].ToString();
        public static string ApiTestingServer = ConfigurationManager.AppSettings["apitestingserver"].ToString();
        public static string paymentsapiserver = ConfigurationManager.AppSettings["paymentsapiserver"];
        public static string PharmapediaServer = ConfigurationManager.AppSettings["PharmapediaServer"];

        public static string cookie_path = ConfigurationManager.AppSettings["CookiePath"].ToString();
        public static string email_server = ConfigurationManager.AppSettings["emailserver"].ToString();
        public static string email_username = ConfigurationManager.AppSettings["emailusername"].ToString();
        public static string email_password = ConfigurationManager.AppSettings["emailpassword"].ToString();
        public static string SendGridAPIKey = ConfigurationManager.AppSettings["SendGridAPIKey"].ToString();

        public static int TokenPrintFontSize = Convert.ToInt32(ConfigurationManager.AppSettings["TokenPrintFontSize"]);
        public static int TokenPrintPageHeight = Convert.ToInt32(ConfigurationManager.AppSettings["TokenPrintPageHeight"]);

        public static string ml_key = ConfigurationManager.AppSettings["MicrosoftCS"].ToString();

        public static string GooglePlacesApi = ConfigurationManager.AppSettings["GooglePlacesApi"].ToString();

        public static string SystemKey = ConfigurationManager.AppSettings["ApiKey"].ToString();
        public static string BlobPath = "/Uploads/"; 
        public static string BlobName = "instacarepk";
        public static string ApiKey = "9NsfW4NrmGaj5IASUTmn1JhO0Yj7lsruAzzBDVQEPS9X4JDKLYDCVD9SxIILpLOUWNrxckb9EiB/YohCvLL+5g==";
        public static string FacebookAppID = ConfigurationManager.AppSettings["FacebookAppID"];
        public static string FacebookAppSecret = ConfigurationManager.AppSettings["FacebookAppSecret"];

        public static string GoogleClientId = ConfigurationManager.AppSettings["GoogleClientId"];
        public static string GoogleClientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"];

        public static string InstaCareAdminEmail = ConfigurationManager.AppSettings["InstaCareAdminEmail"].ToString();
        public static string HubSpotAPIKey = ConfigurationManager.AppSettings["HubSpotAPIKey"];

        //Excel Labs
        public static string el_url = "http://api.excel-labs.com/ExcelPanelIntegration.svc/";

        public static string el_key = "BC3780AB-AAA8-4DA0-839C-37D74E3568C7";

        public static string places_Api = "AIzaSyBKh_QIFCdxpcY8bvq-wfy-bkvLKsJTSU4";
        public static string scriptsource = "https://maps.googleapis.com/maps/api/js?key=" + ServerConfig.GooglePlacesApi + "&v=3.exp&sensor=false&libraries=places";
        public static string scriptSource2 = "https://maps.googleapis.com/maps/api/place/queryautocomplete/json?key=" + places_Api + "&input=";
        public static string DefaultImage = server_path + "assets/img/Image-not-found.jpg";
        public static string DefaultFacilityImage = server_path + "assets/img/facility-not-found.jpg";
        public static string DefaultBackground = server_path + "assets/img/profile_header.jpg";
        public const string SubscriptionsAdminEmail = "subscriptions@instacare.pk";

        // SMS API

        //Dont add / before sms else API will be not called
        public static string SMSApiProduction = apiserver + "api/sms/send";

        public static string SMSApiShortCode = "https://instacare.pk/api/SMS/SendShortCode?SystemKey=6b2e7679-34dd-41f9-b0cd-ac0ea0f8bf8b";
        public static string SMSApiBranded = "https://instacare.pk/api/SMS/SendBrandedSMS?SystemKey=6b2e7679-34dd-41f9-b0cd-ac0ea0f8bf8b";
        public static string SMSApiBrandedLocal = "https://localhost:44396/api/SMS/SendBrandedSMS?SystemKey=6b2e7679-34dd-41f9-b0cd-ac0ea0f8bf8b";

        //Dont add / before sms else API will be not called
        public static string SMSApiStaging = ApiTestingServer + "api/sms/send";

        public static string InstaCareDefaultSMSMasking = "PharmaX";

        //Location Getting API 

        #region Eocean Constants

        public static string EOceansAPISMSUsername_EMR = ConfigurationManager.AppSettings["EOcenasSMSAPIUsername_EMR"];
        public static string EOceansAPISMSPassword_EMR = ConfigurationManager.AppSettings["EOcenasSMSAPIPassword_EMR"];
        public static string EOceansAPISMSResponse = ConfigurationManager.AppSettings["EOcenasSMSAPIResponse"];
        public static string BrandedSMSEoceanAPI = $"https://pk.eocean.net/APIManagement/API/RequestAPI?user={EOceansAPISMSUsername_EMR}&pwd={EOceansAPISMSPassword_EMR}";

        public static string EOceansAPISMSUsername_Trans = ConfigurationManager.AppSettings["EOcenasSMSAPIUsername_Transactional"];
        public static string EOceansAPISMSPassword_Trans = ConfigurationManager.AppSettings["EOcenasSMSAPIPassword_Transactional"];
        public static string EOceansAPISMSResponse_Trans = ConfigurationManager.AppSettings["EOcenasSMSAPIResponse_Transactional"];
        public static string BrandedSMSEoceanAPI_Trans = $"https://pk.eocean.net/APIManagement/API/RequestAPI?user={EOceansAPISMSUsername_Trans}&pwd={EOceansAPISMSPassword_Trans}";

        public static string EOceansAPISMSUsername_Promo = ConfigurationManager.AppSettings["EOcenasSMSAPIUsername_Promotional"];
        public static string EOceansAPISMSPassword_Promo = ConfigurationManager.AppSettings["EOcenasSMSAPIPassword_Promotional"];
        public static string EOceansAPISMSResponse_Promo = ConfigurationManager.AppSettings["EOcenasSMSAPIResponse_Promotional"];
        public static string BrandedSMSEoceanAPI_Promo = $"https://pk.eocean.net/APIManagement/API/RequestAPI?user={EOceansAPISMSUsername_Promo}&pwd={EOceansAPISMSPassword_Promo}";

        public static string EOcenasSC_API_URL = ConfigurationManager.AppSettings["EOcenasSC_API_URL"];
        public static string EOcenasSC_API_KEY = ConfigurationManager.AppSettings["EOcenasSC_API_KEY"];
        public static string EOcenasSC_API_ContentType = ConfigurationManager.AppSettings["EOcenasSC_API_ContentType"];
        public static string EOcenasSC_API_ShortCode = ConfigurationManager.AppSettings["EOcenasSC_API_ShortCode"];

        #endregion Eocean Constants

        public static string sms_masking_url(string mask)
        {
            string burl = " http://u.ezeee.pk/sendsms_url.html?Username=03554832273&Password=@03554832273&From=" + mask + "&To=#phone#&Message=#message#";
            return burl;
        }

        private enum service_provider_types
        {
            Doctor,
            Fitness,
            Spa,
            Optical
        }

        public static bool is_indvidual_service_provider(string provider)
        {
            try
            {
                var resp = Enum.Parse(typeof(service_provider_types), provider, true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Old Version of DateTimeNow
        /// </summary>
        public static DateTime DateTimeNowOld
        {
            get { return GetPakDate(); }
        }

        /// <summary>
        /// Latest Version of DateTimeNow.
        /// </summary>
        public static DateTime DateTimeNow
        {
            get { return GetSystemDate(); }
        }

        private static DateTime GetPakDate()
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            DateTime localDateTime = DateTime.Parse(date);
            DateTime utcDateTime = localDateTime.ToUniversalTime();
            string PKTimeZoneKey = "Pakistan Standard Time";
            TimeZoneInfo PkTimeZone = TimeZoneInfo.FindSystemTimeZoneById(PKTimeZoneKey);
            DateTime PkDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, PkTimeZone);
            return PkDateTime;
        }

        private static DateTime GetSystemDate()
        {
            string masterUsername = GetMasterUsernameCallback?.Invoke() ?? "";
            string locationID = GetLocationIDCallback?.Invoke() ?? "";

            string cacheKey = $"TimeZone_{masterUsername}_{locationID}";

            string timeZoneId = _cache.Get(cacheKey) as string;
            if (string.IsNullOrEmpty(timeZoneId))
            {
                if (!string.IsNullOrWhiteSpace(locationID) &&
                    !string.IsNullOrWhiteSpace(masterUsername))
                {
                    //timeZoneId = InstaCareEMR.Core.Utilities.UserUtils.GetTimeZone(locationID, masterUsername);
                }

                if (string.IsNullOrEmpty(timeZoneId))
                {
                    timeZoneId = "Pakistan Standard Time";
                }
            }

            try
            {
                string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                DateTime localDateTime = DateTime.Parse(date);
                DateTime utcDateTime = localDateTime.ToUniversalTime();
                TimeZoneInfo systemTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                DateTime systemDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, systemTimeZone);
                return systemDateTime;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting time zone '{timeZoneId}': {ex.Message}. Returning Pakistan Standard time.");

                string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                DateTime localDateTime = DateTime.Parse(date);
                DateTime utcDateTime = localDateTime.ToUniversalTime();
                TimeZoneInfo systemTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
                DateTime systemDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, systemTimeZone);
                return systemDateTime;
            }
        }

        public static string FormattedTime()
        {
            return GetSystemDate().ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string GetLocalIpAddress()
        {
            string hostname = Dns.GetHostName();
            return Dns.GetHostEntry(hostname).AddressList
                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                ?.ToString();
        }

        public static string GetIPAddress()
        {
            HttpContext context = HttpContext.Current;
            string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipAddress))
            {
                string[] addresses = ipAddress.Split(',');
                if (addresses.Length != 0)
                {
                    return addresses[0];
                }
            }

            return context.Request.ServerVariables["REMOTE_ADDR"];
        }
    }
}