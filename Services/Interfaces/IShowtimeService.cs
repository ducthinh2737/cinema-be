using System;
using System.Collections.Generic;
using System.Threading;
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
        Task<ApiResponse<PagedResultDto<ShowtimeDto>>> GetPagedShowtimesAsync(ShowtimeQueryParameters queryParams, CancellationToken cancellationToken = default);
        Task<ApiResponse<ShowtimeDto>> GetShowtimeByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ApiResponse<IEnumerable<ShowtimeDto>>> GetShowtimesByMovieIdAsync(int movieId, CancellationToken cancellationToken = default);
        Task<ApiResponse<IEnumerable<ShowtimeDto>>> GetShowtimesByCinemaIdAsync(int cinemaId, CancellationToken cancellationToken = default);
        Task<ApiResponse<ShowtimeDto>> CreateShowtimeAsync(ShowtimeCreateDto createDto, CancellationToken cancellationToken = default);
        Task<ApiResponse<string>> CreateBulkShowtimesAsync(ShowtimeBulkCreateDto bulkDto, CancellationToken cancellationToken = default);
        Task<ApiResponse<ShowtimeDto>> UpdateShowtimeAsync(int id, ShowtimeUpdateDto updateDto, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> DeleteShowtimeAsync(int id, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> ValidateShowtimeAsync(ShowtimeCreateDto createDto, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> CheckScheduleConflictAsync(int hallId, DateTime startTime, DateTime endTime, int? excludeShowtimeId = null, CancellationToken cancellationToken = default);
        Task<DateTime> CalculateEndTime(int movieId, DateTime startTime, CancellationToken cancellationToken = default);
        Task<ApiResponse<int>> CalculateAvailableSeatsAsync(int showtimeId, CancellationToken cancellationToken = default);
    }
}
