using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Movies;
using CinemaBooking.API.DTOs.Showtimes;

namespace CinemaBooking.API.Services.Interfaces
{
    /// <summary>
    /// Service for enterprise showtime scheduling, validation, and analytics.
    /// </summary>
    public interface IShowtimeService
    {
        Task<ApiResponse<PagedResultDto<ShowtimeDto>>> GetPagedShowtimesAsync(ShowtimeQueryParameters queryParams);
        Task<ApiResponse<ShowtimeDto>> GetShowtimeByIdAsync(int id);
        Task<ApiResponse<IEnumerable<ShowtimeDto>>> GetShowtimesByMovieIdAsync(int movieId);
        Task<ApiResponse<IEnumerable<ShowtimeDto>>> GetShowtimesByCinemaIdAsync(int cinemaId);
        Task<ApiResponse<ShowtimeDto>> CreateShowtimeAsync(ShowtimeCreateDto createDto);
        Task<ApiResponse<ShowtimeDto>> UpdateShowtimeAsync(int id, ShowtimeUpdateDto updateDto);
        Task<ApiResponse<bool>> DeleteShowtimeAsync(int id);
        Task<ApiResponse<bool>> ValidateShowtimeAsync(ShowtimeCreateDto createDto);
        Task<ApiResponse<bool>> CheckScheduleConflictAsync(int hallId, DateTime startTime, DateTime endTime, int? excludeShowtimeId = null);
        Task<DateTime> CalculateEndTime(int movieId, DateTime startTime);
        Task<ApiResponse<int>> CalculateAvailableSeatsAsync(int showtimeId);
    }
}
