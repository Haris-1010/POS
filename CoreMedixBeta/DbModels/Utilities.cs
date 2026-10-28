using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.DbModels
{
    public class Utilities
    {
        /// <summary>
        /// Normalizes a retail barcode value:
        /// - 13 digits with leading 0 (EAN-13 rendering of UPC-A) -> strip the 0
        /// - exactly 12 digits -> correct the GS1 UPC-A check digit so decoders accept it
        /// Anything else is returned trimmed and unchanged.
        /// </summary>
        public static string NormalizeUpc(string upc)
        {
            if (string.IsNullOrWhiteSpace(upc)) return upc;
            var t = upc.Trim();
            if (t.Length == 13 && t[0] == '0' && t.All(char.IsDigit))
                t = t.Substring(1);
            if (t.Length == 12 && t.All(char.IsDigit))
            {
                var data11 = t.Substring(0, 11);
                return data11 + ComputeUpcCheckDigit(data11);
            }
            return t;
        }

        /// <summary>GS1 UPC-A check digit for the first 11 digits.</summary>
        public static string ComputeUpcCheckDigit(string first11)
        {
            int sum = 0;
            for (int i = 0; i < 11; i++)
            {
                int d = first11[i] - '0';
                sum += (i % 2 == 0) ? d * 3 : d;
            }
            return ((10 - (sum % 10)) % 10).ToString();
        }
    }
}