using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using CinemaBooking.API.Services.Interfaces;
using CinemaBooking.API.SignalR;

namespace CinemaBooking.API.Services.Implementations
{
    /// <summary>
    /// Production-ready enterprise realtime Seat Locking Service utilizing StackExchange.Redis (with local DistributedCache fallback),
    /// dynamic SemaphoreSlim locks cached in IMemoryCache for zero memory leaks, bulk event broadcasting, and TTL refreshing.
    /// </summary>
    public class SeatLockService : ISeatLockService
    {
        private readonly IDistributedCache _cache;
        private readonly IMemoryCache _memoryCache;
        private readonly IHubContext<SeatHub> _hubContext;
        private readonly ILogger<SeatLockService> _logger;
        private readonly IConnectionMultiplexer? _redisConnection;
        private readonly IDatabase? _redisDb;

        public SeatLockService(
            IDistributedCache cache,
            IMemoryCache memoryCache,
            IHubContext<SeatHub> hubContext,
            ILogger<SeatLockService> logger,
            IConnectionMultiplexer? redisConnection = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redisConnection = redisConnection;
            
            if (_redisConnection != null)
            {
                try
                {
                    _redisDb = _redisConnection.GetDatabase();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve Redis database. Will fallback to distributed memory cache.");
                    _redisDb = null;
                }
            }
        }

        #region Cache Key Strategies

        private string GetSeatKey(int showtimeId, int seatId) => $"seatlock:showtime:{showtimeId}:seat:{seatId}";
        private string GetShowtimeAllSeatsKey(int showtimeId) => $"seatlock:showtime:{showtimeId}:all_seats";

        #endregion

        #region Lock Synchronization

        /// <summary>
        /// Retrieves or creates a Showtime-specific SemaphoreSlim from memory cache with automatic 10-minute sliding expiration to prevent memory leaks.
        /// </summary>
        private SemaphoreSlim GetShowtimeLock(int showtimeId)
        {
            var key = $"lock:showtime:{showtimeId}";
            return _memoryCache.GetOrCreate(key, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                return new SemaphoreSlim(1, 1);
            })!;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Attempts to lock a single seat atomically. Uses Redis SET NX when available, otherwise local Semaphore Slim fallback.
        /// </summary>
        public async Task<SeatLockResult> LockSeatAsync(int showtimeId, int seatId, string userId, string sessionId, int minutes = 5, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Acquiring lock on Showtime {ShowtimeId}, Seat {SeatId} for User {UserId} Session {SessionId}", showtimeId, seatId, userId, sessionId);

            var now = DateTime.UtcNow;
            var lockInfo = new SeatLockInfo
            {
                ShowtimeId = showtimeId,
                SeatId = seatId,
                UserId = userId,
                LockedAt = now,
                ExpiredAt = now.AddMinutes(minutes),
                SessionId = sessionId
            };

            var key = GetSeatKey(showtimeId, seatId);
            var json = JsonSerializer.Serialize(lockInfo);
            var expiry = TimeSpan.FromMinutes(minutes);

            if (_redisDb != null)
            {
                // Atomic Redis SET NX EX
                bool success = await _redisDb.StringSetAsync(key, json, expiry, When.NotExists);
                if (!success)
                {
                    var existing = await GetSeatLockInfoAsync(showtimeId, seatId, cancellationToken);
                    if (existing != null && existing.UserId == userId && existing.SessionId == sessionId)
                    {
                        // Re-entry: extend TTL
                        await _redisDb.KeyExpireAsync(key, expiry);
                        return new SeatLockResult
                        {
                            Success = true,
                            Message = "Seat lock refreshed (re-entry).",
                            LockedSeats = new List<int> { seatId }
                        };
                    }

                    return new SeatLockResult
                    {
                        Success = false,
                        Message = $"Seat {seatId} is already locked by another user or session.",
                        FailedSeats = new List<int> { seatId }
                    };
                }

                await AddSeatToAllSeatsListAsync(showtimeId, seatId, cancellationToken);
                await NotifySeatLockedAsync(showtimeId, seatId, userId, sessionId, cancellationToken);

                return new SeatLockResult
                {
                    Success = true,
                    Message = "Seat locked successfully.",
                    LockedSeats = new List<int> { seatId }
                };
            }
            else
            {
                // Thread-safe Memory Fallback
                var semaphore = GetShowtimeLock(showtimeId);
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var existing = await GetSeatLockInfoAsync(showtimeId, seatId, cancellationToken);
                    if (existing != null && existing.ExpiredAt > DateTime.UtcNow)
                    {
                        if (existing.UserId == userId && existing.SessionId == sessionId)
                        {
                            // Re-entry: extend TTL
                            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions { AbsoluteExpiration = lockInfo.ExpiredAt }, cancellationToken);
                            return new SeatLockResult
                            {
                                Success = true,
                                Message = "Seat lock refreshed (re-entry).",
                                LockedSeats = new List<int> { seatId }
                            };
                        }

                        return new SeatLockResult
                        {
                            Success = false,
                            Message = $"Seat {seatId} is already locked by another user or session.",
                            FailedSeats = new List<int> { seatId }
                        };
                    }

                    await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions { AbsoluteExpiration = lockInfo.ExpiredAt }, cancellationToken);
                    await AddSeatToAllSeatsListAsync(showtimeId, seatId, cancellationToken);
                    await NotifySeatLockedAsync(showtimeId, seatId, userId, sessionId, cancellationToken);

