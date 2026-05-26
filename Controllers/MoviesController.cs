using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using CinemaBooking.API.DTOs.Movies;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly IWebHostEnvironment _env;

        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileBytes = 5 * 1024 * 1024; // 5 MB

        public MoviesController(IMovieService movieService, IWebHostEnvironment env)
        {
            _movieService = movieService;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetMovies([FromQuery] MovieQueryParameters queryParams)
        {
            var result = await _movieService.GetPagedMoviesAsync(queryParams);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetMovieById(int id)
        {
            var movie = await _movieService.GetMovieByIdAsync(id);
            if (movie == null)
            {
                return NotFound(new { Message = $"Movie with ID {id} not found." });
            }
            return Ok(movie);
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetMovieBySlug(string slug)
        {
            var movie = await _movieService.GetMovieBySlugAsync(slug);
            if (movie == null)
            {
                return NotFound(new { Message = $"Movie with slug '{slug}' not found." });
            }
            return Ok(movie);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMovie([FromBody] MovieCreateDto createDto)
        {
            try
            {
                var movie = await _movieService.CreateMovieAsync(createDto);
                return CreatedAtAction(nameof(GetMovieById), new { id = movie.Id }, movie);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateMovie(int id, [FromBody] MovieUpdateDto updateDto)
        {
            try
            {
                var movie = await _movieService.UpdateMovieAsync(id, updateDto);
                if (movie == null)
                {
                    return NotFound(new { Message = $"Movie with ID {id} not found." });
                }
                return Ok(movie);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "Admin";
            var success = await _movieService.DeleteMovieAsync(id, deletedBy);
            if (!success)
            {
                return NotFound(new { Message = $"Movie with ID {id} not found or already deleted." });
            }
            return Ok(new { Message = "Movie has been successfully soft deleted." });
        }

        [HttpPost("{id:int}/upload-poster")]
        public async Task<IActionResult> UploadPoster(int id, IFormFile file)
        {
            return await HandleFileUpload(id, file, "posters", true);
        }

        [HttpPost("{id:int}/upload-banner")]
        public async Task<IActionResult> UploadBanner(int id, IFormFile file)
        {
            return await HandleFileUpload(id, file, "banners", false);
        }

        private async Task<IActionResult> HandleFileUpload(int id, IFormFile file, string folderName, bool isPoster)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "No file uploaded." });
            }

            if (file.Length > MaxFileBytes)
            {
                return BadRequest(new { Message = "File size exceeds limit of 5 MB." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return BadRequest(new { Message = $"Invalid file format. Allowed formats: {string.Join(", ", _allowedExtensions)}" });
            }

            // Create directories if they do not exist
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", folderName);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update in database (relative URL)
            var fileUrl = $"/uploads/{folderName}/{fileName}";
            var success = await _movieService.UpdateMoviePhotosAsync(id, isPoster ? fileUrl : null, isPoster ? null : fileUrl);

            if (!success)
            {
                // Clean up file if db save failed
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                return NotFound(new { Message = $"Movie with ID {id} not found." });
            }

            return Ok(new { Url = fileUrl });
        }
    }
}
