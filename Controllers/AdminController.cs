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
        public async Task<IActionResult> GetDashboardSummary()
        {
            var summary = await _dashboardService.GetDashboardSummaryAsync();
            return Ok(summary);
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueStatistics()
        {
            var revenue = await _dashboardService.GetRevenueStatisticsAsync();
            return Ok(revenue);
        }

        [HttpGet("top-movies")]
        public async Task<IActionResult> GetTopMovies([FromQuery] int limit = 5)
        {
            var topMovies = await _dashboardService.GetTopMoviesAsync(limit);
            return Ok(topMovies);
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetGeneralStatistics()
        {
            var statistics = await _dashboardService.GetGeneralStatisticsAsync();
            return Ok(statistics);
        }
    }
}
