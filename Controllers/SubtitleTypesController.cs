using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Movies;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubtitleTypesController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public SubtitleTypesController(CinemaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetSubtitleTypes()
        {
            var subtitleTypes = await _context.SubtitleTypes.ToListAsync();
            return Ok(subtitleTypes);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSubtitleTypeById(int id)
        {
            var subtitleType = await _context.SubtitleTypes.FindAsync(id);
            if (subtitleType == null) return NotFound(new { Message = $"SubtitleType with ID {id} not found." });
            return Ok(subtitleType);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubtitleType([FromBody] SubtitleTypeCreateDto dto)
        {
            var subtitleType = new SubtitleType
            {
                SubtitleTypeName = dto.SubtitleTypeName,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            _context.SubtitleTypes.Add(subtitleType);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSubtitleTypeById), new { id = subtitleType.SubtitleTypeId }, subtitleType);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSubtitleType(int id, [FromBody] SubtitleTypeUpdateDto dto)
        {
            var subtitleType = await _context.SubtitleTypes.FindAsync(id);
            if (subtitleType == null) return NotFound(new { Message = $"SubtitleType with ID {id} not found." });
            
            subtitleType.SubtitleTypeName = dto.SubtitleTypeName;
            subtitleType.Description = dto.Description;
            subtitleType.IsDeleted = dto.IsDeleted;
            subtitleType.LastModifiedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return Ok(subtitleType);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSubtitleType(int id)
        {
            var subtitleType = await _context.SubtitleTypes.FindAsync(id);
            if (subtitleType == null) return NotFound(new { Message = $"SubtitleType with ID {id} not found." });
            
            _context.SubtitleTypes.Remove(subtitleType);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "SubtitleType deleted successfully." });
        }

        [HttpDelete("{id:int}/hard")]
        public async Task<IActionResult> HardDeleteSubtitleType(int id)
        {
            return await DeleteSubtitleType(id);
        }
    }

    public class SubtitleTypeCreateDto
    {
        public string SubtitleTypeName { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class SubtitleTypeUpdateDto
    {
        public string SubtitleTypeName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
    }
}
