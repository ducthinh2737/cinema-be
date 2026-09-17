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

        [HttpGet("movie/{movieId:int}/summary")]
        public async Task<IActionResult> GetMovieRatingSummary(int movieId)
        {
            var summary = await _reviewService.GetMovieRatingSummaryAsync(movieId);
            return Ok(summary);
        }

        [HttpGet("top-rated")]
        public async Task<IActionResult> GetTopRatedMovies([FromQuery] int limit = 10)
        {
            var result = await _reviewService.GetTopRatedMoviesAsync(limit);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetReviews([FromQuery] ReviewQueryParameters queryParams)
        {
            var result = await _reviewService.GetPagedReviewsAsync(queryParams);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("statistics")]
        public async Task<IActionResult> GetReviewAnalytics()
        {
            var result = await _reviewService.GetReviewAnalyticsAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateReviewStatus(int id, [FromBody] UpdateReviewStatusDto statusDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _reviewService.UpdateReviewStatusAsync(id, statusDto.Status);
            if (result == null)
            {
                return NotFound(new { Message = $"Review with ID {id} not found." });
            }
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
            var userId = GetCurrentUserId();
            var result = await _reviewService.LikeReviewAsync(id, userId);
            if (result == null)
            {
                return NotFound(new { Message = $"Review with ID {id} not found." });
            }
            return Ok(result);
        }

        [Authorize]
        [HttpPost("{id:int}/dislike")]
        public async Task<IActionResult> DislikeReview(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _reviewService.DislikeReviewAsync(id, userId);
            if (result == null)
            {
                return NotFound(new { Message = $"Review with ID {id} not found." });
            }
            return Ok(result);
        }

        [Authorize]
        [HttpPost("{id:int}/replies")]
        public async Task<IActionResult> AddReply(int id, [FromBody] ReviewReplyCreateDto createDto)
        {
            if (createDto == null || string.IsNullOrWhiteSpace(createDto.Content))
            {
                return BadRequest("Nội dung không thể để trống.");
            }

            var userId = GetCurrentUserId();
            var result = await _reviewService.AddReplyAsync(id, userId, createDto);
            if (result == null)
            {
                return NotFound(new { Message = $"Review with ID {id} not found." });
            }
            return Created("", result);
        }

        [Authorize]
        [HttpDelete("replies/{replyId:int}")]
        public async Task<IActionResult> DeleteReply(int replyId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();
                var success = await _reviewService.DeleteReplyAsync(replyId, userId, userRole);
                if (!success)
                {
                    return NotFound(new { Message = $"Reply with ID {replyId} not found or already deleted." });
                }
                return Ok(new { Message = "Phản hồi đã được xóa thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
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
