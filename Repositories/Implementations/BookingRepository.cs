using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Bookings;
using CinemaBooking.API.Repositories.Interfaces;

namespace CinemaBooking.API.Repositories.Implementations
{
    public class BookingRepository : IBookingRepository
    {
        private readonly CinemaDbContext _context;

        public BookingRepository(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<Booking?> GetByIdAsync(int id)
        {
            return await _context.Bookings.FindAsync(id);
        }

        public async Task<Booking?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .FirstOrDefaultAsync(b => b.BookingId == id);
        }

        public async Task<IEnumerable<Booking>> GetByUserIdAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetActiveBookingsByShowtimeAsync(int showtimeId)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-10);
            return await _context.Bookings
                .Include(b => b.BookingSeats)
                .Where(b => b.ShowtimeId == showtimeId && 
                            b.BookingStatus != "Cancelled" &&
                            (b.BookingStatus == "Confirmed" || b.CreatedAt > threshold))
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetExpiredPendingBookingsAsync(DateTime threshold)
        {
            return await _context.Bookings
                .Include(b => b.BookingSeats)
                .Where(b => b.BookingStatus == "Pending" && b.CreatedAt < threshold)
                .ToListAsync();
        }

        public async Task AddAsync(Booking booking)
        {
            await _context.Bookings.AddAsync(booking);
        }

        public void Update(Booking booking)
        {
            _context.Bookings.Update(booking);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
