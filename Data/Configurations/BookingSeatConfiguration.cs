using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Bookings;

namespace CinemaBooking.API.Data.Configurations
{
    public class BookingSeatConfiguration : IEntityTypeConfiguration<BookingSeat>
    {
        public void Configure(EntityTypeBuilder<BookingSeat> builder)
        {
            builder.HasKey(bs => bs.BookingSeatId);
            builder.Property(bs => bs.UnitPrice).HasColumnType("decimal(18,2)");

            builder.HasOne(bs => bs.Booking)
                   .WithMany(b => b.BookingSeats)
                   .HasForeignKey(bs => bs.BookingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(bs => bs.Seat)
                   .WithMany()
                   .HasForeignKey(bs => bs.SeatId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