                    return new SeatLockResult
                    {
                        Success = true,
                        Message = "Seat locked successfully.",
                        LockedSeats = new List<int> { seatId }
                    };
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Unlocks a single seat if the requesting user owns the lock.
        /// </summary>
        public async Task<SeatLockResult> UnlockSeatAsync(int showtimeId, int seatId, string userId, string sessionId, bool force = false, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Releasing lock on Showtime {ShowtimeId}, Seat {SeatId} for User {UserId} Session {SessionId} (Force: {Force})", showtimeId, seatId, userId, sessionId, force);

            var key = GetSeatKey(showtimeId, seatId);
            var lockInfo = await GetSeatLockInfoAsync(showtimeId, seatId, cancellationToken);

            if (lockInfo == null)
            {
                return new SeatLockResult
                {
                    Success = true,
                    Message = "Seat lock is already released or expired."
                };
            }

            if (!force)
            {
                var isOwner = await ValidateSeatOwnershipAsync(showtimeId, seatId, userId, sessionId, cancellationToken);
                if (!isOwner)
                {
                    _logger.LogWarning("Unauthorized unlock attempt on Showtime {ShowtimeId}, Seat {SeatId} by User {UserId} Session {SessionId}", showtimeId, seatId, userId, sessionId);
                    return new SeatLockResult
                    {
                        Success = false,
                        Message = "You do not own the lock on this seat in this session.",
                        FailedSeats = new List<int> { seatId }
                    };
                }
            }

            if (_redisDb != null)
            {
                await _redisDb.KeyDeleteAsync(key);
            }
            else
            {
                await _cache.RemoveAsync(key, cancellationToken);
            }

            await RemoveSeatFromAllSeatsListAsync(showtimeId, seatId, cancellationToken);
            await NotifySeatReleasedAsync(showtimeId, seatId, "seat_unlocked", cancellationToken);

            return new SeatLockResult
            {
                Success = true,
                Message = "Seat unlocked successfully.",
                LockedSeats = new List<int> { seatId }
            };
        }

