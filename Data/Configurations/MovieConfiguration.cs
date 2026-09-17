using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaBooking.API.Models.Movies;

namespace CinemaBooking.API.Data.Configurations
{
    public class MovieConfiguration : IEntityTypeConfiguration<Movie>
    {
        public void Configure(EntityTypeBuilder<Movie> builder)
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).HasColumnName("MovieId");

            builder.Property(m => m.Title).IsRequired().HasMaxLength(250);
            builder.Property(m => m.Slug).IsRequired().HasMaxLength(250);
            builder.Property(m => m.Language).IsRequired().HasMaxLength(50);
            builder.Property(m => m.PosterUrl).HasMaxLength(500);
            builder.Property(m => m.BannerUrl).HasMaxLength(500);
            builder.Property(m => m.TrailerUrl).HasMaxLength(500);
            builder.Property(m => m.Description).HasMaxLength(2000);
            builder.Property(m => m.Rating).HasDefaultValue(0.0);
            builder.Property(m => m.IsFeatured).HasDefaultValue(false);
            builder.Property(m => m.Status).IsRequired().HasMaxLength(50).HasDefaultValue("NowShowing");

            // Index on Slug
            builder.HasIndex(m => m.Slug).IsUnique();

            builder.HasMany(m => m.MovieActors)
                   .WithOne(ma => ma.Movie)
                   .HasForeignKey(ma => ma.MovieId);

            builder.HasMany(m => m.MovieFormats)
                   .WithMany(f => f.Movies)
                   .UsingEntity<Dictionary<string, object>>(
                       "MovieMovieFormat",
                       j => j.HasOne<MovieFormat>().WithMany().HasForeignKey("MovieFormatId"),
                       j => j.HasOne<Movie>().WithMany().HasForeignKey("MovieId"),
                       j =>
                       {
                           j.HasKey("MovieId", "MovieFormatId");
                           j.ToTable("MovieMovieFormats");
                       });

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(m => !m.IsDeleted);
        }
    }
}
