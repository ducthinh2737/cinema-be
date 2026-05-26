namespace CinemaBooking.API.Models.Bookings
{
    public class Ticket
    {
        public int TicketId { get; set; }
        public string TicketCode { get; set; } = null!;
        public decimal Price { get; set; }
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
        public int BookingSeatId { get; set; }
        public BookingSeat BookingSeat { get; set; } = null!;
    }
}
