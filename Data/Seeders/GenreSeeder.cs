using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Models.Movies;

namespace CinemaBooking.API.Data.Seeders
{
    public static class GenreSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Genre>().HasData(
                new Genre { GenreId = 1, GenreName = "Action" },
                new Genre { GenreId = 2, GenreName = "Comedy" },
                new Genre { GenreId = 3, GenreName = "Drama" }
            );
        }
    }
}
