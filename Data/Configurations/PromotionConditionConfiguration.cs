using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Promotions;

namespace CinemaBooking.API.Data.Configurations
{
    public class PromotionConditionConfiguration : IEntityTypeConfiguration<PromotionCondition>
    {
        public void Configure(EntityTypeBuilder<PromotionCondition> builder)
        {
            builder.HasKey(c => c.PromotionConditionId);
            builder.Property(c => c.ApplyType).IsRequired().HasMaxLength(30).HasConversion<string>();
            builder.Property(c => c.DaysOfWeek).HasMaxLength(150);

            // Optional time constraints
            builder.Property(c => c.StartHour);
            builder.Property(c => c.EndHour);
        }
    }
}
