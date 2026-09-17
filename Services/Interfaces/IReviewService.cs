using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Reviews;
using CinemaBooking.API.DTOs.Common;
using CinemaBooking.API.DTOs.Movies;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface IReviewService
    {
        Task<CinemaBooking.API.DTOs.Common.PagedResultDto<ReviewDto>> GetPagedReviewsByMovieIdAsync(int movieId, ReviewQueryParameters queryParams);
        Task<CinemaBooking.API.DTOs.Common.PagedResultDto<ReviewDto>> GetPagedReviewsByUserIdAsync(int userId, ReviewQueryParameters queryParams);
        Task<CinemaBooking.API.DTOs.Common.PagedResultDto<ReviewDto>> GetPagedReviewsAsync(ReviewQueryParameters queryParams);
        Task<ReviewDto?> UpdateReviewStatusAsync(int id, string status);
        Task<ReviewDto?> GetReviewByIdAsync(int id);
        Task<ReviewDto> CreateReviewAsync(int userId, ReviewCreateDto createDto);
        Task<ReviewDto?> UpdateReviewAsync(int id, int userId, ReviewUpdateDto updateDto);
        Task<bool> DeleteReviewAsync(int id, int userId, string userRole);
        Task<ReviewDto?> LikeReviewAsync(int id, int userId);
        Task<ReviewDto?> DislikeReviewAsync(int id, int userId);
        Task<ReviewReplyDto?> AddReplyAsync(int reviewId, int userId, ReviewReplyCreateDto createDto);
        Task<bool> DeleteReplyAsync(int replyId, int userId, string userRole);
        Task<double> GetAverageRatingAsync(int movieId);
        Task<MovieRatingSummaryDto> GetMovieRatingSummaryAsync(int movieId);
        Task<IEnumerable<MovieDto>> GetTopRatedMoviesAsync(int limit);
        Task<ReviewAnalyticsDto> GetReviewAnalyticsAsync();
    }
}
