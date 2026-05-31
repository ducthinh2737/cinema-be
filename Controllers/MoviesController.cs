using System;
using System.IO;
using System.Linq;
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
            try
            {
                var result = await _movieService.GetMovieByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail<MovieDetailDto>(ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail<MovieDetailDto>(ex.Message));
            }
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetMovieBySlug(string slug)
        {
            try
            {
                var result = await _movieService.GetMovieBySlugAsync(slug);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail<MovieDetailDto>(ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail<MovieDetailDto>(ex.Message));
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateMovie([FromBody] MovieCreateDto createDto)
        {
            try
            {
                var result = await _movieService.CreateMovieAsync(createDto);
                return CreatedAtAction(nameof(GetMovieById), new { id = result.Data.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse.Fail<MovieDetailDto>(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse.Fail<MovieDetailDto>(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail<MovieDetailDto>(ex.Message));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateMovie(int id, [FromBody] MovieUpdateDto updateDto)
        {
            try
            {
                var result = await _movieService.UpdateMovieAsync(id, updateDto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail<MovieDetailDto>(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse.Fail<MovieDetailDto>(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse.Fail<MovieDetailDto>(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail<MovieDetailDto>(ex.Message));
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            try
            {
                var result = await _movieService.DeleteMovieAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail<bool>(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail<bool>(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail<bool>(ex.Message));
            }
        }

        [HttpPost("{id:int}/restore")]
        public async Task<IActionResult> RestoreMovie(int id)
        {
            try
            {
                var result = await _movieService.RestoreMovieAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail<bool>(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail<bool>(ex.Message));
            }
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
                return BadRequest(ApiResponse.Fail<string>("No file uploaded."));
            }

            if (file.Length > MaxFileBytes)
            {
                return BadRequest(ApiResponse.Fail<string>("File size exceeds limit of 5 MB."));
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponse.Fail<string>($"Invalid file format. Allowed formats: {string.Join(", ", _allowedExtensions)}"));
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", folderName);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/{folderName}/{fileName}";
            try
            {
                var result = await _movieService.UpdateMoviePhotosAsync(id, isPoster ? fileUrl : null, isPoster ? null : fileUrl);
                return Ok(ApiResponse.Success(new { Url = fileUrl }));
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                return NotFound(ApiResponse.Fail<string>(ex.Message));
            }
        }
    }
}
