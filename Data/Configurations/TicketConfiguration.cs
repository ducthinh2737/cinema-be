using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Bookings;

namespace CinemaBooking.API.Data.Configurations
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.HasKey(t => t.TicketId);
            builder.Property(t => t.TicketCode).IsRequired().HasMaxLength(100);
            builder.Property(t => t.Price).HasColumnType("decimal(18,2)");

            builder.HasOne(t => t.Booking)
                   .WithMany()
                   .HasForeignKey(t => t.BookingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.BookingSeat)
                   .WithMany()
                   .HasForeignKey(t => t.BookingSeatId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
