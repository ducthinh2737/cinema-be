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
    public class AgeRatingsController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public AgeRatingsController(CinemaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAgeRatings()
        {
            var ageRatings = await _context.AgeRatings.ToListAsync();
            return Ok(ageRatings);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAgeRatingById(int id)
        {
            var ageRating = await _context.AgeRatings.FindAsync(id);
            if (ageRating == null) return NotFound(new { Message = $"AgeRating with ID {id} not found." });
            return Ok(ageRating);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAgeRating([FromBody] AgeRatingCreateDto dto)
        {
            var ageRating = new AgeRating
            {
                RatingCode = dto.RatingCode,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            _context.AgeRatings.Add(ageRating);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAgeRatingById), new { id = ageRating.AgeRatingId }, ageRating);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAgeRating(int id, [FromBody] AgeRatingUpdateDto dto)
        {
            var ageRating = await _context.AgeRatings.FindAsync(id);
            if (ageRating == null) return NotFound(new { Message = $"AgeRating with ID {id} not found." });
            
            ageRating.RatingCode = dto.RatingCode;
            ageRating.Description = dto.Description;
            ageRating.IsDeleted = dto.IsDeleted;
            ageRating.LastModifiedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return Ok(ageRating);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAgeRating(int id)
        {
            var ageRating = await _context.AgeRatings.FindAsync(id);
            if (ageRating == null) return NotFound(new { Message = $"AgeRating with ID {id} not found." });
            
            _context.AgeRatings.Remove(ageRating);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "AgeRating deleted successfully." });
        }
    }

    public class AgeRatingCreateDto
    {
        public string RatingCode { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class AgeRatingUpdateDto
    {
        public string RatingCode { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
    }
}
