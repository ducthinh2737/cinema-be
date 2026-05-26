using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Admin;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardDto> GetDashboardSummaryAsync();
        Task<RevenueStatsDto> GetRevenueStatisticsAsync();
        Task<List<MovieRevenueDto>> GetTopMoviesAsync(int limit = 5);
        Task<List<CinemaRevenueDto>> GetTopCinemasAsync(int limit = 5);
        Task<AdminStatisticsDto> GetGeneralStatisticsAsync();
    }
}
