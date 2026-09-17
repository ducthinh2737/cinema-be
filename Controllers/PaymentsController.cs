using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.Services.Interfaces;

using Microsoft.Extensions.Configuration;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;

        public PaymentsController(IPaymentService paymentService, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _configuration = configuration;
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
        /// Endpoint for simulating successful payment (bypasses SePay limits / test environment).
        /// </summary>
        [Authorize]
        [HttpPost("simulate-success")]
        public async Task<IActionResult> SimulatePaymentSuccess([FromBody] ConfirmPaymentDto request)
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

        /// <summary>
        /// Webhook integration for SePay auto banking transaction notification.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("sepay-webhook")]
        public async Task<IActionResult> SePayWebhook([FromBody] SePayWebhookDto request)
        {
            try
            {
                // Optional security token check
                var expectedKey = _configuration["SePay:ApiKey"];
                if (!string.IsNullOrEmpty(expectedKey))
                {
                    var authHeader = Request.Headers["Authorization"].ToString();
                    var token = authHeader.Replace("Apikey ", "", StringComparison.OrdinalIgnoreCase).Trim();
                    if (token != expectedKey.Trim())
                    {
                        return Unauthorized(new { Message = "Bảo mật webhook không hợp lệ." });
                    }
                }

                var result = await _paymentService.ProcessSePayWebhookAsync(request);
                return Ok(new { status = "success", message = "Xác nhận giao dịch thành công.", data = result });
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