        /// <summary>
        /// Locks multiple seats in a transaction-like behavior. Rollbacks all locks if any seat lock fails.
        /// </summary>
        public async Task<SeatLockResult> LockMultipleSeatsAsync(int showtimeId, List<int> seatIds, string userId, string sessionId, int minutes = 5, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Locking multiple seats {Seats} for Showtime {ShowtimeId} User {UserId} Session {SessionId}", string.Join(",", seatIds), showtimeId, userId, sessionId);

            var lockedSeats = new List<int>();
            var failedSeats = new List<int>();
            var now = DateTime.UtcNow;
            var expiry = TimeSpan.FromMinutes(minutes);

            var semaphore = GetShowtimeLock(showtimeId);
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                // 1. Validation Phase (Critical Section)
                foreach (var seatId in seatIds)
                {
                    var lockInfo = await GetSeatLockInfoAsync(showtimeId, seatId, cancellationToken);
                    if (lockInfo != null && (lockInfo.UserId != userId || lockInfo.SessionId != sessionId) && lockInfo.ExpiredAt > DateTime.UtcNow)
                    {
                        failedSeats.Add(seatId);
                    }
                }

                if (failedSeats.Any())
                {
                    _logger.LogWarning("Cannot acquire multiple locks. Seats {Failed} are unavailable.", string.Join(",", failedSeats));
                    return new SeatLockResult
                    {
                        Success = false,
                        Message = "One or more seats are already locked.",
                        FailedSeats = failedSeats
                    };
                }

                // 2. Acquisition Phase
                foreach (var seatId in seatIds)
                {
                    var lockInfo = new SeatLockInfo
                    {
                        ShowtimeId = showtimeId,
                        SeatId = seatId,
                        UserId = userId,
                        LockedAt = now,
                        ExpiredAt = now.AddMinutes(minutes),
                        SessionId = sessionId
                    };

                    var key = GetSeatKey(showtimeId, seatId);
                    var json = JsonSerializer.Serialize(lockInfo);

                    if (_redisDb != null)
                    {
                        bool success = await _redisDb.StringSetAsync(key, json, expiry, When.NotExists);
                        if (success)
                        {
                            lockedSeats.Add(seatId);
                        }
                        else
                        {
                            failedSeats.Add(seatId);
                        }
                    }
                    else
                    {
                        await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions { AbsoluteExpiration = lockInfo.ExpiredAt }, cancellationToken);
                        lockedSeats.Add(seatId);
                    }
                }

                // 3. Rollback or Commit Transaction Phase
                if (failedSeats.Any())
                {
                    _logger.LogError("Atomic multiple seat locking failed. Rolling back already locked seats: {RollbackSeats}", string.Join(",", lockedSeats));
                    
                    foreach (var seatId in lockedSeats)
                    {
                        var key = GetSeatKey(showtimeId, seatId);
                        if (_redisDb != null)
                        {
                            await _redisDb.KeyDeleteAsync(key);
                        }
                        else
                        {
                            await _cache.RemoveAsync(key, cancellationToken);
                        }
                    }

                    return new SeatLockResult
                    {
                        Success = false,
                        Message = "Failed to acquire locks atomically. Rolled back.",
                        FailedSeats = failedSeats
                    };
                }

                // 4. Update Index and Broadcast Bulk Event
                await UpdateAllSeatsIndexAsync(showtimeId, lockedSeats, true, false, cancellationToken);
                await NotifySeatsLockedBulkAsync(showtimeId, lockedSeats, userId, sessionId, cancellationToken);

                return new SeatLockResult
                {
                    Success = true,
                    Message = "All requested seats locked successfully.",
                    LockedSeats = lockedSeats
                };
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Unlocks multiple seats in bulk.
        /// </summary>
        public async Task<SeatLockResult> UnlockMultipleSeatsAsync(int showtimeId, List<int> seatIds, string userId, string sessionId, bool force = false, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Unlocking multiple seats {Seats} for Showtime {ShowtimeId} User {UserId} Session {SessionId} (Force: {Force})", string.Join(",", seatIds), showtimeId, userId, sessionId, force);

            var success = true;
            var unlockedSeats = new List<int>();
            var failedSeats = new List<int>();

            foreach (var seatId in seatIds)
            {
                var singleResult = await UnlockSeatAsync(showtimeId, seatId, userId, sessionId, force, cancellationToken);
                if (singleResult.Success)
                {
                    unlockedSeats.Add(seatId);
                }
                else
                {
                    success = false;
                    failedSeats.Add(seatId);
                }
            }

            if (unlockedSeats.Any())
            {
                await NotifySeatsReleasedBulkAsync(showtimeId, unlockedSeats, userId, "seat_unlocked", cancellationToken);
            }

            return new SeatLockResult
            {
                Success = success,
                Message = success ? "All seats unlocked." : "Some seats failed to unlock.",
                LockedSeats = unlockedSeats,
                FailedSeats = failedSeats
            };
        }

