using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.DTOs.Movies;

namespace CinemaBooking.API.Repositories.Interfaces
{
    public interface IMovieRepository
    {
        Task<Movie?> GetByIdAsync(int id);
        Task<Movie?> GetByIdWithDetailsAsync(int id);
        Task<Movie?> GetBySlugAsync(string slug);
        Task<IEnumerable<Movie>> GetAllAsync();
        Task<(IEnumerable<Movie> Items, int TotalCount)> GetPagedMoviesAsync(MovieQueryParameters queryParams);
        Task AddAsync(Movie movie);
        void Update(Movie movie);
        void Delete(Movie movie);
        Task<bool> SaveChangesAsync();
    }
}
