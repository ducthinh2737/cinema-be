using System;
using System.Collections.Generic;
using CinemaBooking.API.Models.Cinemas;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.Models.Bookings;

namespace CinemaBooking.API.Models.Showtimes
{
    public class Showtime
    {
        public int ShowtimeId { get; set; }

        public int MovieId { get; set; }

        public int HallId { get; set; }

        public int PriceId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public byte[] RowVersion { get; set; } = null!;
        public bool? IsPriceOverride { get; set; }
        public decimal? CustomPrice { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string Status { get; set; } = "Active";

        public Movie Movie { get; set; } = null!;

        public Hall Hall { get; set; } = null!;

        public Price Price { get; set; } = null!;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
