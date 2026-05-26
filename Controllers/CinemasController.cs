using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using CinemaBooking.API.DTOs.Cinemas;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CinemasController : ControllerBase
    {
        private readonly ICinemaService _cinemaService;
        private readonly IWebHostEnvironment _env;

        private const long MaxFileBytes = 5 * 1024 * 1024; // 5 MB
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public CinemasController(ICinemaService cinemaService, IWebHostEnvironment env)
        {
            _cinemaService = cinemaService;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetCinemas([FromQuery] CinemaQueryParameters queryParams)
        {
            var result = await _cinemaService.GetPagedCinemasAsync(queryParams);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCinemaById(int id)
        {
            var cinema = await _cinemaService.GetCinemaByIdAsync(id);
            if (cinema == null)
            {
                return NotFound(new { Message = $"Cinema with ID {id} not found." });
            }
            return Ok(cinema);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCinema([FromBody] CinemaCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cinema = await _cinemaService.CreateCinemaAsync(createDto);
            return CreatedAtAction(nameof(GetCinemaById), new { id = cinema.CinemaId }, cinema);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCinema(int id, [FromBody] CinemaUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedCinema = await _cinemaService.UpdateCinemaAsync(id, updateDto);
            if (updatedCinema == null)
            {
                return NotFound(new { Message = $"Cinema with ID {id} not found." });
            }
            return Ok(updatedCinema);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCinema(int id)
        {
            var success = await _cinemaService.DeleteCinemaAsync(id);
            if (!success)
            {
                return NotFound(new { Message = $"Cinema with ID {id} not found or already deleted." });
            }
            return Ok(new { Message = "Cinema has been successfully soft deleted." });
        }

        [HttpPost("{id:int}/upload-image")]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
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

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "cinemas");
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

            var relativeUrl = $"/uploads/cinemas/{fileName}";
            var updatedCinema = await _cinemaService.UpdateCinemaImageAsync(id, relativeUrl);
            if (updatedCinema == null)
            {
                return NotFound(new { Message = $"Cinema with ID {id} not found." });
            }

            return Ok(updatedCinema);
        }

        [HttpGet("{cinemaId:int}/halls")]
        public async Task<IActionResult> GetCinemaHalls(int cinemaId)
        {
            var halls = await _cinemaService.GetHallsByCinemaIdAsync(cinemaId);
            return Ok(halls);
        }
    }
}