        /// <summary>
        /// Returns a list of active locked seats for a showtime, cleaning up expired locks.
        /// </summary>
        public async Task<List<int>> GetLockedSeatsAsync(int showtimeId, CancellationToken cancellationToken = default)
        {
            var listKey = GetShowtimeAllSeatsKey(showtimeId);
            string? json;
            if (_redisDb != null)
            {
                json = await _redisDb.StringGetAsync(listKey);
            }
            else
            {
                json = await _cache.GetStringAsync(listKey, cancellationToken);
            }

            if (json == null) return new List<int>();

            var seatIds = JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
            var activeSeats = new List<int>();
            var expiredSeats = new List<int>();

            foreach (var seatId in seatIds)
            {
                var lockInfo = await GetSeatLockInfoAsync(showtimeId, seatId, cancellationToken);
                if (lockInfo != null && lockInfo.ExpiredAt > DateTime.UtcNow)
                {
                    activeSeats.Add(seatId);
                }
                else
                {
                    expiredSeats.Add(seatId);
                }
            }

            // Cleanup expired entries from index
            if (expiredSeats.Any())
            {
                await UpdateAllSeatsIndexAsync(showtimeId, expiredSeats, false, true, cancellationToken);
            }

            return activeSeats;
        }

        /// <summary>
        /// Retrieves seat lock information metadata.
        /// </summary>
        public async Task<SeatLockInfo?> GetSeatLockInfoAsync(int showtimeId, int seatId, CancellationToken cancellationToken = default)
        {
            var key = GetSeatKey(showtimeId, seatId);
            string? json;
            if (_redisDb != null)
            {
                json = await _redisDb.StringGetAsync(key);
            }
            else
            {
                json = await _cache.GetStringAsync(key, cancellationToken);
            }

            if (json == null) return null;

            try
            {
                return JsonSerializer.Deserialize<SeatLockInfo>(json);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize seat lock info.");
                return null;
            }
        }

        /// <summary>
        /// Checks if a seat is currently locked.
        /// </summary>
        public async Task<bool> IsSeatLockedAsync(int showtimeId, int seatId, CancellationToken cancellationToken = default)
        {
            var lockInfo = await GetSeatLockInfoAsync(showtimeId, seatId, cancellationToken);
            return lockInfo != null && lockInfo.ExpiredAt > DateTime.UtcNow;
        }

        /// <summary>
        /// Validates that a given user owns the lock.
        /// </summary>
        public async Task<bool> ValidateSeatOwnershipAsync(int showtimeId, int seatId, string userId, string sessionId, CancellationToken cancellationToken = default)
        {
            var lockInfo = await GetSeatLockInfoAsync(showtimeId, seatId, cancellationToken);
            return lockInfo != null && lockInfo.UserId == userId && lockInfo.SessionId == sessionId && lockInfo.ExpiredAt > DateTime.UtcNow;
        }

