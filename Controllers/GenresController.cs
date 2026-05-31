using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Movies;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenresController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public GenresController(CinemaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetGenres()
        {
            var genres = await _context.Genres
                .Select(g => new GenreDto
                {
                    GenreId = g.GenreId,
                    GenreName = g.GenreName,
                    Description = g.Description,
                    IsDeleted = g.IsDeleted,
                    CreatedAt = g.CreatedAt
                })
                .ToListAsync();
            return Ok(genres);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetGenreById(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null) return NotFound(new { Message = $"Genre with ID {id} not found." });
            
            var dto = new GenreDto
            {
                GenreId = genre.GenreId,
                GenreName = genre.GenreName,
                Description = genre.Description,
                IsDeleted = genre.IsDeleted,
                CreatedAt = genre.CreatedAt
            };
            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGenre([FromBody] GenreCreateDto dto)
        {
            var genre = new Genre 
            { 
                GenreName = dto.GenreName,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            _context.Genres.Add(genre);
            await _context.SaveChangesAsync();

            var resultDto = new GenreDto
            {
                GenreId = genre.GenreId,
                GenreName = genre.GenreName,
                Description = genre.Description,
                IsDeleted = genre.IsDeleted,
                CreatedAt = genre.CreatedAt
            };

            return CreatedAtAction(nameof(GetGenreById), new { id = genre.GenreId }, resultDto);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateGenre(int id, [FromBody] GenreUpdateDto dto)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null) return NotFound(new { Message = $"Genre with ID {id} not found." });
            
            genre.GenreName = dto.GenreName;
            genre.Description = dto.Description;
            genre.IsDeleted = dto.IsDeleted;
            genre.LastModifiedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            var resultDto = new GenreDto
            {
                GenreId = genre.GenreId,
                GenreName = genre.GenreName,
                Description = genre.Description,
                IsDeleted = genre.IsDeleted,
                CreatedAt = genre.CreatedAt
            };

            return Ok(resultDto);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null) return NotFound(new { Message = $"Genre with ID {id} not found." });
            
            genre.IsDeleted = true;
            genre.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            return Ok(new { Message = "Genre soft deleted successfully." });
        }
    }

    public class GenreDto
    {
        public int GenreId { get; set; }
        public string GenreName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GenreCreateDto
    {
        public string GenreName { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class GenreUpdateDto
    {
        public string GenreName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
    }
}


