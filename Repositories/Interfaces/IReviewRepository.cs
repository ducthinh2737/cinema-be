using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.DTOs.Reviews;

namespace CinemaBooking.API.Repositories.Interfaces
{
    public interface IReviewRepository
    {
        Task<(IEnumerable<Review> Reviews, int TotalCount)> GetPagedReviewsByMovieIdAsync(int movieId, ReviewQueryParameters queryParams);
        Task<(IEnumerable<Review> Reviews, int TotalCount)> GetPagedReviewsByUserIdAsync(int userId, ReviewQueryParameters queryParams);
        Task<(IEnumerable<Review> Reviews, int TotalCount)> GetPagedReviewsAsync(ReviewQueryParameters queryParams);
        Task<Review?> GetReviewByIdAsync(int id);
        Task<Review?> GetReviewByUserAndMovieAsync(int userId, int movieId);
        Task AddReviewAsync(Review review);
        Task UpdateReviewAsync(Review review);
        Task DeleteReviewAsync(Review review);
        Task<double> GetAverageRatingAsync(int movieId);
        Task<MovieRatingSummaryDto> GetMovieRatingSummaryAsync(int movieId);
        Task<IEnumerable<Movie>> GetTopRatedMoviesAsync(int limit);
        Task<ReviewAnalyticsDto> GetReviewAnalyticsAsync();
        Task<bool> SaveChangesAsync();
    }
}
