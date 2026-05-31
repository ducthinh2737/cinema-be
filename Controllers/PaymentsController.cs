using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Exposes the VietQR creation endpoint for general customers.
        /// </summary>
        [Authorize]
        [HttpPost("vietqr/create")]
        public async Task<IActionResult> CreateVietQRPayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var result = await _paymentService.CreateVietQRPaymentAsync(request.BookingId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Admin manual verification and approval endpoint for VietQR transfers.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentDto request)
        {
            try
            {
                var result = await _paymentService.ConfirmPaymentAsync(request.BookingId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Exposes retry payment url endpoints. Backward compatible.
        /// </summary>
        [Authorize]
        [HttpPost("retry")]
        public async Task<IActionResult> RetryPayment([FromBody] RetryPaymentRequest request)
        {
            try
            {
                var result = await _paymentService.CreateVietQRPaymentAsync(request.BookingId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }

    public class CreatePaymentRequest
    {
        public int BookingId { get; set; }
    }

    public class RetryPaymentRequest
    {
        public int BookingId { get; set; }
        public string? GatewayName { get; set; }
    }
}
