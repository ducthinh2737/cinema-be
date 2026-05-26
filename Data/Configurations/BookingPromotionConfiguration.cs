using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Bookings;

namespace CinemaBooking.API.Data.Configurations
{
    public class BookingPromotionConfiguration : IEntityTypeConfiguration<BookingPromotion>
    {
        public void Configure(EntityTypeBuilder<BookingPromotion> builder)
        {
            builder.HasKey(bp => bp.BookingPromotionId);

            builder.HasOne<Booking>()
                   .WithMany()
                   .HasForeignKey(bp => bp.BookingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Models.Promotions.Promotion>()
                   .WithMany()
                   .HasForeignKey(bp => bp.PromotionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
