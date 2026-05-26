using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Cinemas;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeatTypesController : ControllerBase
    {
        private readonly ICinemaService _cinemaService;

        public SeatTypesController(ICinemaService cinemaService)
        {
            _cinemaService = cinemaService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSeatTypes()
        {
            var types = await _cinemaService.GetSeatTypesAsync();
            return Ok(types);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSeatTypeById(int id)
        {
            var type = await _cinemaService.GetSeatTypeByIdAsync(id);
            if (type == null)
            {
                return NotFound(new { Message = $"Seat Type with ID {id} not found." });
            }
            return Ok(type);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSeatType([FromBody] SeatTypeCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var type = await _cinemaService.CreateSeatTypeAsync(createDto);
            return CreatedAtAction(nameof(GetSeatTypeById), new { id = type.SeatTypeId }, type);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSeatType(int id, [FromBody] SeatTypeUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedType = await _cinemaService.UpdateSeatTypeAsync(id, updateDto);
            if (updatedType == null)
            {
                return NotFound(new { Message = $"Seat Type with ID {id} not found." });
            }
            return Ok(updatedType);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSeatType(int id)
        {
            var success = await _cinemaService.DeleteSeatTypeAsync(id);
            if (!success)
            {
                return NotFound(new { Message = $"Seat Type with ID {id} not found or already deleted." });
            }
            return Ok(new { Message = "Seat Type has been successfully soft deleted." });
        }
    }
}
