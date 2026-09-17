using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Users;

namespace CinemaBooking.API.Data.Configurations
{
    public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
    {
        public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
        {
            builder.HasKey(lt => lt.LoyaltyTransactionId);
            builder.Property(lt => lt.TransactionType).IsRequired().HasMaxLength(50);
            builder.Property(lt => lt.Status).IsRequired().HasMaxLength(50);
            builder.Property(lt => lt.Description).HasMaxLength(250);

            builder.HasOne(lt => lt.User)
                   .WithMany(u => u.LoyaltyTransactions)
                   .HasForeignKey(lt => lt.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(lt => lt.Booking)
                   .WithMany()
                   .HasForeignKey(lt => lt.BookingId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
