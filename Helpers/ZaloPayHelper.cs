using System;
using System.Security.Cryptography;
using System.Text;

namespace CinemaBooking.API.Helpers
{
    public class ZaloPayHelper
    {
        public static string CreateSignature(string appId, string appTransId, string appUser, string amount, string appTime, string embedData, string item, string key1)
        {
            var rawData = $"{appId}|{appTransId}|{appUser}|{amount}|{appTime}|{embedData}|{item}";
            return HmacSha256(key1, rawData);
        }

        public static bool VerifyCallbackSignature(string data, string requestSignature, string key2)
        {
            var calculated = HmacSha256(key2, data);
            return calculated.Equals(requestSignature, StringComparison.OrdinalIgnoreCase);
        }

        private static string HmacSha256(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                var hash = new StringBuilder();
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
                return hash.ToString();
            }
        }
    }
}
