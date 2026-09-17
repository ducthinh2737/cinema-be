using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Application.Common.Interfaces;
using CinemaBooking.API.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.API.Infrastructure.Locking
{
    public class SqlDistributedLock : IDistributedLock
    {
        private readonly CinemaDbContext _dbContext;
        private readonly ILogger<SqlDistributedLock> _logger;

        public SqlDistributedLock(CinemaDbContext dbContext, ILogger<SqlDistributedLock> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IDisposable> AcquireLockAsync(string resourceKey, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            var start = DateTime.UtcNow;
            var attempt = 0;
            var delayMs = 50; // Initial delay for backoff

            while (DateTime.UtcNow - start < timeout)
            {
                attempt++;
                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            DECLARE @result INT;
                            EXEC @result = sp_getapplock 
                                @Resource = @Resource, 
                                @LockMode = 'Exclusive', 
                                @LockOwner = 'Session', 
                                @LockTimeout = 0; -- Do not block here, we handle backoff retry manually
                            SELECT @result;";

                        var resourceParam = new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = resourceKey };
                        command.Parameters.Add(resourceParam);

                        var resultObj = await command.ExecuteScalarAsync(cancellationToken);
                        var lockResult = (resultObj as int?) ?? -1;

                        if (lockResult >= 0)
                        {
                            _logger.LogInformation("Successfully acquired SQL app lock for '{ResourceKey}' on attempt {Attempt}.", resourceKey, attempt);
                            return new SqlLockReleaseTicket(connection, resourceKey, _logger);
                        }

                        _logger.LogWarning("SQL Lock collision on '{ResourceKey}', attempt {Attempt} failed. Retrying in {DelayMs}ms.", resourceKey, attempt, delayMs);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while acquiring SQL app lock for '{ResourceKey}' on attempt {Attempt}.", resourceKey, attempt);
                }

                // Exponential backoff
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
                delayMs = Math.Min(delayMs * 2, 1000); // Caps backoff delay at 1 second
            }

            _logger.LogError("Failed to acquire lock for '{ResourceKey}' after timeout of {TimeoutMs}ms.", resourceKey, timeout.TotalMilliseconds);
            throw new BusinessException($"Phòng chiếu đang bận xếp lịch (Lock Timeout: {resourceKey}). Vui lòng thử lại sau.");
        }

        private class SqlLockReleaseTicket : IDisposable
        {
            private readonly IDbConnection _connection;
            private readonly string _resourceKey;
            private readonly ILogger _logger;
            private bool _disposed;

            public SqlLockReleaseTicket(IDbConnection connection, string resourceKey, ILogger logger)
            {
                _connection = connection;
                _resourceKey = resourceKey;
                _logger = logger;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                try
                {
                    if (_connection.State == ConnectionState.Open)
                    {
                        using (var command = _connection.CreateCommand())
                        {
                            command.CommandText = "EXEC sp_releaseapplock @Resource = @Resource, @LockOwner = 'Session';";
                            var resourceParam = new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = _resourceKey };
                            command.Parameters.Add(resourceParam);
                            command.ExecuteNonQuery();
                        }
                        _logger.LogInformation("Released SQL lock for '{ResourceKey}' successfully.", _resourceKey);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to release SQL lock for '{ResourceKey}'.", _resourceKey);
                }
            }
        }
    }
}
