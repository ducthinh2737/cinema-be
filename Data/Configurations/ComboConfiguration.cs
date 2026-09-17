using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Combos;

namespace CinemaBooking.API.Data.Configurations
{
    public class ComboConfiguration : IEntityTypeConfiguration<Combo>
    {
        public void Configure(EntityTypeBuilder<Combo> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("ComboId");

            builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
            builder.Property(c => c.Description).HasMaxLength(1000);
            builder.Property(c => c.ImageUrl).HasMaxLength(500);
            builder.Property(c => c.Price).HasColumnType("decimal(18,2)");
            builder.Property(c => c.OriginalPrice).HasColumnType("decimal(18,2)");
            builder.Property(c => c.DiscountBadge).HasMaxLength(50);
            builder.Property(c => c.IsActive).HasDefaultValue(true);
            builder.Property(c => c.DisplayOrder).HasDefaultValue(0);

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(c => !c.IsDeleted);
        }
    }
}
