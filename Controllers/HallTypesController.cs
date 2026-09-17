using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Cinemas;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HallTypesController : ControllerBase
    {
        private readonly ICinemaService _cinemaService;
        private readonly CinemaDbContext _context;

        public HallTypesController(ICinemaService cinemaService, CinemaDbContext context)
        {
            _cinemaService = cinemaService;
            _context = context;
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

        [HttpDelete("{id:int}/hard")]
        public async Task<IActionResult> HardDeleteHallType(int id)
        {
            var hallType = await _context.HallTypes.FindAsync(id);
            if (hallType == null) return NotFound(new { Message = $"Không tìm thấy loại phòng chiếu với ID {id}." });

            var hasHalls = await _context.Halls.AnyAsync(h => h.HallTypeId == id);
            if (hasHalls)
            {
                return BadRequest(new { Message = "Không thể xóa vật lý loại phòng này vì vẫn còn phòng chiếu liên kết với nó." });
            }

            _context.HallTypes.Remove(hallType);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Loại phòng chiếu đã được xóa vật lý khỏi cơ sở dữ liệu." });
        }
    }
}
