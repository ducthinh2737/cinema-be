using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class PricingController : ControllerBase
    {
        private readonly IPricingService _pricingService;

        public PricingController(IPricingService pricingService)
        {
            _pricingService = pricingService;
        }

        [HttpGet("pricing-rules")]
        public async Task<IActionResult> GetRules(CancellationToken cancellationToken)
        {
            var result = await _pricingService.GetAllRulesAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet("pricing-rules/{id:int}")]
        public async Task<IActionResult> GetRuleById(int id, CancellationToken cancellationToken)
        {
            var result = await _pricingService.GetRuleByIdAsync(id, cancellationToken);
            return Ok(result);
        }

        [HttpPost("pricing-rules")]
        public async Task<IActionResult> CreateRule([FromBody] PricingRuleCreateUpdateDto dto, CancellationToken cancellationToken)
        {
            var result = await _pricingService.CreateRuleAsync(dto, cancellationToken);
            return Ok(result);
        }

        [HttpPut("pricing-rules/{id:int}")]
        public async Task<IActionResult> UpdateRule(int id, [FromBody] PricingRuleCreateUpdateDto dto, CancellationToken cancellationToken)
        {
            var result = await _pricingService.UpdateRuleAsync(id, dto, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("pricing-rules/{id:int}")]
        public async Task<IActionResult> DeleteRule(int id, CancellationToken cancellationToken)
        {
            var result = await _pricingService.DeleteRuleAsync(id, cancellationToken);
            return Ok(result);
        }

        [HttpPost("pricing/compute")]
        public async Task<IActionResult> ComputePricePost([FromBody] PricingComputeRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _pricingService.ComputePriceAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet("pricing/compute")]
        public async Task<IActionResult> ComputePriceGet([FromQuery] string seatType, [FromQuery] int hallId, [FromQuery] int movieId, [FromQuery] int showtimeId, CancellationToken cancellationToken)
        {
            var request = new PricingComputeRequestDto
            {
                SeatType = seatType,
                HallId = hallId,
                MovieId = movieId,
                ShowtimeId = showtimeId
            };
            var result = await _pricingService.ComputePriceAsync(request, cancellationToken);
            return Ok(result);
        }
    }
}
