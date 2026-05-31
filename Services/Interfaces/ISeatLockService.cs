using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaBooking.API.Services.Interfaces
{
    /// <summary>
    /// Metadata object stored in cache representing a seat lock session.
    /// </summary>
    public class SeatLockInfo
    {
        public int ShowtimeId { get; set; }
        public int SeatId { get; set; }
        public string UserId { get; set; } = null!;
        public DateTime LockedAt { get; set; }
        public DateTime ExpiredAt { get; set; }
        public string SessionId { get; set; } = null!;
    }

    /// <summary>
    /// Standard result representation for Seat Lock operations.
    /// </summary>
    public class SeatLockResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<int> LockedSeats { get; set; } = new();
        public List<int> FailedSeats { get; set; } = new();

        /// <summary>
        /// Allows implicit evaluation to bool for compatibility with existing business logic.
        /// </summary>
        public static implicit operator bool(SeatLockResult result)
        {
            return result != null && result.Success;
        }
    }

    /// <summary>
    /// Service managing high performance, atomic, and realtime seat locking for showtimes.
    /// </summary>
    public interface ISeatLockService
    {
        Task<SeatLockResult> LockSeatAsync(int showtimeId, int seatId, string userId, string sessionId, int minutes = 5, CancellationToken cancellationToken = default);
        Task<SeatLockResult> UnlockSeatAsync(int showtimeId, int seatId, string userId, string sessionId, bool force = false, CancellationToken cancellationToken = default);
        Task<SeatLockResult> LockMultipleSeatsAsync(int showtimeId, List<int> seatIds, string userId, string sessionId, int minutes = 5, CancellationToken cancellationToken = default);
        Task<SeatLockResult> UnlockMultipleSeatsAsync(int showtimeId, List<int> seatIds, string userId, string sessionId, bool force = false, CancellationToken cancellationToken = default);
        Task<List<int>> GetLockedSeatsAsync(int showtimeId, CancellationToken cancellationToken = default);
        Task<SeatLockInfo?> GetSeatLockInfoAsync(int showtimeId, int seatId, CancellationToken cancellationToken = default);
        Task<bool> IsSeatLockedAsync(int showtimeId, int seatId, CancellationToken cancellationToken = default);
        Task ReleaseExpiredLocksAsync(int showtimeId, CancellationToken cancellationToken = default);
        Task<bool> ValidateSeatOwnershipAsync(int showtimeId, int seatId, string userId, string sessionId, CancellationToken cancellationToken = default);
        Task<string?> GetSeatLockOwnerAsync(int showtimeId, int seatId, CancellationToken cancellationToken = default);
        Task<SeatLockResult> RefreshSeatLockAsync(int showtimeId, List<int> seatIds, string userId, string sessionId, int minutes = 5, CancellationToken cancellationToken = default);
    }
}
