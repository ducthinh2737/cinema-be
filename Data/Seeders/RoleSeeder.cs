using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Models.Users;

namespace CinemaBooking.API.Data.Seeders
{
    public static class RoleSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "User" }
            );
        }
    }
}
