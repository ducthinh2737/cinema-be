namespace CinemaBooking.API.Models.Bookings
{
    public class BookingPromotion
    {
        public int BookingPromotionId { get; set; }

        public int BookingId { get; set; }

        public int PromotionId { get; set; }
    }
}
