using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Cinemas;

namespace CinemaBooking.API.Data.Configurations
{
    public class HallTypeConfiguration : IEntityTypeConfiguration<HallType>
    {
        public void Configure(EntityTypeBuilder<HallType> builder)
        {
            builder.HasKey(ht => ht.HallTypeId);
            builder.Property(ht => ht.TypeName).IsRequired().HasMaxLength(50);

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(ht => !ht.IsDeleted);
        }
    }
}
