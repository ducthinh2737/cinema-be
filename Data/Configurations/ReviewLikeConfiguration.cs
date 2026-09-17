using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Movies;

namespace CinemaBooking.API.Data.Configurations
{
    public class ReviewLikeConfiguration : IEntityTypeConfiguration<ReviewLike>
    {
        public void Configure(EntityTypeBuilder<ReviewLike> builder)
        {
            builder.HasKey(rl => rl.ReviewLikeId);

            // Unique index to prevent duplicate likes from a user on a single review
            builder.HasIndex(rl => new { rl.UserId, rl.ReviewId })
                   .IsUnique();

            builder.HasOne(rl => rl.Review)
                   .WithMany()
                   .HasForeignKey(rl => rl.ReviewId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rl => rl.User)
                   .WithMany()
                   .HasForeignKey(rl => rl.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
