using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.Models.Bookings;

namespace CinemaBooking.API.Repositories.Interfaces
{
    public interface IBookingRepository
    {
        Task<Booking?> GetByIdAsync(int id);
        Task<Booking?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<Booking>> GetByUserIdAsync(int userId);
        Task<IEnumerable<Booking>> GetActiveBookingsByShowtimeAsync(int showtimeId);
        Task<IEnumerable<Booking>> GetExpiredPendingBookingsAsync(DateTime threshold);
        Task AddAsync(Booking booking);
        void Update(Booking booking);
        Task<bool> SaveChangesAsync();
    }
}
