using System;
using System.Collections.Generic;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.Models.Users;
using CinemaBooking.API.Models.Payments;

namespace CinemaBooking.API.Models.Bookings
{
    public class Booking
    {
        public int BookingId { get; set; }

        public string BookingCode { get; set; } = null!;

        public int UserId { get; set; }

        public int ShowtimeId { get; set; }

        public decimal TotalAmount { get; set; }

        public string BookingStatus { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public decimal ServiceFee { get; set; }

        public decimal DiscountAmount { get; set; }

        public string? QRCodeUrl { get; set; }

        public byte[] RowVersion { get; set; } = null!;

        public User User { get; set; } = null!;

        public Showtime Showtime { get; set; } = null!;

        public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
