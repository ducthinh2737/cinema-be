using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Promotions;

namespace CinemaBooking.API.Data.Configurations
{
    public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
    {
        public void Configure(EntityTypeBuilder<Promotion> builder)
        {
            builder.HasKey(p => p.PromotionId);
            builder.Property(p => p.PromoCode).IsRequired().HasMaxLength(50);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
            builder.Property(p => p.Description).HasMaxLength(500);
            builder.Property(p => p.DiscountType).IsRequired().HasMaxLength(20).HasConversion<string>();
            builder.Property(p => p.DiscountValue).HasColumnType("decimal(18,2)");
            builder.Property(p => p.MaxDiscountAmount).HasColumnType("decimal(18,2)");
            builder.Property(p => p.MinimumOrderValue).HasColumnType("decimal(18,2)");

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(p => !p.IsDeleted);

            // One-to-many relationship with PromotionConditions
            builder.HasMany(p => p.PromotionConditions)
                .WithOne(c => c.Promotion)
                .HasForeignKey(c => c.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
