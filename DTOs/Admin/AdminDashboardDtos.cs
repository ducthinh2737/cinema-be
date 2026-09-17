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
        public double OccupancyRate { get; set; }
        public int ShowtimesCount { get; set; }
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

    public class DashboardDataDto
    {
        public DashboardKpisDto Kpis { get; set; } = new();
        public List<DailyRevenueDto> Revenue7Days { get; set; } = new();
        public List<MovieRevenueDto> TopMovies { get; set; } = new();
        public List<SeatTypeOccupancyDto> OccupancyBySeatType { get; set; } = new();
        public List<TopCustomerDto> TopCustomers { get; set; } = new();
        public List<HallRevenueDto> RevenueByHall { get; set; } = new();
        public List<TopShowtimeDto> TopShowtimes { get; set; } = new();
        public List<GoldenHourRevenueDto> GoldenHours { get; set; } = new();
        public List<DashboardAlertDto> Alerts { get; set; } = new();
        public ConcessionAnalyticsDto ConcessionAnalytics { get; set; } = new();
    }

    public class ConcessionAnalyticsDto
    {
        public decimal TotalComboRevenue { get; set; }
        public double ComboAttachRate { get; set; } // Percentage of bookings that contain at least one combo
        public decimal AverageBookingComboValue { get; set; } // Average total amount of bookings that contain combos
        public List<ComboSalesDto> TopSellingCombos { get; set; } = new();
        public List<ComboSalesDto> SlowSellingCombos { get; set; } = new();
        public List<DailyComboRevenueDto> DailyComboRevenueChart { get; set; } = new();
    }

    public class ComboSalesDto
    {
        public int ComboId { get; set; }
        public string Name { get; set; } = null!;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DailyComboRevenueDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int QuantitySold { get; set; }
    }

    public class DashboardAlertDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Subtitle { get; set; }
        public string Detail { get; set; } = null!;
        public string Severity { get; set; } = null!; // "warning", "danger", "info"
        public string Type { get; set; } = null!; // "promotion", "occupancy", "movie"
    }

    public class DashboardKpisDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal RevenueGrowth { get; set; }
        public int TicketsSold { get; set; }
        public decimal TicketGrowth { get; set; }
        public int ActiveMembers { get; set; }
        public decimal MemberGrowth { get; set; }
        public int ActivePromotions { get; set; }
        public double AverageOccupancyRate { get; set; }
        public decimal OccupancyGrowth { get; set; }
        public int TodayShowtimes { get; set; }
        public decimal ShowtimesGrowth { get; set; }
        public int PendingPaymentsCount { get; set; }
        public decimal PendingPaymentsGrowth { get; set; }
        public int ActiveMoviesCount { get; set; }
        public decimal ActiveMoviesGrowth { get; set; }
    }

    public class SeatTypeOccupancyDto
    {
        public string SeatType { get; set; } = null!;
        public int Value { get; set; }
    }

    public class TopCustomerDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int TicketsBought { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class HallRevenueDto
    {
        public int HallId { get; set; }
        public string HallName { get; set; } = null!;
        public string CinemaName { get; set; } = null!;
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }
        public double OccupancyRate { get; set; }
    }

    public class TopShowtimeDto
    {
        public int ShowtimeId { get; set; }
        public string MovieTitle { get; set; } = null!;
        public string HallName { get; set; } = null!;
        public string CinemaName { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public int TicketsSold { get; set; }
        public double OccupancyRate { get; set; }
    }

    public class GoldenHourRevenueDto
    {
        public string HourRange { get; set; } = null!;
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }
    }
}
