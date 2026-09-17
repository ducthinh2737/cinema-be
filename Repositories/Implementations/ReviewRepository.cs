using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.DTOs.Reviews;
using CinemaBooking.API.Repositories.Interfaces;

namespace CinemaBooking.API.Repositories.Implementations
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly CinemaDbContext _context;

        public ReviewRepository(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Review> Reviews, int TotalCount)> GetPagedReviewsByMovieIdAsync(int movieId, ReviewQueryParameters queryParams)
        {
            var query = _context.Reviews
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Movie)
                .Include(r => r.Replies).ThenInclude(rp => rp.User)
                .Include(r => r.Replies).ThenInclude(rp => rp.ParentReply).ThenInclude(pr => pr.User)
                .Where(r => r.MovieId == movieId && !r.IsDeleted && r.Status == "Approved")
                .AsQueryable();

            int totalCount = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(r => r.IsVerifiedViewer)
                .ThenByDescending(r => r.CreatedAt)
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return (reviews, totalCount);
        }

        public async Task<(IEnumerable<Review> Reviews, int TotalCount)> GetPagedReviewsByUserIdAsync(int userId, ReviewQueryParameters queryParams)
        {
            var query = _context.Reviews
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Movie)
                .Where(r => r.UserId == userId && !r.IsDeleted)
                .AsQueryable();

            int totalCount = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return (reviews, totalCount);
        }

        public async Task<(IEnumerable<Review> Reviews, int TotalCount)> GetPagedReviewsAsync(ReviewQueryParameters queryParams)
        {
            var query = _context.Reviews
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Movie)
                .Where(r => !r.IsDeleted)
                .AsQueryable();

            if (queryParams.MovieId.HasValue)
            {
                query = query.Where(r => r.MovieId == queryParams.MovieId.Value);
            }

            if (queryParams.Rating.HasValue)
            {
                query = query.Where(r => r.Rating == queryParams.Rating.Value);
            }

            if (!string.IsNullOrEmpty(queryParams.Status) && queryParams.Status != "All")
            {
                query = query.Where(r => r.Status == queryParams.Status);
            }

            if (queryParams.VerifiedOnly.HasValue && queryParams.VerifiedOnly.Value)
            {
                query = query.Where(r => r.IsVerifiedViewer);
            }

            if (!string.IsNullOrEmpty(queryParams.SearchKeyword))
            {
                var keyword = queryParams.SearchKeyword.ToLower();
                query = query.Where(r => r.Comment != null && r.Comment.ToLower().Contains(keyword) || 
                                         r.User.FullName.ToLower().Contains(keyword));
            }

            int totalCount = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return (reviews, totalCount);
        }

        public async Task<Review?> GetReviewByIdAsync(int id)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Movie)
                .Include(r => r.Replies).ThenInclude(rp => rp.User)
                .Include(r => r.Replies).ThenInclude(rp => rp.ParentReply).ThenInclude(pr => pr.User)
                .FirstOrDefaultAsync(r => r.ReviewId == id && !r.IsDeleted);
        }

        public async Task<Review?> GetReviewByUserAndMovieAsync(int userId, int movieId)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId && !r.IsDeleted);
        }

        public async Task AddReviewAsync(Review review)
        {
            review.CreatedAt = DateTime.UtcNow;
            await _context.Reviews.AddAsync(review);
        }

        public Task UpdateReviewAsync(Review review)
        {
            review.UpdatedAt = DateTime.UtcNow;
            _context.Reviews.Update(review);
            return Task.CompletedTask;
        }

        public Task DeleteReviewAsync(Review review)
        {
            _context.Reviews.Remove(review);
            return Task.CompletedTask;
        }

        public async Task<double> GetAverageRatingAsync(int movieId)
        {
            var hasReviews = await _context.Reviews.AnyAsync(r => r.MovieId == movieId && !r.IsDeleted && r.Status == "Approved");
            if (!hasReviews) return 0.0;

            double avg = await _context.Reviews
                .Where(r => r.MovieId == movieId && !r.IsDeleted && r.Status == "Approved")
                .AverageAsync(r => (double)r.Rating);

            return Math.Round(avg, 1, MidpointRounding.AwayFromZero);
        }

        public async Task<MovieRatingSummaryDto> GetMovieRatingSummaryAsync(int movieId)
        {
            var summary = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.MovieId == movieId && !r.IsDeleted && r.Status == "Approved")
                .GroupBy(r => r.MovieId)
                .Select(g => new MovieRatingSummaryDto
                {
                    TotalReviews = g.Count(),
                    AverageRating = g.Average(r => (double)r.Rating),
                    FiveStarCount = g.Count(r => r.Rating == 5),
                    FourStarCount = g.Count(r => r.Rating == 4),
                    ThreeStarCount = g.Count(r => r.Rating == 3),
                    TwoStarCount = g.Count(r => r.Rating == 2),
                    OneStarCount = g.Count(r => r.Rating == 1)
                })
                .FirstOrDefaultAsync();

            if (summary == null)
            {
                return new MovieRatingSummaryDto
                {
                    AverageRating = 0.0,
                    TotalReviews = 0,
                    FiveStarCount = 0,
                    FourStarCount = 0,
                    ThreeStarCount = 0,
                    TwoStarCount = 0,
                    OneStarCount = 0
                };
            }

            summary.AverageRating = Math.Round(summary.AverageRating, 1, MidpointRounding.AwayFromZero);
            return summary;
        }

        public async Task<IEnumerable<Movie>> GetTopRatedMoviesAsync(int limit)
        {
            return await _context.Movies
                .Select(m => new
                {
                    Movie = m,
                    Reviews = _context.Reviews.Where(r => r.MovieId == m.Id && !r.IsDeleted && r.Status == "Approved"),
                })
                .Select(x => new
                {
                    x.Movie,
                    ReviewCount = x.Reviews.Count(),
                    AvgRating = x.Reviews.Any() ? x.Reviews.Average(r => (double)r.Rating) : 0.0
                })
                .Where(x => x.ReviewCount >= 5)
                .OrderByDescending(x => x.AvgRating)
                .ThenByDescending(x => x.ReviewCount)
                .Take(limit)
                .Select(x => x.Movie)
                .ToListAsync();
        }

        public async Task<ReviewAnalyticsDto> GetReviewAnalyticsAsync()
        {
            var activeReviews = _context.Reviews.Where(r => !r.IsDeleted);

            int totalReviews = await activeReviews.CountAsync();
            double avgRating = 0.0;
            if (totalReviews > 0)
            {
                avgRating = Math.Round(await activeReviews.AverageAsync(r => (double)r.Rating), 1);
            }

            int verifiedReviews = await activeReviews.CountAsync(r => r.IsVerifiedViewer);
            int pendingReviews = await activeReviews.CountAsync(r => r.Status == "Pending");

            // Rating Distribution (1-5 stars)
            var distribution = await activeReviews
                .GroupBy(r => r.Rating)
                .Select(g => new RatingDistributionItem
                {
                    Stars = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Ensure all star counts 1-5 exist in the list
            var ratingDistribution = new List<RatingDistributionItem>();
            for (int i = 1; i <= 5; i++)
            {
                var match = distribution.FirstOrDefault(d => d.Stars == i);
                ratingDistribution.Add(new RatingDistributionItem
                {
                    Stars = i,
                    Count = match?.Count ?? 0
                });
            }

            // Reviews per day (last 7 days)
            var cutoffDate = DateTime.UtcNow.Date.AddDays(-7);
            var dailyReviews = await activeReviews
                .Where(r => r.CreatedAt >= cutoffDate)
                .GroupBy(r => r.CreatedAt.Date)
                .Select(g => new ReviewsPerDayItem
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Count = g.Count()
                })
                .ToListAsync();

            // Fill missing dates in the last 7 days
            var reviewsPerDay = new List<ReviewsPerDayItem>();
            for (int i = 6; i >= 0; i--)
            {
                var targetDateStr = DateTime.UtcNow.Date.AddDays(-i).ToString("yyyy-MM-dd");
                var match = dailyReviews.FirstOrDefault(d => d.Date == targetDateStr);
                reviewsPerDay.Add(new ReviewsPerDayItem
                {
                    Date = targetDateStr,
                    Count = match?.Count ?? 0
                });
            }

            // Top rated movies (at least 1 review)
            var topMovies = await _context.Reviews
                .Include(r => r.Movie)
                .Where(r => !r.IsDeleted && r.Status == "Approved")
                .GroupBy(r => new { r.MovieId, r.Movie.Title })
                .Select(g => new TopRatedMovieItem
                {
                    MovieTitle = g.Key.Title,
                    AverageRating = Math.Round(g.Average(r => (double)r.Rating), 1),
                    ReviewsCount = g.Count()
                })
                .OrderByDescending(x => x.AverageRating)
                .ThenByDescending(x => x.ReviewsCount)
                .Take(5)
                .ToListAsync();

            return new ReviewAnalyticsDto
            {
                TotalReviews = totalReviews,
                AverageRating = avgRating,
                VerifiedReviews = verifiedReviews,
                PendingReviews = pendingReviews,
                RatingDistribution = ratingDistribution,
                ReviewsPerDay = reviewsPerDay,
                TopRatedMovies = topMovies
            };
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
