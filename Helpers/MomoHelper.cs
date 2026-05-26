using System;
using System.Security.Cryptography;
using System.Text;

namespace CinemaBooking.API.Helpers
{
    public class MomoHelper
    {
        public static string CreateSignature(string partnerCode, string accessKey, string requestId, string amount, string orderId, string orderInfo, string redirectUrl, string ipnUrl, string extraData, string requestType, string secretKey)
        {
            var rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
            return HmacSha256(secretKey, rawHash);
        }

        public static bool VerifySignature(string partnerCode, string orderId, string requestId, string amount, string message, string resultStatus, string transId, string extraData, string signature, string secretKey)
        {
            // Momo callback signature raw format:
            // partnerCode=$partnerCode&orderId=$orderId&requestId=$requestId&amount=$amount&orderInfo=$orderInfo&orderType=$orderType&transId=$transId&resultCode=$resultCode&message=$message&payType=$payType&extraData=$extraData
            // Let's implement a standard verifier based on key params
            var rawHash = $"amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&partnerCode={partnerCode}&requestId={requestId}&resultCode={resultStatus}&transId={transId}";
            var calculated = HmacSha256(secretKey, rawHash);
            return calculated.Equals(signature, StringComparison.OrdinalIgnoreCase);
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
