using System;

namespace CinemaBooking.API.Models.Payments
{
    public class Refund
    {
        public int RefundId { get; set; }
        public string RefundReason { get; set; } = null!;
        public decimal RefundAmount { get; set; }
        public string RefundStatus { get; set; } = null!;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public int PaymentId { get; set; }
        public Payment Payment { get; set; } = null!;
    }
}
