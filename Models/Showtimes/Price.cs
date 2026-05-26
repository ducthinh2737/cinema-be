using System.Collections.Generic;

namespace CinemaBooking.API.Models.Showtimes
{
    public class Price
    {
        public int PriceId { get; set; }
        public decimal Value { get; set; }
        public string TicketType { get; set; } = null!;
        public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
    }
}
