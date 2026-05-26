using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Movies;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface IMovieService
    {
        Task<PagedResultDto<MovieDto>> GetPagedMoviesAsync(MovieQueryParameters queryParams);
        Task<MovieDetailDto?> GetMovieByIdAsync(int id);
        Task<MovieDetailDto?> GetMovieBySlugAsync(string slug);
        Task<MovieDetailDto> CreateMovieAsync(MovieCreateDto createDto);
        Task<MovieDetailDto?> UpdateMovieAsync(int id, MovieUpdateDto updateDto);
        Task<bool> DeleteMovieAsync(int id, string deletedBy);
        Task<bool> UpdateMoviePhotosAsync(int id, string? posterUrl, string? bannerUrl);
    }
}
