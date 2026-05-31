using System;
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

    public class DashboardSummaryDto : AdminDashboardDto
    {
        public double OccupancyRate { get; set; }
        public double CancellationRate { get; set; }
        public decimal RevenueGrowthPercentage { get; set; }
        public int ActiveUsersToday { get; set; }
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
        public double OccupancyRate { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Revenue { get; set; }
        public int BookingsCount { get; set; }
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
        public decimal VietQRRevenue { get; set; }
        public decimal Total { get; set; }
    }

    public class RevenueAnalyticsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal CashRevenue { get; set; }
        public decimal VietQRRevenue { get; set; }
        public decimal GrowthPercentage { get; set; }
        public List<DailyRevenueDto> DailyChart { get; set; } = new();
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int BookingsCount { get; set; }
    }

    public class BookingAnalyticsDto
    {
        public int TotalBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int PendingBookings { get; set; }
        public int CancelledBookings { get; set; }
        public double ConversionRate { get; set; }
        public double CancellationRate { get; set; }
        public List<PeakHourDto> PeakBookingHours { get; set; } = new();
    }

    public class PeakHourDto
    {
        public int Hour { get; set; }
        public int BookingsCount { get; set; }
    }

    public class CinemaAnalyticsDto
    {
        public int CinemaId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Revenue { get; set; }
        public int BookingsCount { get; set; }
        public double OccupancyRate { get; set; }
    }

    public class MovieAnalyticsDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = null!;
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }
    }
}
