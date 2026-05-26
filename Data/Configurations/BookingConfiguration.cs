using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Bookings;

namespace CinemaBooking.API.Data.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.HasKey(b => b.BookingId);
            builder.Property(b => b.BookingCode).IsRequired().HasMaxLength(50);
            builder.Property(b => b.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Property(b => b.ServiceFee).HasColumnType("decimal(18,2)");
            builder.Property(b => b.DiscountAmount).HasColumnType("decimal(18,2)");
            builder.Property(b => b.QRCodeUrl).HasMaxLength(500);
            
            builder.Property(b => b.RowVersion).IsRowVersion();

            builder.HasMany(b => b.BookingSeats)
                   .WithOne(bs => bs.Booking)
                   .HasForeignKey(bs => bs.BookingId);
        }
    }
}
