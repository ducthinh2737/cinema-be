using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Bookings;
using CinemaBooking.API.Services.Interfaces;
using CinemaBooking.API.Services.Implementations;

namespace CinemaBooking.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking([FromBody] BookingCreateDto createDto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(ApiResponse.Fail<object>("Unauthorized access."));

            var result = await _bookingService.CreateBookingAsync(userId.Value, createDto);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return CreatedAtAction(nameof(GetBookingById), new { id = result.Data.BookingId }, result);
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmBooking([FromBody] BookingConfirmDto confirmDto)
        {
            var result = await _bookingService.ConfirmBookingAsync(confirmDto);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelBooking([FromBody] BookingCancelDto cancelDto)
        {
            var result = await _bookingService.CancelBookingAsync(cancelDto.BookingId);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllBookings()
        {
            var result = await _bookingService.GetAllBookingsAsync();
            return Ok(result);
        }

        [HttpGet("my-bookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(ApiResponse.Fail<object>("Unauthorized access."));

            var result = await _bookingService.GetUserBookingsAsync(userId.Value);
            return Ok(result);
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBookingById(int id)
        {
            var result = await _bookingService.GetBookingByIdAsync(id);
            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            var userId = GetUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (result.Data.UserId != userId && role != "Admin")
            {
                return Forbid();
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
