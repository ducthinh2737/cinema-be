namespace CinemaBooking.API.Services.Interfaces
{
    /// <summary>
    /// Service to retrieve context information about the currently authenticated user.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Unique user identifier.
        /// </summary>
        string? UserId { get; }

        /// <summary>
        /// Name of the authenticated user. Falls back to "System" if anonymous.
        /// </summary>
        string? UserName { get; }
    }
}
