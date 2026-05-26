using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CinemaBooking.API.Helpers
{
    public class VNPayHelper
    {
        public static string CreatePaymentUrl(string baseUrl, string tmnCode, string hashSecret, string returnUrl, string txnRef, decimal amount, string orderInfo, string ipAddress)
        {
            var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", tmnCode },
                { "vnp_Amount", ((long)(amount * 100)).ToString() },
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", ipAddress },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", orderInfo },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_TxnRef", txnRef }
            };

            var rawData = string.Join("&", vnpParams.Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));
            var secureHash = HmacSha512(hashSecret, rawData);
            
            return $"{baseUrl}?{rawData}&vnp_SecureHash={secureHash}";
        }

        public static bool VerifySignature(Dictionary<string, string> queryParameters, string secureHash, string hashSecret)
        {
            var sortedParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in queryParameters)
            {
                if (!string.IsNullOrEmpty(kv.Key) && kv.Key.StartsWith("vnp_") && kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                {
                    sortedParams.Add(kv.Key, kv.Value);
                }
            }

            var rawData = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));
            var calculatedHash = HmacSha512(hashSecret, rawData);
            
            return calculatedHash.Equals(secureHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }
}
