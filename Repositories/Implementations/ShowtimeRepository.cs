using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.Repositories.Interfaces;

namespace CinemaBooking.API.Repositories.Implementations
{
    public class ShowtimeRepository : IShowtimeRepository
    {
        private readonly CinemaDbContext _context;

        public ShowtimeRepository(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<Showtime?> GetByIdAsync(int id)
        {
            return await _context.Showtimes.FindAsync(id);
        }

        public async Task<Showtime?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Hall).ThenInclude(h => h.Seats)
                .Include(s => s.Price)
                .FirstOrDefaultAsync(s => s.ShowtimeId == id);
        }

        public async Task<IEnumerable<Showtime>> GetAllAsync()
        {
            return await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Price)
                .ToListAsync();
        }

        public async Task<IEnumerable<Showtime>> GetPagedShowtimesAsync(ShowtimeQueryParameters queryParams)
        {
            var query = BuildFilteredQuery(queryParams);

            return await query
                .OrderBy(s => s.StartTime)
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(ShowtimeQueryParameters queryParams)
        {
            var query = BuildFilteredQuery(queryParams);
            return await query.CountAsync();
        }

        public async Task<IEnumerable<Showtime>> GetShowtimesByMovieIdAsync(int movieId)
        {
            return await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Hall).ThenInclude(h => h.Seats)
                .Include(s => s.Price)
                .Where(s => s.MovieId == movieId)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Showtime>> GetShowtimesByCinemaIdAsync(int cinemaId)
        {
            return await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Hall).ThenInclude(h => h.Seats)
                .Include(s => s.Price)
                .Where(s => s.Hall.CinemaId == cinemaId)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Showtime>> GetOverlappingShowtimesAsync(int hallId, DateTime startTime, DateTime endTime, int? excludeShowtimeId = null)
        {
            var query = _context.Showtimes
                .Where(s => s.HallId == hallId && s.StartTime < endTime && s.EndTime > startTime);

            if (excludeShowtimeId.HasValue)
            {
                query = query.Where(s => s.ShowtimeId != excludeShowtimeId.Value);
            }

            return await query.ToListAsync();
        }

        public async Task AddAsync(Showtime showtime)
        {
            await _context.Showtimes.AddAsync(showtime);
        }

        public void Update(Showtime showtime)
        {
            _context.Showtimes.Update(showtime);
        }

        public void Delete(Showtime showtime)
        {
            _context.Showtimes.Remove(showtime);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<int> GetBookedSeatsCountAsync(int showtimeId)
        {
            return await _context.BookingSeats
                .CountAsync(bs => bs.Booking.ShowtimeId == showtimeId && bs.Booking.BookingStatus != "Cancelled");
        }

        private IQueryable<Showtime> BuildFilteredQuery(ShowtimeQueryParameters queryParams)
        {
            var query = _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Hall).ThenInclude(h => h.Seats)
                .Include(s => s.Price)
                .AsQueryable();

            if (queryParams.CinemaId.HasValue)
            {
                query = query.Where(s => s.Hall.CinemaId == queryParams.CinemaId.Value);
            }

            if (queryParams.MovieId.HasValue)
            {
                query = query.Where(s => s.MovieId == queryParams.MovieId.Value);
            }

            if (queryParams.Date.HasValue)
            {
                var date = queryParams.Date.Value.Date;
                query = query.Where(s => s.StartTime.Date == date);
            }

            return query;
        }
    }
}
