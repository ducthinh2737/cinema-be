using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Showtimes;

namespace CinemaBooking.API.Data.Configurations
{
    public class ShowtimeConfiguration : IEntityTypeConfiguration<Showtime>
    {
        public void Configure(EntityTypeBuilder<Showtime> builder)
        {
            builder.HasKey(s => s.ShowtimeId);
            
            builder.Property(s => s.RowVersion).IsRowVersion();

            builder.HasQueryFilter(s => !s.IsDeleted);
            builder.Property(s => s.Status).HasMaxLength(50).HasDefaultValue("Active");

            builder.HasMany(s => s.Bookings)
                   .WithOne(b => b.Showtime)
                   .HasForeignKey(b => b.ShowtimeId);
        }
    }
}
