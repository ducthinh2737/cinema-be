using System;
using CinemaBooking.API.Models.Bookings;

namespace CinemaBooking.API.Models.Users
{
    public class LoyaltyTransaction
    {
        public int LoyaltyTransactionId { get; set; }
        public int UserId { get; set; }
        public int? BookingId { get; set; }
        public int PointsChanged { get; set; }             // Positive (earned) or negative (redeemed)
        public string TransactionType { get; set; } = null!; // Earn, Redeem, Refund, EventBonus, ManualAdjustment
        public string Status { get; set; } = null!;          // Pending, Completed, Cancelled
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public Booking? Booking { get; set; }
    }
}
