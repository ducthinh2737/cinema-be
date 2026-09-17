using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Cinemas;

namespace CinemaBooking.API.Data.Configurations
{
    public class SeatTypeConfiguration : IEntityTypeConfiguration<SeatType>
    {
        public void Configure(EntityTypeBuilder<SeatType> builder)
        {
            builder.HasKey(st => st.SeatTypeId);
            builder.Property(st => st.TypeName).IsRequired().HasMaxLength(50);

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(st => !st.IsDeleted);
        }
    }
}
