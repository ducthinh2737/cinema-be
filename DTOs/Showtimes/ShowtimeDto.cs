using System;

namespace CinemaBooking.API.DTOs.Showtimes
{
    public class ShowtimeDto
    {
        public int ShowtimeId { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = null!;
        public int HallId { get; set; }
        public string HallName { get; set; } = null!;
        public int CinemaId { get; set; }
        public string CinemaName { get; set; } = null!;
        public int PriceId { get; set; }
        public decimal PriceValue { get; set; }
        public string TicketType { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int AvailableSeats { get; set; }
        public int TotalSeats { get; set; }
        public string Status { get; set; } = null!;
        public CinemaBooking.API.DTOs.Cinemas.HallDto? Hall { get; set; }
        public CinemaBooking.API.DTOs.Movies.MovieDto? Movie { get; set; }
    }

    public class ShowtimeCreateDto
    {
        public int MovieId { get; set; }
        public int HallId { get; set; }
        public int PriceId { get; set; }
        public DateTime StartTime { get; set; }
    }

    public class ShowtimeUpdateDto
    {
        public int MovieId { get; set; }
        public int HallId { get; set; }
        public int PriceId { get; set; }
        public DateTime StartTime { get; set; }
    }

    public class ShowtimeBulkCreateDto
    {
        public int MovieId { get; set; }
        public int HallId { get; set; }
        public int PriceId { get; set; }
        public System.Collections.Generic.List<DateTime> Dates { get; set; } = null!;
        public System.Collections.Generic.List<string> TimeSlots { get; set; } = null!;
        public bool FlatPriceEnabled { get; set; }
        public decimal? FlatPrice { get; set; }
    }

    public class ShowtimeQueryParameters
    {
        private const int MaxPageSize = 1000;
        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        public int? CinemaId { get; set; }
        public int? MovieId { get; set; }
        public DateTime? Date { get; set; }
        public string? Status { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; }
    }
}
