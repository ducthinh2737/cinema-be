using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Cinemas;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HallTypesController : ControllerBase
    {
        private readonly ICinemaService _cinemaService;

        public HallTypesController(ICinemaService cinemaService)
        {
            _cinemaService = cinemaService;
        }

        [HttpGet]
        public async Task<IActionResult> GetHallTypes()
        {
            var types = await _cinemaService.GetHallTypesAsync();
            return Ok(types);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetHallTypeById(int id)
        {
            var type = await _cinemaService.GetHallTypeByIdAsync(id);
            if (type == null)
            {
                return NotFound(new { Message = $"Hall Type with ID {id} not found." });
            }
            return Ok(type);
        }

        [HttpPost]
        public async Task<IActionResult> CreateHallType([FromBody] HallTypeCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var type = await _cinemaService.CreateHallTypeAsync(createDto);
            return CreatedAtAction(nameof(GetHallTypeById), new { id = type.HallTypeId }, type);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateHallType(int id, [FromBody] HallTypeUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedType = await _cinemaService.UpdateHallTypeAsync(id, updateDto);
            if (updatedType == null)
            {
                return NotFound(new { Message = $"Hall Type with ID {id} not found." });
            }
            return Ok(updatedType);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteHallType(int id)
        {
            var success = await _cinemaService.DeleteHallTypeAsync(id);
            if (!success)
            {
                return NotFound(new { Message = $"Hall Type with ID {id} not found or already deleted." });
            }
            return Ok(new { Message = "Hall Type has been successfully soft deleted." });
        }
    }
}
