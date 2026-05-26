using System.Collections.Generic;
using System.Threading.Tasks;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface ISeatLockService
    {
        Task<bool> LockSeatAsync(int showtimeId, int seatId, string userId, int minutes = 10);
        Task<bool> UnlockSeatAsync(int showtimeId, int seatId, string userId);
        Task<bool> LockMultipleSeatsAsync(int showtimeId, List<int> seatIds, string userId, int minutes = 10);
        Task<bool> UnlockMultipleSeatsAsync(int showtimeId, List<int> seatIds, string userId);
        Task<List<int>> GetLockedSeatsAsync(int showtimeId);
        Task<string?> GetSeatLockOwnerAsync(int showtimeId, int seatId);
    }
}
