using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Combos;

namespace CinemaBooking.API.Data.Configurations
{
    public class ComboItemConfiguration : IEntityTypeConfiguration<ComboItem>
    {
        public void Configure(EntityTypeBuilder<ComboItem> builder)
        {
            builder.HasKey(ci => new { ci.ComboId, ci.ProductId });

            builder.HasOne(ci => ci.Combo)
                   .WithMany(c => c.ComboItems)
                   .HasForeignKey(ci => ci.ComboId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ci => ci.Product)
                   .WithMany(p => p.ComboItems)
                   .HasForeignKey(ci => ci.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(ci => ci.Quantity).HasDefaultValue(1);
        }
    }
}
