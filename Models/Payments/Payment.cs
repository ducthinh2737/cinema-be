using System;
using CinemaBooking.API.Models.Bookings;

namespace CinemaBooking.API.Models.Payments
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
    }
}
