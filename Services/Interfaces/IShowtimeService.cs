using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Movies;
using CinemaBooking.API.DTOs.Showtimes;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface IShowtimeService
    {
        Task<PagedResultDto<ShowtimeDto>> GetPagedShowtimesAsync(ShowtimeQueryParameters queryParams);
        Task<ShowtimeDto?> GetShowtimeByIdAsync(int id);
        Task<IEnumerable<ShowtimeDto>> GetShowtimesByMovieIdAsync(int movieId);
        Task<IEnumerable<ShowtimeDto>> GetShowtimesByCinemaIdAsync(int cinemaId);
        Task<ShowtimeDto> CreateShowtimeAsync(ShowtimeCreateDto createDto);
        Task<ShowtimeDto?> UpdateShowtimeAsync(int id, ShowtimeUpdateDto updateDto);
        Task<bool> DeleteShowtimeAsync(int id);
    }
}
