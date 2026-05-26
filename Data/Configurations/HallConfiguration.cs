using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Cinemas;

namespace CinemaBooking.API.Data.Configurations
{
    public class HallConfiguration : IEntityTypeConfiguration<Hall>
    {
        public void Configure(EntityTypeBuilder<Hall> builder)
        {
            builder.HasKey(h => h.HallId);
            builder.Property(h => h.HallName).IsRequired().HasMaxLength(100);

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(h => !h.IsDeleted);
        }
    }
}
