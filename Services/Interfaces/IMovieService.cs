using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Movies;

namespace CinemaBooking.API.Services.Interfaces
{
    /// <summary>
    /// Enterprise Movie Management Service for administration, analytics, and listings.
    /// </summary>
    public interface IMovieService
    {
        Task<ApiResponse<PagedResultDto<MovieDto>>> GetPagedMoviesAsync(MovieQueryParameters queryParams);
        Task<ApiResponse<MovieDetailDto>> GetMovieByIdAsync(int id);
        Task<ApiResponse<MovieDetailDto>> GetMovieBySlugAsync(string slug);
        Task<ApiResponse<MovieDetailDto>> CreateMovieAsync(MovieCreateDto createDto);
        Task<ApiResponse<MovieDetailDto>> UpdateMovieAsync(int id, MovieUpdateDto updateDto);
        Task<ApiResponse<bool>> DeleteMovieAsync(int id);
        Task<ApiResponse<bool>> RestoreMovieAsync(int id);
        Task<ApiResponse<bool>> UpdateMoviePhotosAsync(int id, string? posterUrl, string? bannerUrl);
        Task<ApiResponse<bool>> ValidateMovieAsync(MovieCreateDto createDto);
        Task<ApiResponse<bool>> UpdateMovieActorsAsync(int id, List<int> actorIds);
    }
}
