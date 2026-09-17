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
using CinemaBooking.API.Models.News;
using CinemaBooking.API.Models.Combos;

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
        public DbSet<MemberTier> MemberTiers { get; set; } = null!;
        public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; } = null!;
        public DbSet<Movie> Movies { get; set; } = null!;
        public DbSet<Genre> Genres { get; set; } = null!;
        public DbSet<Director> Directors { get; set; } = null!;
        public DbSet<Actor> Actors { get; set; } = null!;
        public DbSet<MovieActor> MovieActors { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<ReviewLike> ReviewLikes { get; set; } = null!;
        public DbSet<ReviewDislike> ReviewDislikes { get; set; } = null!;
        public DbSet<ReviewReply> ReviewReplies { get; set; } = null!;
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
        public DbSet<PromotionCondition> PromotionConditions { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<MovieFormat> MovieFormats { get; set; } = null!;
        public DbSet<Language> Languages { get; set; } = null!;
        public DbSet<SubtitleType> SubtitleTypes { get; set; } = null!;
        public DbSet<AgeRating> AgeRatings { get; set; } = null!;
        public DbSet<CinemaBooking.API.Infrastructure.Outbox.OutboxEvent> OutboxEvents { get; set; } = null!;
        public DbSet<PricingRule> PricingRules { get; set; } = null!;
        public DbSet<NewsItem> NewsItems { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Combo> Combos { get; set; } = null!;
        public DbSet<ComboItem> ComboItems { get; set; } = null!;
        public DbSet<OrderCombo> OrderCombos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CinemaDbContext).Assembly);

            // Configure all decimal properties to use decimal(18,2) precision
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18,2)");
            }

            // Call Seeders
            CinemaBooking.API.Data.Seeders.RoleSeeder.Seed(modelBuilder);
            CinemaBooking.API.Data.Seeders.GenreSeeder.Seed(modelBuilder);
            CinemaBooking.API.Data.Seeders.CinemaSeeder.Seed(modelBuilder);
        }
    }
}
