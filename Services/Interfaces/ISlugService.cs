using System.Threading.Tasks;

namespace CinemaBooking.API.Services.Interfaces
{
    /// <summary>
    /// Service for generating SEO friendly, collision-safe unique URL slugs.
    /// </summary>
    public interface ISlugService
    {
        /// <summary>
        /// Converts standard Vietnamese string to clean lowercase URL-friendly slug.
        /// </summary>
        string GenerateSlug(string text);

        /// <summary>
        /// Generates a unique, collision-safe slug for the given entity in database.
        /// </summary>
        Task<string> GenerateUniqueSlugAsync<TEntity>(string text, string columnName = "Slug") where TEntity : class;
    }
}
