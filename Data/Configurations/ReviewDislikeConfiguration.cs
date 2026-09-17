using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Movies;

namespace CinemaBooking.API.Data.Configurations
{
    public class ReviewDislikeConfiguration : IEntityTypeConfiguration<ReviewDislike>
    {
        public void Configure(EntityTypeBuilder<ReviewDislike> builder)
        {
            builder.HasKey(rd => rd.ReviewDislikeId);

            // Unique index to prevent duplicate dislikes from a user on a single review
            builder.HasIndex(rd => new { rd.UserId, rd.ReviewId })
                   .IsUnique();

            builder.HasOne(rd => rd.Review)
                   .WithMany()
                   .HasForeignKey(rd => rd.ReviewId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rd => rd.User)
                   .WithMany()
                   .HasForeignKey(rd => rd.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
