using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Promotions;

namespace CinemaBooking.API.Data.Configurations
{
    public class UserPromotionConfiguration : IEntityTypeConfiguration<UserPromotion>
    {
        public void Configure(EntityTypeBuilder<UserPromotion> builder)
        {
            builder.HasKey(up => up.UserPromotionId);

            builder.HasOne(up => up.User)
                   .WithMany()
                   .HasForeignKey(up => up.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(up => up.Promotion)
                   .WithMany()
                   .HasForeignKey(up => up.PromotionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
