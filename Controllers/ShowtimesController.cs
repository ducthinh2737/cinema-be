using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShowtimesController : ControllerBase
    {
        private readonly IShowtimeService _showtimeService;

        public ShowtimesController(IShowtimeService showtimeService)
        {
            _showtimeService = showtimeService;
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
            var showtime = await _showtimeService.GetShowtimeByIdAsync(id);
            if (showtime == null)
            {
                return NotFound(new { Message = $"Showtime with ID {id} not found." });
            }
            return Ok(showtime);
        }

        [HttpGet("movie/{movieId:int}")]
        public async Task<IActionResult> GetShowtimesByMovieId(int movieId)
        {
            var showtimes = await _showtimeService.GetShowtimesByMovieIdAsync(movieId);
            return Ok(showtimes);
        }

        [HttpGet("cinema/{cinemaId:int}")]
        public async Task<IActionResult> GetShowtimesByCinemaId(int cinemaId)
        {
            var showtimes = await _showtimeService.GetShowtimesByCinemaIdAsync(cinemaId);
            return Ok(showtimes);
        }

        [HttpPost]
        public async Task<IActionResult> CreateShowtime([FromBody] ShowtimeCreateDto createDto)
        {
            try
            {
                var showtime = await _showtimeService.CreateShowtimeAsync(createDto);
                return CreatedAtAction(nameof(GetShowtimeById), new { id = showtime.ShowtimeId }, showtime);
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
                return StatusCode(500, new { Message = "An error occurred while creating showtime.", Details = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateShowtime(int id, [FromBody] ShowtimeUpdateDto updateDto)
        {
            try
            {
                var showtime = await _showtimeService.UpdateShowtimeAsync(id, updateDto);
                if (showtime == null)
                {
                    return NotFound(new { Message = $"Showtime with ID {id} not found." });
                }
                return Ok(showtime);
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
                return StatusCode(500, new { Message = "An error occurred while updating showtime.", Details = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteShowtime(int id)
        {
            var success = await _showtimeService.DeleteShowtimeAsync(id);
            if (!success)
            {
                return NotFound(new { Message = $"Showtime with ID {id} not found." });
            }
            return Ok(new { Message = "Showtime has been successfully deleted." });
        }
    }
}
