using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Users;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyService _loyaltyService;

        public LoyaltyController(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(ApiResponse.Fail<object>("Unauthorized access."));

            var result = await _loyaltyService.GetLoyaltyDashboardAsync(userId.Value, cancellationToken);
            return Ok(result);
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10, 
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(ApiResponse.Fail<object>("Unauthorized access."));

            var result = await _loyaltyService.GetLoyaltyTransactionsAsync(userId.Value, page, pageSize, cancellationToken);
            return Ok(result);
        }

        [HttpPost("validate-redeem")]
        public async Task<IActionResult> ValidateRedeem(
            [FromBody] LoyaltyRedeemValidateRequestDto request, 
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(ApiResponse.Fail<object>("Unauthorized access."));

            var result = await _loyaltyService.ValidateRedeemPointsAsync(userId.Value, request, cancellationToken);
            return Ok(result);
        }

        // Admin Endpoints
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/statistics")]
        public async Task<IActionResult> GetAdminStatistics(CancellationToken cancellationToken)
        {
            var result = await _loyaltyService.GetAdminLoyaltyStatsAsync(cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/transactions")]
        public async Task<IActionResult> GetAllTransactions(
            [FromQuery] string? search, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10, 
            CancellationToken cancellationToken = default)
        {
            var result = await _loyaltyService.GetAllTransactionsAsync(search, page, pageSize, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin/adjust")]
        public async Task<IActionResult> AdjustPoints([FromBody] LoyaltyPointsAdjustDto dto, CancellationToken cancellationToken)
        {
            var result = await _loyaltyService.AdjustPointsManuallyAsync(dto, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        private int? GetUserId()
        {
            var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idString, out int id))
            {
                return id;
            }
            return null;
        }
    }
}
