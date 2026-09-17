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
    public class SeatTypesController : ControllerBase
    {
        private readonly ICinemaService _cinemaService;
        private readonly CinemaDbContext _context;

        public SeatTypesController(ICinemaService cinemaService, CinemaDbContext context)
        {
            _cinemaService = cinemaService;
            _context = context;
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

        [HttpDelete("{id:int}/hard")]
        public async Task<IActionResult> HardDeleteSeatType(int id)
        {
            var seatType = await _context.SeatTypes.FindAsync(id);
            if (seatType == null) return NotFound(new { Message = $"Không tìm thấy loại ghế với ID {id}." });

            var hasSeats = await _context.Seats.AnyAsync(s => s.SeatTypeId == id);
            if (hasSeats)
            {
                return BadRequest(new { Message = "Không thể xóa vật lý loại ghế này vì vẫn còn ghế ngồi liên kết với nó." });
            }

            _context.SeatTypes.Remove(seatType);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Loại ghế đã được xóa vật lý khỏi cơ sở dữ liệu." });
        }
    }
}
