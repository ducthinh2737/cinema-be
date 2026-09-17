using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Movies;

namespace CinemaBooking.API.Data.Configurations
{
    public class ReviewReplyConfiguration : IEntityTypeConfiguration<ReviewReply>
    {
        public void Configure(EntityTypeBuilder<ReviewReply> builder)
        {
            builder.HasKey(rr => rr.ReviewReplyId);

            builder.Property(rr => rr.Content)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.HasOne(rr => rr.Review)
                   .WithMany(r => r.Replies)
                   .HasForeignKey(rr => rr.ReviewId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rr => rr.User)
                   .WithMany()
                   .HasForeignKey(rr => rr.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(rr => rr.ParentReply)
                   .WithMany()
                   .HasForeignKey(rr => rr.ParentReplyId)
                   .OnDelete(DeleteBehavior.NoAction);

            // Filter out soft deleted replies
            builder.HasQueryFilter(rr => !rr.IsDeleted);
        }
    }
}
