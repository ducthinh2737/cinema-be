using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.Services.Interfaces;
using CinemaBooking.API.Application.Common.Interfaces;
using CinemaBooking.API.Infrastructure.Outbox;

namespace CinemaBooking.API.Services.Implementations.BatchShowtimes
{
    public class BatchShowtimeService : IBatchShowtimeService
    {
        private readonly CinemaDbContext _context;
        private readonly ShowtimeBatchValidator _validator;
        private readonly ConflictDetectionService _conflictService;
        private readonly BatchShowtimeEngine _schedulingEngine;
        private readonly ILogger<BatchShowtimeService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IDistributedLock _distributedLock;

        public BatchShowtimeService(
            CinemaDbContext context,
            ShowtimeBatchValidator validator,
            ConflictDetectionService conflictService,
            BatchShowtimeEngine schedulingEngine,
            ILogger<BatchShowtimeService> logger,
            IMemoryCache cache,
            IDistributedLock distributedLock)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _conflictService = conflictService ?? throw new ArgumentNullException(nameof(conflictService));
            _schedulingEngine = schedulingEngine ?? throw new ArgumentNullException(nameof(schedulingEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _distributedLock = distributedLock ?? throw new ArgumentNullException(nameof(distributedLock));
        }

        public async Task<ShowtimeBatchResponseDto> CreateShowtimeBatchAsync(
            ShowtimeBatchRequestDto batchDto, 
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            int dbQueries = 0;
            var response = new ShowtimeBatchResponseDto();

            // 0. Idempotency Check
            if (!string.IsNullOrEmpty(batchDto.IdempotencyKey))
            {
                var cacheKey = $"idempotency:batch_showtimes:{batchDto.IdempotencyKey}";
                if (_cache.TryGetValue<ShowtimeBatchResponseDto>(cacheKey, out var cachedResponse))
                {
                    _logger.LogInformation("Returning cached response for idempotency key: {IdempotencyKey}", batchDto.IdempotencyKey);
                    return cachedResponse!;
                }
            }

            // 1. Validate domain prerequisites and load objects
            var (movie, hall, price, validationError, valQueries) = await _validator.ValidatePrerequisitesAsync(batchDto, cancellationToken);
            dbQueries += valQueries;

            if (validationError != null)
            {
                stopwatch.Stop();
                response.Success = false;
                response.PerformanceMetrics = new PerformanceMetricsDto
                {
                    DbQueries = dbQueries,
                    ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds
                };
                response.Conflicts.Add(new ConflictDetailDto
                {
                    StartTime = batchDto.StartDate,
                    EndTime = batchDto.EndDate,
                    Reason = validationError
                });
                return response;
            }

            // 2. Acquire Distributed App Lock on the Hall
            var lockKey = $"lock:hall:{batchDto.HallId}";
            using (await _distributedLock.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(15), cancellationToken))
            {
                // 3. Start Database Transaction
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // 4. Load active timeline planner for the target date range in a single query
                    var (planner, planQueries) = await _conflictService.LoadHallPlannerAsync(
                        batchDto.HallId, 
                        batchDto.StartDate, 
                        batchDto.EndDate, 
                        cancellationToken);
                    dbQueries += planQueries;

                    // 5. Generate showtime candidates based on mode using the new engine
                    var (candidates, skipped, conflicts) = _schedulingEngine.GenerateShowtimes(batchDto, movie, hall.Cinema, planner);
                    response.SkippedSlots = skipped;
                    response.Conflicts = conflicts;

                    // 6. In Manual mode, if there is even one conflict, abort and rollback transaction
                    if (batchDto.Mode.Equals("Manual", StringComparison.OrdinalIgnoreCase) && conflicts.Any())
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        stopwatch.Stop();
                        response.Success = false;
                        response.PerformanceMetrics = new PerformanceMetricsDto
                        {
                            DbQueries = dbQueries,
                            ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds
                        };
                        return response;
                    }

                    // If no candidates generated
                    if (!candidates.Any())
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        stopwatch.Stop();
                        response.Success = false;
                        response.PerformanceMetrics = new PerformanceMetricsDto
                        {
                            DbQueries = dbQueries,
                            ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds
                        };
                        return response;
                    }

                    // 7. Map candidates to entities and batch insert
                    var entitiesToInsert = new List<Showtime>();
                    foreach (var cand in candidates)
                    {
                        entitiesToInsert.Add(new Showtime
                        {
                            MovieId = batchDto.MovieId,
                            HallId = batchDto.HallId,
                            PriceId = batchDto.PriceId,
                            StartTime = cand.StartTime,
                            EndTime = cand.EndTime
                        });
                    }

                    await _context.Showtimes.AddRangeAsync(entitiesToInsert, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    dbQueries++;

                    // 8. Map to result DTOs and queue outbox notifications
                    var resultDtos = new List<ShowtimeDto>();
                    foreach (var entity in entitiesToInsert)
                    {
                        var dto = MapToShowtimeDto(entity, movie, hall, price);
                        resultDtos.Add(dto);

                        // Queue Outbox Event in the same transaction
                        var outboxEvent = new OutboxEvent
                        {
                            Id = Guid.NewGuid(),
                            EventName = ShowtimeConstants.SignalREvents.ShowtimeCreated,
                            Payload = System.Text.Json.JsonSerializer.Serialize(dto),
                            OccurredOn = DateTime.UtcNow
                        };
                        await _context.OutboxEvents.AddAsync(outboxEvent, cancellationToken);
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    stopwatch.Stop();
                    response.Success = true;
                    response.CreatedShowtimes = resultDtos;
                    response.PerformanceMetrics = new PerformanceMetricsDto
                    {
                        DbQueries = dbQueries,
                        ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds
                    };

                    // Cache for Idempotency
                    if (!string.IsNullOrEmpty(batchDto.IdempotencyKey))
                    {
                        var cacheKey = $"idempotency:batch_showtimes:{batchDto.IdempotencyKey}";
                        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(10));
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Error occurred during Showtime Batch Creation process.");
                    stopwatch.Stop();
                    response.Success = false;
                    response.PerformanceMetrics = new PerformanceMetricsDto
                    {
                        DbQueries = dbQueries,
                        ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds
                    };
                    response.Conflicts.Add(new ConflictDetailDto
                    {
                        StartTime = batchDto.StartDate,
                        EndTime = batchDto.EndDate,
                        Reason = $"Lỗi hệ thống: {ex.Message}"
                    });
                    return response;
                }
            }
        }

        private ShowtimeDto MapToShowtimeDto(Showtime showtime, Models.Movies.Movie movie, Models.Cinemas.Hall hall, Price price)
        {
            return new ShowtimeDto
            {
                ShowtimeId = showtime.ShowtimeId,
                MovieId = showtime.MovieId,
                MovieTitle = movie.Title,
                HallId = showtime.HallId,
                HallName = hall.HallName,
                CinemaId = hall.CinemaId,
                CinemaName = hall.Cinema?.CinemaName ?? "",
                PriceId = showtime.PriceId,
                PriceValue = price.Value,
                TicketType = price.TicketType,
                StartTime = showtime.StartTime,
                EndTime = showtime.EndTime,
                AvailableSeats = hall.Capacity,
                TotalSeats = hall.Capacity,
                Status = "Upcoming"
            };
        }
    }
}
