using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Bookings;
using CinemaBooking.API.Services.Interfaces;

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
            if (userId == null) return Unauthorized(new { Message = "Unauthorized access." });

            try
            {
                var booking = await _bookingService.CreateBookingAsync(userId.Value, createDto);
                return CreatedAtAction(nameof(GetBookingById), new { id = booking.BookingId }, booking);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error occurred while creating booking.", Details = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmBooking([FromBody] BookingConfirmDto confirmDto)
        {
            try
            {
                var booking = await _bookingService.ConfirmBookingAsync(confirmDto);
                return Ok(booking);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error occurred while confirming booking.", Details = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelBooking([FromBody] BookingCancelDto cancelDto)
        {
            try
            {
                var success = await _bookingService.CancelBookingAsync(cancelDto.BookingId);
                if (!success)
                {
                    return NotFound(new { Message = $"Booking with ID {cancelDto.BookingId} not found." });
                }
                return Ok(new { Message = "Booking has been successfully cancelled." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error occurred while cancelling booking.", Details = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet("my-bookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { Message = "Unauthorized access." });

            var bookings = await _bookingService.GetUserBookingsAsync(userId.Value);
            return Ok(bookings);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBookingById(int id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null)
            {
                return NotFound(new { Message = $"Booking with ID {id} not found." });
            }

            // Optional security: Ensure user can only view their own bookings unless Admin
            var userId = GetUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (booking.UserId != userId && role != "Admin")
            {
                return Forbid(new Microsoft.AspNetCore.Authentication.AuthenticationProperties(), "You are not authorized to view this booking.");
            }

            return Ok(booking);
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
