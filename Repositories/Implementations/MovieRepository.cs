using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.DTOs.Movies;
using CinemaBooking.API.Repositories.Interfaces;

namespace CinemaBooking.API.Repositories.Implementations
{
    public class MovieRepository : IMovieRepository
    {
        private readonly CinemaDbContext _context;

        public MovieRepository(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<Movie?> GetByIdAsync(int id)
        {
            return await _context.Movies.FindAsync(id);
        }

        public async Task<Movie?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Movies
                .Include(m => m.Genre)
                .Include(m => m.Director)
                .Include(m => m.MovieActors).ThenInclude(ma => ma.Actor)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<Movie?> GetBySlugAsync(string slug)
        {
            return await _context.Movies
                .Include(m => m.Genre)
                .Include(m => m.Director)
                .Include(m => m.MovieActors).ThenInclude(ma => ma.Actor)
                .FirstOrDefaultAsync(m => m.Slug == slug);
        }

        public async Task<IEnumerable<Movie>> GetAllAsync()
        {
            return await _context.Movies
                .Include(m => m.Genre)
                .Include(m => m.Director)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Movie> Items, int TotalCount)> GetPagedMoviesAsync(MovieQueryParameters queryParams)
        {
            var query = _context.Movies.AsQueryable();

            // Search by Name
            if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
            {
                query = query.Where(m => m.Title.Contains(queryParams.SearchTerm));
            }

            // Filter by Genre
            if (queryParams.GenreId.HasValue)
            {
                query = query.Where(m => m.GenreId == queryParams.GenreId.Value);
            }

            // Filter by Showing/Upcoming status
            if (!string.IsNullOrWhiteSpace(queryParams.Status))
            {
                var now = DateTime.UtcNow;
                var statusLower = queryParams.Status.ToLower();

                if (statusLower == "showing")
                {
                    query = query.Where(m => m.ReleaseDate <= now && m.EndDate >= now);
                }
                else if (statusLower == "upcoming")
                {
                    query = query.Where(m => m.ReleaseDate > now);
                }
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(m => m.Genre)
                .Include(m => m.Director)
                .OrderByDescending(m => m.ReleaseDate)
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(Movie movie)
        {
            await _context.Movies.AddAsync(movie);
        }

        public void Update(Movie movie)
        {
            _context.Movies.Update(movie);
        }

        public void Delete(Movie movie)
        {
            // Note: Since we have global filter, soft delete is done in service by setting IsDeleted = true
            _context.Movies.Remove(movie);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
