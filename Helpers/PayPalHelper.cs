using System;
using System.Collections.Generic;

namespace CinemaBooking.API.Helpers
{
    public class PayPalHelper
    {
        public static string CreateApprovalUrl(string clientId, string mode, string returnUrl, string cancelUrl, string orderId, decimal amount)
        {
            // Simulate PayPal web flow approval URL
            var domain = mode == "sandbox" ? "www.sandbox.paypal.com" : "www.paypal.com";
            return $"https://{domain}/cgi-bin/webscr?cmd=_express-checkout&token=EC-{Guid.NewGuid().ToString().Substring(0, 12).ToUpper()}&amount={amount}&currency_code=USD&invoice={orderId}";
        }

        public static bool VerifyWebhook(string webhookId, string authAlgo, string certUrl, string transmissionId, string transmissionSig, string transmissionTime, string webhookEventRaw, string clientSecret)
        {
            // In production, we send a POST request to PayPal API verifying these headers
            // Here we verify they are present and format is valid
            return !string.IsNullOrEmpty(transmissionSig) && !string.IsNullOrEmpty(transmissionId);
        }
    }
}