        /// <summary>
        /// Cleans up expired locks and triggers expiration event broadcasts.
        /// </summary>
        public async Task ReleaseExpiredLocksAsync(int showtimeId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Scanning for expired seat locks on Showtime {ShowtimeId}", showtimeId);
            var listKey = GetShowtimeAllSeatsKey(showtimeId);
            string? json;
            if (_redisDb != null)
            {
                json = await _redisDb.StringGetAsync(listKey);
            }
            else
            {
                json = await _cache.GetStringAsync(listKey, cancellationToken);
            }

            if (json == null) return;

            var seatIds = JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
            var activeSeats = new List<int>();
            var expiredSeats = new List<int>();

            foreach (var seatId in seatIds)
            {
                var lockInfo = await GetSeatLockInfoAsync(showtimeId, seatId, cancellationToken);
                if (lockInfo == null || lockInfo.ExpiredAt <= DateTime.UtcNow)
                {
                    expiredSeats.Add(seatId);
                }
                else
                {
                    activeSeats.Add(seatId);
                }
            }

            if (expiredSeats.Any())
            {
                _logger.LogInformation("Expiring seat locks {Seats} on Showtime {ShowtimeId}", string.Join(",", expiredSeats), showtimeId);

                // 1. Remove expired cache keys
                foreach (var seatId in expiredSeats)
                {
                    var key = GetSeatKey(showtimeId, seatId);
                    if (_redisDb != null)
                    {
                        await _redisDb.KeyDeleteAsync(key);
                    }
                    else
                    {
                        await _cache.RemoveAsync(key, cancellationToken);
                    }
                }

                // 2. Remove from index
                await UpdateAllSeatsIndexAsync(showtimeId, expiredSeats, false, true, cancellationToken);

                // 3. Broadcast bulk release event
                await NotifySeatsReleasedBulkAsync(showtimeId, expiredSeats, null!, "seat_expired", cancellationToken);
            }
        }

        /// <summary>
        /// Backward compatibility interface.
        /// </summary>
        public async Task<string?> GetSeatLockOwnerAsync(int showtimeId, int seatId, CancellationToken cancellationToken = default)
        {
            var info = await GetSeatLockInfoAsync(showtimeId, seatId, cancellationToken);
            return info?.UserId;
        }

        /// <summary>
        /// Extends the expiration (TTL) of the specified seat locks for the current owner.
        /// </summary>
        public async Task<SeatLockResult> RefreshSeatLockAsync(int showtimeId, List<int> seatIds, string userId, string sessionId, int minutes = 5, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Refreshing TTL of seat locks on Showtime {ShowtimeId} for User {UserId} Session {SessionId}", showtimeId, userId, sessionId);

            var refreshedSeats = new List<int>();
            var failedSeats = new List<int>();
            var now = DateTime.UtcNow;
            var expiry = TimeSpan.FromMinutes(minutes);

            var semaphore = GetShowtimeLock(showtimeId);
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                foreach (var seatId in seatIds)
                {
                    var isOwner = await ValidateSeatOwnershipAsync(showtimeId, seatId, userId, sessionId, cancellationToken);
                    if (!isOwner)
                    {
                        failedSeats.Add(seatId);
                        continue;
                    }

                    var lockInfo = new SeatLockInfo
                    {
                        ShowtimeId = showtimeId,
                        SeatId = seatId,
                        UserId = userId,
                        LockedAt = now,
                        ExpiredAt = now.AddMinutes(minutes),
                        SessionId = sessionId
                    };

                    var key = GetSeatKey(showtimeId, seatId);
                    var json = JsonSerializer.Serialize(lockInfo);

                    if (_redisDb != null)
                    {
                        await _redisDb.StringSetAsync(key, json, expiry);
                    }
                    else
                    {
                        await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions { AbsoluteExpiration = lockInfo.ExpiredAt }, cancellationToken);
                    }

                    refreshedSeats.Add(seatId);
                }

                if (failedSeats.Any())
                {
                    return new SeatLockResult
                    {
                        Success = false,
                        Message = "Some seats failed to refresh because you do not own the lock in this session.",
                        LockedSeats = refreshedSeats,
                        FailedSeats = failedSeats
                    };
                }

