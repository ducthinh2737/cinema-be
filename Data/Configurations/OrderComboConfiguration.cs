using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Combos;

namespace CinemaBooking.API.Data.Configurations
{
    public class OrderComboConfiguration : IEntityTypeConfiguration<OrderCombo>
    {
        public void Configure(EntityTypeBuilder<OrderCombo> builder)
        {
            builder.HasKey(oc => oc.OrderComboId);

            builder.Property(oc => oc.Price).HasColumnType("decimal(18,2)");
            builder.Property(oc => oc.Quantity).IsRequired();

            builder.HasOne(oc => oc.Booking)
                   .WithMany(b => b.OrderCombos)
                   .HasForeignKey(oc => oc.BookingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(oc => oc.Combo)
                   .WithMany()
                   .HasForeignKey(oc => oc.ComboId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
