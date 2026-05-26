using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Cinemas;

namespace CinemaBooking.API.Data.Configurations
{
    public class SeatConfiguration : IEntityTypeConfiguration<Seat>
    {
        public void Configure(EntityTypeBuilder<Seat> builder)
        {
            builder.HasKey(s => s.SeatId);
            builder.Property(s => s.SeatCode).IsRequired().HasMaxLength(10);

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(s => !s.IsDeleted);
        }
    }
}
