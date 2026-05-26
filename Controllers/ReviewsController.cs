using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Reviews;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpGet("movie/{movieId:int}")]
        public async Task<IActionResult> GetReviewsByMovie(int movieId, [FromQuery] ReviewQueryParameters queryParams)
        {
            var result = await _reviewService.GetPagedReviewsByMovieIdAsync(movieId, queryParams);
            return Ok(result);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetReviewsByUser(int userId, [FromQuery] ReviewQueryParameters queryParams)
        {
            var result = await _reviewService.GetPagedReviewsByUserIdAsync(userId, queryParams);
            return Ok(result);
        }

        [HttpGet("movie/{movieId:int}/average-rating")]
        public async Task<IActionResult> GetAverageRating(int movieId)
        {
            var average = await _reviewService.GetAverageRatingAsync(movieId);
            return Ok(new { MovieId = movieId, AverageRating = average });
        }

        [HttpGet("top-rated")]
        public async Task<IActionResult> GetTopRatedMovies([FromQuery] int limit = 10)
        {
            var result = await _reviewService.GetTopRatedMoviesAsync(limit);
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] ReviewCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                var result = await _reviewService.CreateReviewAsync(userId, createDto);
                return CreatedAtAction(nameof(GetReviewsByMovie), new { movieId = result.MovieId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] ReviewUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                var result = await _reviewService.UpdateReviewAsync(id, userId, updateDto);
                if (result == null)
                {
                    return NotFound(new { Message = $"Review with ID {id} not found." });
                }
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var role = GetCurrentUserRole();
                var success = await _reviewService.DeleteReviewAsync(id, userId, role);
                if (!success)
                {
                    return NotFound(new { Message = $"Review with ID {id} not found or already deleted." });
                }
                return Ok(new { Message = "Review has been successfully deleted." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("{id:int}/like")]
        public async Task<IActionResult> LikeReview(int id)
        {
            var result = await _reviewService.LikeReviewAsync(id);
            if (result == null)
            {
                return NotFound(new { Message = $"Review with ID {id} not found." });
            }
            return Ok(result);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("User ID is missing or invalid in JWT token.");
            }
            return userId;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }
    }
}
