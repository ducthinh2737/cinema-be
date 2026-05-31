using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShowtimesController : ControllerBase
    {
        private readonly IShowtimeService _showtimeService;
        private readonly ISeatLockService _seatLockService;
        private readonly CinemaDbContext _context;
        private readonly IMapper _mapper;

        public ShowtimesController(
            IShowtimeService showtimeService,
            ISeatLockService seatLockService,
            CinemaDbContext context,
            IMapper mapper)
        {
            _showtimeService = showtimeService;
            _seatLockService = seatLockService;
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetShowtimes([FromQuery] ShowtimeQueryParameters queryParams)
        {
            var result = await _showtimeService.GetPagedShowtimesAsync(queryParams);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetShowtimeById(int id)
        {
            try
            {
                var result = await _showtimeService.GetShowtimeByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail<ShowtimeDto>(ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail<ShowtimeDto>(ex.Message));
            }
        }

        [HttpGet("movie/{movieId:int}")]
        public async Task<IActionResult> GetShowtimesByMovieId(int movieId)
        {
            var result = await _showtimeService.GetShowtimesByMovieIdAsync(movieId);
            return Ok(result);
        }

        [HttpGet("cinema/{cinemaId:int}")]
        public async Task<IActionResult> GetShowtimesByCinemaId(int cinemaId)
        {
            var result = await _showtimeService.GetShowtimesByCinemaIdAsync(cinemaId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateShowtime([FromBody] ShowtimeCreateDto createDto)
        {
            try
            {
                var result = await _showtimeService.CreateShowtimeAsync(createDto);
                return CreatedAtAction(nameof(GetShowtimeById), new { id = result.Data.ShowtimeId }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse.Fail<ShowtimeDto>(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse.Fail<ShowtimeDto>(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail<ShowtimeDto>(ex.Message));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateShowtime(int id, [FromBody] ShowtimeUpdateDto updateDto)
        {
            try
            {
                var result = await _showtimeService.UpdateShowtimeAsync(id, updateDto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail<ShowtimeDto>(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse.Fail<ShowtimeDto>(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse.Fail<ShowtimeDto>(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail<ShowtimeDto>(ex.Message));
            }
        }

        [HttpGet("{id:int}/seats")]
        public async Task<IActionResult> GetShowtimeSeats(int id)
        {
            try
            {
                var showtime = await _context.Showtimes.FirstOrDefaultAsync(s => s.ShowtimeId == id);
                if (showtime == null)
                {
                    return NotFound(ApiResponse.Fail<List<object>>($"Không tìm thấy suất chiếu ID {id}."));
                }

                var seats = await _context.Seats
                    .Include(s => s.SeatType)
                    .Where(s => s.HallId == showtime.HallId && !s.IsDeleted)
                    .ToListAsync();

                var bookedSeatIds = await _context.BookingSeats
                    .Where(bs => bs.Booking.ShowtimeId == id && bs.Booking.BookingStatus != "Cancelled")
                    .Select(bs => bs.SeatId)
                    .ToListAsync();

                var lockedSeatIds = await _seatLockService.GetLockedSeatsAsync(id);

                var result = new List<object>();
                foreach (var seat in seats)
                {
                    string rowName = seat.SeatCode.Length > 0 ? seat.SeatCode.Substring(0, 1) : "";
                    int seatNumber = 0;
                    if (seat.SeatCode.Length > 1)
                    {
                        int.TryParse(seat.SeatCode.Substring(1), out seatNumber);
                    }

                    string status = "Available";
                    if (bookedSeatIds.Contains(seat.SeatId))
                    {
                        status = "Booked";
                    }
                    else if (lockedSeatIds.Contains(seat.SeatId))
                    {
                        status = "Locked";
                    }

                    string? lockedBy = null;
                    string? lockedBySession = null;
                    if (status == "Locked")
                    {
                        var lockInfo = await _seatLockService.GetSeatLockInfoAsync(id, seat.SeatId);
                        if (lockInfo != null)
                        {
                            lockedBy = lockInfo.UserId;
                            lockedBySession = lockInfo.SessionId;
                        }
                    }

                    result.Add(new
                    {
                        seatId = seat.SeatId,
                        hallId = seat.HallId,
                        rowName = rowName,
                        seatNumber = seatNumber,
                        seatTypeName = seat.SeatType?.TypeName ?? "Standard",
                        status = status,
                        lockedBy = lockedBy,
                        lockedBySession = lockedBySession
                    });
                }

                return Ok(ApiResponse.Success(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail<List<object>>(ex.Message));
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteShowtime(int id)
        {
            try
            {
                var result = await _showtimeService.DeleteShowtimeAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail<bool>(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail<bool>(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail<bool>(ex.Message));
            }
        }
    }
}
