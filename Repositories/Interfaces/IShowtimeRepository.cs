using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.DTOs.Showtimes;

namespace CinemaBooking.API.Repositories.Interfaces
{
    public interface IShowtimeRepository
    {
        Task<Showtime?> GetByIdAsync(int id);
        Task<Showtime?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<Showtime>> GetAllAsync();
        Task<IEnumerable<Showtime>> GetPagedShowtimesAsync(ShowtimeQueryParameters queryParams);
        Task<int> GetTotalCountAsync(ShowtimeQueryParameters queryParams);
        Task<IEnumerable<Showtime>> GetShowtimesByMovieIdAsync(int movieId);
        Task<IEnumerable<Showtime>> GetShowtimesByCinemaIdAsync(int cinemaId);
        Task<IEnumerable<Showtime>> GetOverlappingShowtimesAsync(int hallId, DateTime startTime, DateTime endTime, int? excludeShowtimeId = null);
        Task AddAsync(Showtime showtime);
        void Update(Showtime showtime);
        void Delete(Showtime showtime);
        Task<bool> SaveChangesAsync();
        Task<int> GetBookedSeatsCountAsync(int showtimeId);
    }
}
