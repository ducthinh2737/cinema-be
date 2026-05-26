using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Movies;

namespace CinemaBooking.API.Data.Configurations
{
    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.HasKey(r => r.ReviewId);

            builder.Property(r => r.Comment)
                   .HasMaxLength(1000);

            // Unique index to prevent duplicate user reviews on a single movie
            builder.HasIndex(r => new { r.UserId, r.MovieId })
                   .IsUnique();

            builder.HasOne(r => r.User)
                   .WithMany()
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Movie)
                   .WithMany(m => m.Reviews)
                   .HasForeignKey(r => r.MovieId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Global query filter for soft delete
            builder.HasQueryFilter(r => !r.IsDeleted);
        }
    }
}
