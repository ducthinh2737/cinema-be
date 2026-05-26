using System.Collections.Generic;

namespace CinemaBooking.API.DTOs.Admin
{
    public class AdminDashboardDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int TotalUsers { get; set; }
        public double ConversionRate { get; set; }
        public List<MovieRevenueDto> TopMovies { get; set; } = new();
        public List<CinemaRevenueDto> TopCinemas { get; set; } = new();
        public List<MonthlyRevenueDto> MonthlyRevenueChart { get; set; } = new();
    }

    public class MovieRevenueDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = null!;
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }
    }

    public class CinemaRevenueDto
    {
        public int CinemaId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Revenue { get; set; }
        public int BookingsCount { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Revenue { get; set; }
    }

    public class AdminStatisticsDto
    {
        public int TotalMovies { get; set; }
        public int TotalCinemas { get; set; }
        public int TotalShowtimes { get; set; }
        public int ActiveUsers { get; set; }
        public double AverageMovieRating { get; set; }
    }

    public class RevenueStatsDto
    {
        public decimal CashRevenue { get; set; }
        public decimal MomoRevenue { get; set; }
        public decimal VNPayRevenue { get; set; }
        public decimal ZaloPayRevenue { get; set; }
        public decimal PayPalRevenue { get; set; }
        public decimal Total { get; set; }
    }
}
