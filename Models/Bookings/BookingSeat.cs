using CinemaBooking.API.Models.Cinemas;

namespace CinemaBooking.API.Models.Bookings
{
    public class BookingSeat
    {
        public int BookingSeatId { get; set; }

        public int BookingId { get; set; }

        public int SeatId { get; set; }

        public decimal UnitPrice { get; set; }

        public Booking Booking { get; set; } = null!;

        public Seat Seat { get; set; } = null!;
    }
}
