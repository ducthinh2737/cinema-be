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
                .Include(r => r.User)
                .Include(r => r.Movie)
                .Where(r => r.MovieId == movieId)
                .AsQueryable();

            int totalCount = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return (reviews, totalCount);
        }

        public async Task<(IEnumerable<Review> Reviews, int TotalCount)> GetPagedReviewsByUserIdAsync(int userId, ReviewQueryParameters queryParams)
        {
            var query = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Movie)
                .Where(r => r.UserId == userId)
                .AsQueryable();

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
                .FirstOrDefaultAsync(r => r.ReviewId == id);
        }

        public async Task<Review?> GetReviewByUserAndMovieAsync(int userId, int movieId)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId);
        }

        public async Task AddReviewAsync(Review review)
        {
            review.CreatedAt = DateTime.UtcNow;
            await _context.Reviews.AddAsync(review);
        }

        public Task UpdateReviewAsync(Review review)
        {
            _context.Reviews.Update(review);
            return Task.CompletedTask;
        }

        public Task DeleteReviewAsync(Review review)
        {
            review.IsDeleted = true;
            review.DeletedAt = DateTime.UtcNow;
            _context.Reviews.Update(review);
            return Task.CompletedTask;
        }

        public async Task<double> GetAverageRatingAsync(int movieId)
        {
            var hasReviews = await _context.Reviews.AnyAsync(r => r.MovieId == movieId);
            if (!hasReviews) return 0.0;

            return await _context.Reviews
                .Where(r => r.MovieId == movieId)
                .AverageAsync(r => r.Rating);
        }

        public async Task<IEnumerable<Movie>> GetTopRatedMoviesAsync(int limit)
        {
            return await _context.Movies
                .Select(m => new
                {
                    Movie = m,
                    AvgRating = _context.Reviews.Where(r => r.MovieId == m.Id).Average(r => (double?)r.Rating) ?? 0.0
                })
                .OrderByDescending(x => x.AvgRating)
                .Take(limit)
                .Select(x => x.Movie)
                .ToListAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
