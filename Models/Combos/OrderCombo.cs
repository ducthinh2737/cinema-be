using CinemaBooking.API.Models.Bookings;

namespace CinemaBooking.API.Models.Combos
{
    public class OrderCombo
    {
        public int OrderComboId { get; set; }
        
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        public int ComboId { get; set; }
        public Combo Combo { get; set; } = null!;

        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
