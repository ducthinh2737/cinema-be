using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Cinemas;

namespace CinemaBooking.API.Data.Configurations
{
    public class CinemaConfiguration : IEntityTypeConfiguration<Cinema>
    {
        public void Configure(EntityTypeBuilder<Cinema> builder)
        {
            builder.HasKey(c => c.CinemaId);
            builder.Property(c => c.CinemaName).IsRequired().HasMaxLength(150);
            builder.Property(c => c.Address).IsRequired().HasMaxLength(250);
            builder.Property(c => c.ImageUrl).HasMaxLength(500);

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(c => !c.IsDeleted);
        }
    }
}
