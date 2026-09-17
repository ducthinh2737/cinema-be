using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Users;

namespace CinemaBooking.API.Data.Configurations
{
    public class MemberTierConfiguration : IEntityTypeConfiguration<MemberTier>
    {
        public void Configure(EntityTypeBuilder<MemberTier> builder)
        {
            builder.HasKey(mt => mt.MemberTierId);
            builder.Property(mt => mt.TierName).IsRequired().HasMaxLength(50);
            builder.Property(mt => mt.PointMultiplier).HasColumnType("decimal(18,2)");
        }
    }
}
