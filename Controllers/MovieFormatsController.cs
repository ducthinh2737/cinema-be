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
    public class MovieFormatsController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public MovieFormatsController(CinemaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetFormats()
        {
            var formats = await _context.MovieFormats.ToListAsync();
            return Ok(formats);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetFormatById(int id)
        {
            var format = await _context.MovieFormats.FindAsync(id);
            if (format == null) return NotFound(new { Message = $"Format with ID {id} not found." });
            return Ok(format);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFormat([FromBody] FormatCreateDto dto)
        {
            var format = new MovieFormat
            {
                FormatName = dto.FormatName,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            _context.MovieFormats.Add(format);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFormatById), new { id = format.MovieFormatId }, format);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateFormat(int id, [FromBody] FormatUpdateDto dto)
        {
            var format = await _context.MovieFormats.FindAsync(id);
            if (format == null) return NotFound(new { Message = $"Format with ID {id} not found." });
            
            format.FormatName = dto.FormatName;
            format.Description = dto.Description;
            format.IsDeleted = dto.IsDeleted;
            format.LastModifiedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return Ok(format);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteFormat(int id)
        {
            var format = await _context.MovieFormats.Include(f => f.Movies).FirstOrDefaultAsync(f => f.MovieFormatId == id);
            if (format == null) return NotFound(new { Message = $"Format with ID {id} not found." });

            var hasMovies = format.Movies.Any();
            if (hasMovies)
            {
                return BadRequest(new { Message = "Không thể xóa vật lý định dạng này vì vẫn còn phim liên kết với nó." });
            }
            
            _context.MovieFormats.Remove(format);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Format deleted successfully." });
        }

        [HttpDelete("{id:int}/hard")]
        public async Task<IActionResult> HardDeleteFormat(int id)
        {
            return await DeleteFormat(id);
        }
    }

    public class FormatCreateDto
    {
        public string FormatName { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class FormatUpdateDto
    {
        public string FormatName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
    }
}
