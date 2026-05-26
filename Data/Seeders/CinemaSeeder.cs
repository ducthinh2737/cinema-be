using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Models.Cinemas;

namespace CinemaBooking.API.Data.Seeders
{
    public static class CinemaSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<City>().HasData(
                new City { CityId = 1, CityName = "Ho Chi Minh" },
                new City { CityId = 2, CityName = "Ha Noi" }
            );

            modelBuilder.Entity<Cinema>().HasData(
                new Cinema { CinemaId = 1, CinemaName = "Cinema Center District 1", Address = "123 Le Loi", CityId = 1 },
                new Cinema { CinemaId = 2, CinemaName = "Cinema Center Dong Da", Address = "456 Nguyen Trai", CityId = 2 }
            );
        }
    }
}
