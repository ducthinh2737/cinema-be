using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Models.Users;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.Models.Cinemas;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.Models.Bookings;
using CinemaBooking.API.Models.Payments;
using CinemaBooking.API.Models.Promotions;
using CinemaBooking.API.Models.Notifications;
using CinemaBooking.API.Models.Logs;

namespace CinemaBooking.API.Data
{
    public class CinemaDbContext : DbContext
    {
        public CinemaDbContext(DbContextOptions<CinemaDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<Movie> Movies { get; set; } = null!;
        public DbSet<Genre> Genres { get; set; } = null!;
        public DbSet<Director> Directors { get; set; } = null!;
        public DbSet<Actor> Actors { get; set; } = null!;
        public DbSet<MovieActor> MovieActors { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<City> Cities { get; set; } = null!;
        public DbSet<Cinema> Cinemas { get; set; } = null!;
        public DbSet<Hall> Halls { get; set; } = null!;
        public DbSet<HallType> HallTypes { get; set; } = null!;
        public DbSet<Seat> Seats { get; set; } = null!;
        public DbSet<SeatType> SeatTypes { get; set; } = null!;
        public DbSet<Showtime> Showtimes { get; set; } = null!;
        public DbSet<Price> Prices { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<BookingSeat> BookingSeats { get; set; } = null!;
        public DbSet<BookingPromotion> BookingPromotions { get; set; } = null!;
        public DbSet<Ticket> Tickets { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
        public DbSet<Refund> Refunds { get; set; } = null!;
        public DbSet<Promotion> Promotions { get; set; } = null!;
        public DbSet<UserPromotion> UserPromotions { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CinemaDbContext).Assembly);

            // Call Seeders
            CinemaBooking.API.Data.Seeders.RoleSeeder.Seed(modelBuilder);
            CinemaBooking.API.Data.Seeders.GenreSeeder.Seed(modelBuilder);
            CinemaBooking.API.Data.Seeders.CinemaSeeder.Seed(modelBuilder);
        }
    }
}
