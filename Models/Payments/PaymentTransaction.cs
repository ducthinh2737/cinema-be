using System;

namespace CinemaBooking.API.Models.Payments
{
    public class PaymentTransaction
    {
        public int PaymentTransactionId { get; set; }
        public string TransactionReference { get; set; } = null!;
        public string GatewayName { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public int PaymentId { get; set; }
        public Payment Payment { get; set; } = null!;
    }
}