                return new SeatLockResult
                {
                    Success = true,
                    Message = "Seat locks refreshed successfully.",
                    LockedSeats = refreshedSeats
                };
            }
            finally
            {
                semaphore.Release();
            }
        }

        #endregion

        #region Private Helpers

        private async Task UpdateAllSeatsIndexAsync(int showtimeId, List<int> seatIds, bool isAdd, bool acquireLock, CancellationToken cancellationToken = default)
        {
            var listKey = GetShowtimeAllSeatsKey(showtimeId);
            SemaphoreSlim? semaphore = null;
            if (acquireLock)
            {
                semaphore = GetShowtimeLock(showtimeId);
                await semaphore.WaitAsync(cancellationToken);
            }
            try
            {
                string? json;
                if (_redisDb != null)
                {
                    json = await _redisDb.StringGetAsync(listKey);
                }
                else
                {
                    json = await _cache.GetStringAsync(listKey, cancellationToken);
                }

                var currentIds = json != null ? JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>() : new List<int>();
                
                bool changed = false;
                foreach (var seatId in seatIds)
                {
                    if (isAdd)
                    {
                        if (!currentIds.Contains(seatId))
                        {
                            currentIds.Add(seatId);
                            changed = true;
                        }
                    }
                    else
                    {
                        if (currentIds.Contains(seatId))
                        {
                            currentIds.Remove(seatId);
                            changed = true;
                        }
                    }
                }
                
                if (changed)
                {
                    var newJson = JsonSerializer.Serialize(currentIds);
                    if (_redisDb != null)
                    {
                        await _redisDb.StringSetAsync(listKey, newJson);
                    }
                    else
                    {
                        await _cache.SetStringAsync(listKey, newJson, cancellationToken);
                    }
                }
            }
            finally
            {
                if (semaphore != null)
                {
                    semaphore.Release();
                }
            }
        }

        private async Task AddSeatToAllSeatsListAsync(int showtimeId, int seatId, CancellationToken cancellationToken = default)
        {
            await UpdateAllSeatsIndexAsync(showtimeId, new List<int> { seatId }, true, false, cancellationToken);
        }

        private async Task RemoveSeatFromAllSeatsListAsync(int showtimeId, int seatId, CancellationToken cancellationToken = default)
        {
            await UpdateAllSeatsIndexAsync(showtimeId, new List<int> { seatId }, false, true, cancellationToken);
        }

        private async Task NotifySeatLockedAsync(int showtimeId, int seatId, string userId, string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var groupName = $"Showtime_{showtimeId}";
                await _hubContext.Clients.Group(groupName).SendAsync("SeatLocked", showtimeId, seatId, userId, sessionId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast single seat locked SignalR notification.");
            }
        }

        private async Task NotifySeatReleasedAsync(int showtimeId, int seatId, string eventName, CancellationToken cancellationToken = default)
        {
            try
            {
                var groupName = $"Showtime_{showtimeId}";
                await _hubContext.Clients.Group(groupName).SendAsync("SeatReleased", showtimeId, seatId, eventName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast single seat released SignalR notification.");
            }
        }

        private async Task NotifySeatsLockedBulkAsync(int showtimeId, List<int> seatIds, string userId, string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var groupName = $"Showtime_{showtimeId}";
                await _hubContext.Clients.Group(groupName).SendAsync("SeatsLocked", new
                {
                    showtimeId,
                    seatIds,
                    userId,
                    sessionId
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast bulk seats locked SignalR notification.");
            }
        }

        private async Task NotifySeatsReleasedBulkAsync(int showtimeId, List<int> seatIds, string userId, string reason, CancellationToken cancellationToken = default)
        {
            try
            {
                var groupName = $"Showtime_{showtimeId}";
                await _hubContext.Clients.Group(groupName).SendAsync("SeatsReleased", new
                {
                    showtimeId,
                    seatIds,
                    userId,
                    reason
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast bulk seats released SignalR notification.");
            }
        }

        #endregion
    }
}
