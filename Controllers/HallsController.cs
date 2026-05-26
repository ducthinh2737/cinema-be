using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Cinemas;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HallsController : ControllerBase
    {
        private readonly ICinemaService _cinemaService;

        public HallsController(ICinemaService cinemaService)
        {
            _cinemaService = cinemaService;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetHallById(int id)
        {
            var hall = await _cinemaService.GetHallByIdAsync(id);
            if (hall == null)
            {
                return NotFound(new { Message = $"Hall with ID {id} not found." });
            }
            return Ok(hall);
        }

        [HttpPost]
        public async Task<IActionResult> CreateHall([FromBody] HallCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var hall = await _cinemaService.CreateHallAsync(createDto);
            return CreatedAtAction(nameof(GetHallById), new { id = hall.HallId }, hall);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateHall(int id, [FromBody] HallUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedHall = await _cinemaService.UpdateHallAsync(id, updateDto);
            if (updatedHall == null)
            {
                return NotFound(new { Message = $"Hall with ID {id} not found." });
            }
            return Ok(updatedHall);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteHall(int id)
        {
            var success = await _cinemaService.DeleteHallAsync(id);
            if (!success)
            {
                return NotFound(new { Message = $"Hall with ID {id} not found or already deleted." });
            }
            return Ok(new { Message = "Hall has been successfully soft deleted." });
        }

        [HttpGet("{hallId:int}/seats")]
        public async Task<IActionResult> GetHallSeats(int hallId)
        {
            var seats = await _cinemaService.GetSeatsByHallIdAsync(hallId);
            return Ok(seats);
        }
    }
}
