using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminDashboardService _dashboardService;

        public AdminController(IAdminDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _dashboardService.GetDashboardSummaryAsync(from, to);
            return Ok(result);
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueStatistics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _dashboardService.GetRevenueStatisticsAsync(from, to);
            return Ok(result);
        }

        [HttpGet("top-movies")]
        public async Task<IActionResult> GetTopMovies([FromQuery] int limit = 5, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var result = await _dashboardService.GetTopMoviesAsync(limit, from, to);
            return Ok(result);
        }

        [HttpGet("top-cinemas")]
        public async Task<IActionResult> GetTopCinemas([FromQuery] int limit = 5, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var result = await _dashboardService.GetTopCinemasAsync(limit, from, to);
            return Ok(result);
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetGeneralStatistics()
        {
            var result = await _dashboardService.GetGeneralStatisticsAsync();
            return Ok(result);
        }

        [HttpGet("revenue-chart")]
        public async Task<IActionResult> GetRevenueChart([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _dashboardService.GetRevenueChartAsync(from, to);
            return Ok(result);
        }

        [HttpGet("bookings-analytics")]
        public async Task<IActionResult> GetBookingAnalytics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _dashboardService.GetBookingAnalyticsAsync(from, to);
            return Ok(result);
        }

        [HttpGet("occupancy-rate")]
        public async Task<IActionResult> GetOccupancyRate([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _dashboardService.GetOccupancyRateAsync(from, to);
            return Ok(result);
        }
    }
}
