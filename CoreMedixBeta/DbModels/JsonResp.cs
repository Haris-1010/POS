using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.DbModels
{
    public class JsonResp
    {
        public bool status { get; set; }
        public string message { get; set; }
        public string redirectUrl { get; set; }
        public int responsecode { get; set; }

        public JsonResp()
        {
        }

        public JsonResp(bool status, string message)
        {
            this.status = status;
            this.message = message;
        }

        public JsonResp(bool status, string message, string redirectUrl, int responsecode)
        {
            this.status = status;
            this.message = message;
            this.redirectUrl = redirectUrl;
            this.responsecode = responsecode;
        }
    }
}