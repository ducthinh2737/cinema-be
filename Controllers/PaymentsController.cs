using System;
using System.Collections.Generic;
using System.Linq;
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

        [Authorize]
        [HttpPost("vnpay/create")]
        public async Task<IActionResult> CreateVNPayPayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var paymentUrl = await _paymentService.CreateVNPayPaymentUrlAsync(request.BookingId, ipAddress);
                return Ok(new { PaymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("momo/create")]
        public async Task<IActionResult> CreateMomoPayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var paymentUrl = await _paymentService.CreateMomoPaymentUrlAsync(request.BookingId);
                return Ok(new { PaymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("zalopay/create")]
        public async Task<IActionResult> CreateZaloPayPayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var paymentUrl = await _paymentService.CreateZaloPayPaymentUrlAsync(request.BookingId);
                return Ok(new { PaymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("paypal/create")]
        public async Task<IActionResult> CreatePayPalPayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var paymentUrl = await _paymentService.CreatePayPalPaymentUrlAsync(request.BookingId);
                return Ok(new { PaymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // Unified callback endpoint supporting VNPay / Momo redirections (GET)
        [HttpGet("callback")]
        public async Task<IActionResult> HandlePaymentCallbackGet([FromQuery] string gateway)
        {
            try
            {
                var queryData = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
                var success = await _paymentService.ProcessCallbackAsync(gateway, queryData);
                if (success)
                {
                    return Ok(new { Message = $"Payment processed successfully via {gateway}.", Success = true });
                }
                return BadRequest(new { Message = $"Payment failed or signature invalid for {gateway}.", Success = false });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error processing callback", Details = ex.Message });
            }
        }

        // Unified callback webhook IPN endpoint supporting POST payloads
        [HttpPost("callback")]
        public async Task<IActionResult> HandlePaymentCallbackPost([FromQuery] string gateway)
        {
            try
            {
                Dictionary<string, string> callbackData;

                if (Request.HasFormContentType)
                {
                    callbackData = Request.Form.ToDictionary(f => f.Key, f => f.Value.ToString());
                }
                else
                {
                    // Fallback to query params or JSON body if POST doesn't use Form URL Encoding
                    callbackData = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
                }

                var success = await _paymentService.ProcessCallbackAsync(gateway, callbackData);
                if (success)
                {
                    return Ok(new { Message = "IPN parsed successfully.", Success = true });
                }
                return BadRequest(new { Message = "IPN verification failed.", Success = false });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error processing callback IPN", Details = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("refund")]
        public async Task<IActionResult> ProcessRefund([FromBody] RefundPaymentRequest request)
        {
            try
            {
                var result = await _paymentService.RefundPaymentAsync(request.PaymentId, request.Reason, request.Amount);
                if (result)
                {
                    return Ok(new { Message = "Refund processed successfully." });
                }
                return BadRequest(new { Message = "Refund processing failed." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("retry")]
        public async Task<IActionResult> RetryPayment([FromBody] RetryPaymentRequest request)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var paymentUrl = await _paymentService.RetryPaymentUrlAsync(request.BookingId, request.GatewayName, ipAddress);
                return Ok(new { PaymentUrl = paymentUrl });
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

    public class RefundPaymentRequest
    {
        public int PaymentId { get; set; }
        public string Reason { get; set; } = null!;
        public decimal Amount { get; set; }
    }

    public class RetryPaymentRequest
    {
        public int BookingId { get; set; }
        public string GatewayName { get; set; } = null!;
    }
}
