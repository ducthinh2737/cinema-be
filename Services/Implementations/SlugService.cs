using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Services.Implementations
{
    /// <summary>
    /// Service for generating SEO friendly, collision-safe unique URL slugs.
    /// </summary>
    public class SlugService : ISlugService
    {
        private readonly CinemaDbContext _context;

        public SlugService(CinemaDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Converts standard Vietnamese string to clean lowercase URL-friendly slug.
        /// </summary>
        public string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var cleanText = RemoveAccents(text).ToLowerInvariant();

            // Replace special Vietnamese characters
            cleanText = cleanText.Replace("đ", "d");

            // Remove invalid characters
            cleanText = Regex.Replace(cleanText, @"[^a-z0-9\s-]", "");

            // Collapse multiple spaces into one
            cleanText = Regex.Replace(cleanText, @"\s+", " ").Trim();

            // Limit length to 45 chars
            cleanText = cleanText.Substring(0, Math.Min(cleanText.Length, 45)).Trim();

            // Replace spaces with hyphens
            cleanText = Regex.Replace(cleanText, @"\s", "-");

            return cleanText;
        }

        /// <summary>
        /// Generates a unique, collision-safe slug for the given entity in database.
        /// </summary>
        public async Task<string> GenerateUniqueSlugAsync<TEntity>(string text, string columnName = "Slug") where TEntity : class
        {
            var baseSlug = GenerateSlug(text);
            var slug = baseSlug;
            var counter = 1;

            var dbSet = _context.Set<TEntity>();

            while (await SlugExistsAsync(dbSet, columnName, slug))
            {
                slug = $"{baseSlug}-{counter++}";
            }

            return slug;
        }

        private async Task<bool> SlugExistsAsync<TEntity>(DbSet<TEntity> dbSet, string columnName, string slugValue) where TEntity : class
        {
            return await dbSet.AnyAsync(e => EF.Property<string>(e, columnName) == slugValue);
        }

        private string RemoveAccents(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
