using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Admin;

namespace CinemaBooking.API.Services.Interfaces
{
    /// <summary>
    /// Interface for high performance, cached, date-range filtered enterprise dashboard analytics.
    /// </summary>
    public interface IAdminDashboardService
    {
        Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryAsync(DateTime? from = null, DateTime? to = null);
        Task<ApiResponse<RevenueStatsDto>> GetRevenueStatisticsAsync(DateTime? from = null, DateTime? to = null);
        Task<ApiResponse<List<MovieRevenueDto>>> GetTopMoviesAsync(int limit = 5, DateTime? from = null, DateTime? to = null);
        Task<ApiResponse<List<CinemaRevenueDto>>> GetTopCinemasAsync(int limit = 5, DateTime? from = null, DateTime? to = null);
        Task<ApiResponse<AdminStatisticsDto>> GetGeneralStatisticsAsync();
        Task<ApiResponse<List<MonthlyRevenueDto>>> GetRevenueChartAsync(DateTime? from = null, DateTime? to = null);
        Task<ApiResponse<BookingAnalyticsDto>> GetBookingAnalyticsAsync(DateTime? from = null, DateTime? to = null);
        Task<ApiResponse<double>> GetOccupancyRateAsync(DateTime? from = null, DateTime? to = null);
        Task<ApiResponse<DashboardDataDto>> GetUnifiedDashboardDataAsync(DateTime? from = null, DateTime? to = null);
    }
}
