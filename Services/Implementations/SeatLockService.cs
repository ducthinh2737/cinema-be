using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Services.Implementations
{
    public class SeatLockService : ISeatLockService
    {
        private readonly IDistributedCache _cache;

        public SeatLockService(IDistributedCache cache)
        {
            _cache = cache;
        }

        private string GetSeatKey(int showtimeId, int seatId) => $"lock:showtime:{showtimeId}:seat:{seatId}";
        private string GetShowtimeAllSeatsKey(int showtimeId) => $"lock:showtime:{showtimeId}:all_seats";

        public async Task<bool> LockSeatAsync(int showtimeId, int seatId, string userId, int minutes = 10)
        {
            var key = GetSeatKey(showtimeId, seatId);
            var existingOwner = await _cache.GetStringAsync(key);

            if (existingOwner != null && existingOwner != userId)
            {
                return false; // Already locked by someone else
            }

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes)
            };
            await _cache.SetStringAsync(key, userId, options);

            await AddSeatToAllSeatsListAsync(showtimeId, seatId);
            return true;
        }

        public async Task<bool> UnlockSeatAsync(int showtimeId, int seatId, string userId)
        {
            var key = GetSeatKey(showtimeId, seatId);
            var owner = await _cache.GetStringAsync(key);

            if (owner == null) return true; // Already expired or released
            if (owner != userId) return false; // Not owner

            await _cache.RemoveAsync(key);
            await RemoveSeatFromAllSeatsListAsync(showtimeId, seatId);
            return true;
        }

        public async Task<bool> LockMultipleSeatsAsync(int showtimeId, List<int> seatIds, string userId, int minutes = 10)
        {
            // Check locks first
            foreach (var seatId in seatIds)
            {
                var key = GetSeatKey(showtimeId, seatId);
                var existingOwner = await _cache.GetStringAsync(key);
                if (existingOwner != null && existingOwner != userId)
                {
                    return false;
                }
            }

            // Lock seats
            foreach (var seatId in seatIds)
            {
                await LockSeatAsync(showtimeId, seatId, userId, minutes);
            }

            return true;
        }

        public async Task<bool> UnlockMultipleSeatsAsync(int showtimeId, List<int> seatIds, string userId)
        {
            var success = true;
            foreach (var seatId in seatIds)
            {
                var ok = await UnlockSeatAsync(showtimeId, seatId, userId);
                if (!ok) success = false;
            }
            return success;
        }

        public async Task<List<int>> GetLockedSeatsAsync(int showtimeId)
        {
            var listKey = GetShowtimeAllSeatsKey(showtimeId);
            var json = await _cache.GetStringAsync(listKey);
            if (json == null) return new List<int>();

            var seatIds = JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
            var activeSeats = new List<int>();
            var expiredSeats = new List<int>();

            foreach (var seatId in seatIds)
            {
                var key = GetSeatKey(showtimeId, seatId);
                var owner = await _cache.GetStringAsync(key);
                if (owner != null)
                {
                    activeSeats.Add(seatId);
                }
                else
                {
                    expiredSeats.Add(seatId);
                }
            }

            // Self-clean expired lock items
            if (expiredSeats.Any())
            {
                var updatedList = seatIds.Except(expiredSeats).ToList();
                await _cache.SetStringAsync(listKey, JsonSerializer.Serialize(updatedList));
            }

            return activeSeats;
        }

        public async Task<string?> GetSeatLockOwnerAsync(int showtimeId, int seatId)
        {
            return await _cache.GetStringAsync(GetSeatKey(showtimeId, seatId));
        }

        private async Task AddSeatToAllSeatsListAsync(int showtimeId, int seatId)
        {
            var listKey = GetShowtimeAllSeatsKey(showtimeId);
            var json = await _cache.GetStringAsync(listKey);
            var seatIds = json != null ? JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>() : new List<int>();

            if (!seatIds.Contains(seatId))
            {
                seatIds.Add(seatId);
                await _cache.SetStringAsync(listKey, JsonSerializer.Serialize(seatIds));
            }
        }

        private async Task RemoveSeatFromAllSeatsListAsync(int showtimeId, int seatId)
        {
            var listKey = GetShowtimeAllSeatsKey(showtimeId);
            var json = await _cache.GetStringAsync(listKey);
            if (json == null) return;

            var seatIds = JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
            if (seatIds.Contains(seatId))
            {
                seatIds.Remove(seatId);
                await _cache.SetStringAsync(listKey, JsonSerializer.Serialize(seatIds));
            }
        }
    }
}
