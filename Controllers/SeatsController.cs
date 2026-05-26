using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Cinemas;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeatsController : ControllerBase
    {
        private readonly ICinemaService _cinemaService;

        public SeatsController(ICinemaService cinemaService)
        {
            _cinemaService = cinemaService;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSeatById(int id)
        {
            var seat = await _cinemaService.GetSeatByIdAsync(id);
            if (seat == null)
            {
                return NotFound(new { Message = $"Seat with ID {id} not found." });
            }
            return Ok(seat);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSeat([FromBody] SeatCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var seat = await _cinemaService.CreateSeatAsync(createDto);
            return CreatedAtAction(nameof(GetSeatById), new { id = seat.SeatId }, seat);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSeat(int id, [FromBody] SeatUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedSeat = await _cinemaService.UpdateSeatAsync(id, updateDto);
            if (updatedSeat == null)
            {
                return NotFound(new { Message = $"Seat with ID {id} not found." });
            }
            return Ok(updatedSeat);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSeat(int id)
        {
            var success = await _cinemaService.DeleteSeatAsync(id);
            if (!success)
            {
                return NotFound(new { Message = $"Seat with ID {id} not found or already deleted." });
            }
            return Ok(new { Message = "Seat has been successfully soft deleted." });
        }
    }
}
