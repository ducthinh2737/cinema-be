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
    public class LanguagesController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public LanguagesController(CinemaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetLanguages()
        {
            var languages = await _context.Languages.ToListAsync();
            return Ok(languages);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLanguageById(int id)
        {
            var language = await _context.Languages.FindAsync(id);
            if (language == null) return NotFound(new { Message = $"Language with ID {id} not found." });
            return Ok(language);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLanguage([FromBody] LanguageCreateDto dto)
        {
            var language = new Language
            {
                LanguageName = dto.LanguageName,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            _context.Languages.Add(language);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetLanguageById), new { id = language.LanguageId }, language);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateLanguage(int id, [FromBody] LanguageUpdateDto dto)
        {
            var language = await _context.Languages.FindAsync(id);
            if (language == null) return NotFound(new { Message = $"Language with ID {id} not found." });
            
            language.LanguageName = dto.LanguageName;
            language.Description = dto.Description;
            language.IsDeleted = dto.IsDeleted;
            language.LastModifiedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return Ok(language);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteLanguage(int id)
        {
            var language = await _context.Languages.FindAsync(id);
            if (language == null) return NotFound(new { Message = $"Language with ID {id} not found." });
            
            _context.Languages.Remove(language);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Language deleted successfully." });
        }
    }

    public class LanguageCreateDto
    {
        public string LanguageName { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class LanguageUpdateDto
    {
        public string LanguageName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
    }
}
