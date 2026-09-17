using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.DTOs.Movies;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.Models.Cinemas;
using CinemaBooking.API.Services.Interfaces;
using CinemaBooking.API.Domain.Exceptions;
using CinemaBooking.API.Domain.Policies;
using CinemaBooking.API.Application.Common.Interfaces;
using CinemaBooking.API.Infrastructure.Outbox;

namespace CinemaBooking.API.Services.Implementations
{
    public static class ShowtimeConstants
    {
        public static class Statuses
        {
            public const string Upcoming = "Upcoming";
            public const string NowShowing = "NowShowing";
            public const string Ended = "Ended";
        }

        public static class SignalREvents
        {
            public const string ShowtimeCreated = "ShowtimeCreated";
            public const string ShowtimeUpdated = "ShowtimeUpdated";
            public const string ShowtimeDeleted = "ShowtimeDeleted";
            public const string BulkShowtimesCreated = "BulkShowtimesCreated";
        }
    }

    public static class BookingConstants
    {
        public const string Cancelled = "Cancelled";
    }

    public class ShowtimeService : IShowtimeService
    {
        private readonly CinemaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ShowtimeService> _logger;
        private readonly ISeatLockService _seatLockService;
        private readonly IDistributedLock _distributedLock;
        private readonly TimeProvider _timeProvider;

        public ShowtimeService(
            CinemaDbContext context,
            IMapper mapper,
            ILogger<ShowtimeService> logger,
            ISeatLockService seatLockService,
            IDistributedLock distributedLock,
            TimeProvider? timeProvider = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _seatLockService = seatLockService ?? throw new ArgumentNullException(nameof(seatLockService));
            _distributedLock = distributedLock ?? throw new ArgumentNullException(nameof(distributedLock));
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        #region Public Interface Query Methods

        public async Task<ApiResponse<PagedResultDto<ShowtimeDto>>> GetPagedShowtimesAsync(
            ShowtimeQueryParameters queryParams, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving paginated showtimes for Page Number: {PageNumber}", queryParams.PageNumber);

            var query = _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Hall).ThenInclude(h => h.HallType)
                .Include(s => s.Price)
                .AsNoTracking();

            // Apply Filters
            if (queryParams.CinemaId.HasValue)
            {
                query = query.Where(s => s.Hall.CinemaId == queryParams.CinemaId.Value);
            }

            if (queryParams.MovieId.HasValue)
            {
                query = query.Where(s => s.MovieId == queryParams.MovieId.Value);
            }

            if (queryParams.Date.HasValue)
            {
                var targetDate = queryParams.Date.Value.Date;
                var nextDate = targetDate.AddDays(1);
                query = query.Where(s => s.StartTime >= targetDate && s.StartTime < nextDate);
            }

            // Filter by dynamic Status using TimeProvider abstraction
            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                query = queryParams.Status.ToLower() switch
                {
                    "upcoming" => query.Where(s => s.StartTime > now),
                    "nowshowing" => query.Where(s => s.StartTime <= now && s.EndTime >= now),
                    "ended" => query.Where(s => s.EndTime < now),
                    _ => query
                };
            }

            // Search support
            if (!string.IsNullOrEmpty(queryParams.SearchTerm))
            {
                var search = queryParams.SearchTerm;
                query = query.Where(s => s.Movie.Title.Contains(search) || s.Hall.HallName.Contains(search));
            }

            // Sorting
            if (!string.IsNullOrEmpty(queryParams.SortBy))
            {
                query = queryParams.SortBy.ToLower() switch
                {
                    "starttime" => queryParams.IsDescending ? query.OrderByDescending(s => s.StartTime) : query.OrderBy(s => s.StartTime),
                    "endtime" => queryParams.IsDescending ? query.OrderByDescending(s => s.EndTime) : query.OrderBy(s => s.EndTime),
                    "movie" => queryParams.IsDescending ? query.OrderByDescending(s => s.Movie.Title) : query.OrderBy(s => s.Movie.Title),
                    _ => queryParams.IsDescending ? query.OrderByDescending(s => s.ShowtimeId) : query.OrderBy(s => s.ShowtimeId)
                };
            }
            else
            {
                query = query.OrderBy(s => s.StartTime);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = await MapShowtimesToDtosAsync(items, cancellationToken);

            var pagedResult = new PagedResultDto<ShowtimeDto>
            {
                Items = dtos,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse.Success(pagedResult);
        }

        public async Task<ApiResponse<ShowtimeDto>> GetShowtimeByIdAsync(
            int id, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving showtime by ID: {ShowtimeId}", id);

            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Hall).ThenInclude(h => h.HallType)
                .Include(s => s.Price)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShowtimeId == id, cancellationToken);

            if (showtime == null)
            {
                throw new NotFoundException($"Không tìm thấy suất chiếu có ID {id}.");
            }

            var dtos = await MapShowtimesToDtosAsync(new List<Showtime> { showtime }, cancellationToken);
            return ApiResponse.Success(dtos.First());
        }

        public async Task<ApiResponse<IEnumerable<ShowtimeDto>>> GetShowtimesByMovieIdAsync(
            int movieId, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving showtimes for Movie ID: {MovieId}", movieId);

            var today = DateTime.UtcNow.Date;
            var showtimes = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Hall).ThenInclude(h => h.HallType)
                .Include(s => s.Price)
                .Where(s => s.MovieId == movieId && s.StartTime >= today)
                .OrderBy(s => s.StartTime)
                .Take(100)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var dtos = await MapShowtimesToDtosAsync(showtimes, cancellationToken);
            return ApiResponse.Success<IEnumerable<ShowtimeDto>>(dtos);
        }

        public async Task<ApiResponse<IEnumerable<ShowtimeDto>>> GetShowtimesByCinemaIdAsync(
            int cinemaId, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving showtimes for Cinema ID: {CinemaId}", cinemaId);

            var today = DateTime.UtcNow.Date;
            var showtimes = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Hall).ThenInclude(h => h.HallType)
                .Include(s => s.Price)
                .Where(s => s.Hall.CinemaId == cinemaId && s.StartTime >= today)
                .OrderBy(s => s.StartTime)
                .Take(100)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var dtos = await MapShowtimesToDtosAsync(showtimes, cancellationToken);
            return ApiResponse.Success<IEnumerable<ShowtimeDto>>(dtos);
        }

        #endregion

        #region Public Mutation Methods

        public async Task<ApiResponse<ShowtimeDto>> CreateShowtimeAsync(
            ShowtimeCreateDto createDto, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating showtime in Hall {HallId} at {StartTime}", createDto.HallId, createDto.StartTime);

            var movie = await ValidateAndLoadShowtimePrerequisitesAsync(createDto, cancellationToken);
            var endTime = ShowtimeSchedulingPolicy.CalculateEndTime(movie, createDto.StartTime);

            var lockKey = $"lock:hall:{createDto.HallId}";
            using (await _distributedLock.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10), cancellationToken))
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Check conflict safely within locked transaction using Domain Policy logic
                    var conflictExists = await CheckOverlappingScheduleAsync(createDto.HallId, createDto.StartTime, endTime, null, cancellationToken);
                    if (conflictExists)
                    {
                        throw new BusinessException("Lịch chiếu bị trùng hoặc quá gần lịch chiếu khác trong sảnh này (yêu cầu khoảng dọn dẹp 15 phút).");
                    }

                    var showtime = _mapper.Map<Showtime>(createDto);
                    showtime.EndTime = endTime;

                    await _context.Showtimes.AddAsync(showtime, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    var savedShowtime = await _context.Showtimes
                        .Include(s => s.Movie)
                        .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                        .Include(s => s.Hall).ThenInclude(h => h.HallType)
                        .Include(s => s.Price)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.ShowtimeId == showtime.ShowtimeId, cancellationToken);

                    var dtos = await MapShowtimesToDtosAsync(new List<Showtime> { savedShowtime ?? showtime }, cancellationToken);
                    var resultDto = dtos.First();

                    // Queue Outbox Event in the same transaction
                    var outboxEvent = new OutboxEvent
                    {
                        Id = Guid.NewGuid(),
                        EventName = ShowtimeConstants.SignalREvents.ShowtimeCreated,
                        Payload = System.Text.Json.JsonSerializer.Serialize(resultDto),
                        OccurredOn = DateTime.UtcNow
                    };
                    await _context.OutboxEvents.AddAsync(outboxEvent, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Showtime created successfully with ID {ShowtimeId}", showtime.ShowtimeId);
                    return ApiResponse.Success(resultDto);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Error occurred during showtime creation for Hall ID {HallId}.", createDto.HallId);
                    throw;
                }
            }
        }

        public async Task<ApiResponse<string>> CreateBulkShowtimesAsync(
            ShowtimeBulkCreateDto bulkDto,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Bulk creating showtimes for Hall {HallId}, Movie {MovieId}", bulkDto.HallId, bulkDto.MovieId);

            // Fetch prerequisites: Movie, Hall, Price
            var movie = await _context.Movies
                .Include(m => m.MovieFormats)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == bulkDto.MovieId && !m.IsDeleted, cancellationToken);
            if (movie == null)
            {
                throw new ValidationException($"Phim có ID {bulkDto.MovieId} không tồn tại hoặc đã bị ẩn.");
            }

            var hall = await _context.Halls
                .Include(h => h.Cinema)
                .Include(h => h.HallType)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HallId == bulkDto.HallId, cancellationToken);
            if (hall == null || hall.IsDeleted)
            {
                throw new ValidationException($"Sảnh chiếu có ID {bulkDto.HallId} không tồn tại hoặc đã bị ẩn.");
            }

            if (hall.Cinema == null || hall.Cinema.IsDeleted)
            {
                throw new ValidationException("Rạp chiếu của sảnh này hiện tại đang bị dừng hoạt động.");
            }

            var price = await _context.Prices.AsNoTracking().FirstOrDefaultAsync(p => p.PriceId == bulkDto.PriceId, cancellationToken);
            if (price == null)
            {
                throw new ValidationException($"Mức giá cấu hình có ID {bulkDto.PriceId} không tồn tại.");
            }

            // Validate format compatibility
            string format = "2D"; // Default fallback
            if (!string.IsNullOrEmpty(price.TicketType))
            {
                if (price.TicketType.Trim().StartsWith("{"))
                {
                    try
                    {
                        var obj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(price.TicketType);
                        if (obj != null && obj.TryGetValue("roomType", out var roomType))
                        {
                            format = roomType;
                        }
                    }
                    catch
                    {
                        // Fallback
                    }
                }
                
                if (format == "2D" || format == "Standard" || format == "VIP")
                {
                    format = "2D";
                    string upperType = price.TicketType.ToUpper();
                    if (upperType.Contains("IMAX")) format = "IMAX";
                    else if (upperType.Contains("3D")) format = "3D";
                }
            }

            var supportedFormats = new List<string>();
            if (!string.IsNullOrEmpty(hall.Description) && hall.Description.Trim().StartsWith("{"))
            {
                try
                {
                    var obj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(hall.Description);
                    if (obj != null && obj.TryGetValue("supportedFormats", out var formatsObj) && formatsObj is System.Text.Json.JsonElement elem && elem.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in elem.EnumerateArray())
                        {
                            if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                supportedFormats.Add(item.GetString()!);
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback
                }
            }

            if (supportedFormats.Count == 0)
            {
                string typeName = (hall.HallType?.TypeName ?? "").ToUpper();
                if (typeName.Contains("IMAX"))
                {
                    supportedFormats.AddRange(new[] { "IMAX", "3D", "2D" });
                }
                else
                {
                    supportedFormats.AddRange(new[] { "2D", "3D" });
                }
            }

            if (movie.MovieFormats != null && movie.MovieFormats.Any())
            {
                var movieHasFormat = movie.MovieFormats.Any(f => f.FormatName.Equals(format, StringComparison.OrdinalIgnoreCase));
                if (!movieHasFormat)
                {
                    throw new ValidationException($"Phim '{movie.Title}' không hỗ trợ định dạng {format}.");
                }
            }

            var hallHasFormat = supportedFormats.Any(f => f.Equals(format, StringComparison.OrdinalIgnoreCase));
            if (!hallHasFormat)
            {
                throw new ValidationException($"Phòng chiếu '{hall.HallName}' không hỗ trợ định dạng {format}.");
            }

            var lockKey = $"lock:hall:{bulkDto.HallId}";
            using (await _distributedLock.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(15), cancellationToken))
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Fetch existing active showtimes for this hall
                    var existingShowtimes = await _context.Showtimes
                        .AsNoTracking()
                        .Where(s => s.HallId == bulkDto.HallId)
                        .Select(s => new { s.StartTime, s.EndTime })
                        .ToListAsync(cancellationToken);

                    var batchList = new List<Showtime>();
                    int skippedCount = 0;
                    var localTimeZone = ShowtimeSchedulingPolicy.LocalTimeZone;
                    var skippedReasons = new List<string>();

                    foreach (var date in bulkDto.Dates)
                    {
                        foreach (var slotStr in bulkDto.TimeSlots)
                        {
                            TimeSpan timeSpan;
                            if (!TimeSpan.TryParse(slotStr, out timeSpan))
                            {
                                var parts = slotStr.Split(':');
                                if (parts.Length >= 2 && int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes))
                                {
                                    int seconds = 0;
                                    if (parts.Length > 2) int.TryParse(parts[2], out seconds);
                                    timeSpan = new TimeSpan(hours, minutes, seconds);
                                }
                                else
                                {
                                    _logger.LogWarning("Invalid time slot string: {TimeSlot}", slotStr);
                                    skippedCount++;
                                    continue;
                                }
                            }

                            // Convert to Vietnam local timezone date component
                            var localDate = date.Kind == DateTimeKind.Utc
                                ? TimeZoneInfo.ConvertTimeFromUtc(date, localTimeZone).Date
                                : date.Date;

                            var startLocal = localDate.Add(timeSpan);
                            var startTimeUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), localTimeZone);

                            if (startTimeUtc <= _timeProvider.GetUtcNow().UtcDateTime)
                            {
                                _logger.LogWarning("Skipping time slot in the past: {StartTimeUtc}", startTimeUtc);
                                skippedCount++;
                                continue;
                            }

                            var endTime = ShowtimeSchedulingPolicy.CalculateEndTime(movie, startTimeUtc);

                            // Validate release window and operating hours
                            try
                            {
                                ShowtimeSchedulingPolicy.ValidateReleaseWindow(movie, startTimeUtc);
                                if (hall.Cinema != null)
                                {
                                    ShowtimeSchedulingPolicy.ValidateOperatingHours(startTimeUtc, endTime, hall.Cinema);
                                }
                            }
                            // BẮT LỖI GIỜ HOẠT ĐỘNG: Ngắt hẳn tiến trình sinh lịch của ngày này nếu vượt quá giờ đóng cửa rạp
                            catch (ValidationException ex) when (ex.Message.Contains("hoạt động") || ex.Message.Contains("đóng cửa"))
                            {
                                _logger.LogInformation("Dừng xếp lịch tự động cho chuỗi ngày này do vi phạm giờ đóng cửa: {Msg}", ex.Message);
                                skippedCount++;
                                if (!skippedReasons.Contains(ex.Message))
                                {
                                    skippedReasons.Add(ex.Message);
                                }
                                break; 
                            }
                            catch (ValidationException ex)
                            {
                                _logger.LogWarning(ex, "Skipping slot due to validation: {StartTimeUtc}", startTimeUtc);
                                skippedCount++;
                                if (!skippedReasons.Contains(ex.Message))
                                {
                                    skippedReasons.Add(ex.Message);
                                }
                                continue;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Skipping slot due to policy violation: {StartTimeUtc}", startTimeUtc);
                                skippedCount++;
                                if (!skippedReasons.Contains(ex.Message))
                                {
                                    skippedReasons.Add(ex.Message);
                                }
                                continue;
                            }

                            // Conflict overlap validation
                            bool hasConflict = false;
                            foreach (var existing in existingShowtimes)
                            {
                                if (ConflictPolicy.Overlaps(startTimeUtc, endTime, existing.StartTime, existing.EndTime, ShowtimeSchedulingPolicy.CleanUpBufferMinutes))
                                {
                                    hasConflict = true;
                                    break;
                                }
                            }

                            if (!hasConflict)
                            {
                                foreach (var batchItem in batchList)
                                {
                                    if (ConflictPolicy.Overlaps(startTimeUtc, endTime, batchItem.StartTime, batchItem.EndTime, ShowtimeSchedulingPolicy.CleanUpBufferMinutes))
                                    {
                                        hasConflict = true;
                                        break;
                                    }
                                }
                            }

                            if (hasConflict)
                            {
                                _logger.LogWarning("Skipping slot due to schedule conflict: Hall={HallId}, Start={Start}, End={End}", bulkDto.HallId, startTimeUtc, endTime);
                                skippedCount++;
                                string conflictMsg = "Trùng lịch chiếu với suất chiếu khác.";
                                if (!skippedReasons.Contains(conflictMsg))
                                {
                                    skippedReasons.Add(conflictMsg);
                                }
                                continue;
                            }

                            var newShowtime = new Showtime
                            {
                                MovieId = bulkDto.MovieId,
                                HallId = bulkDto.HallId,
                                PriceId = bulkDto.PriceId,
                                StartTime = startTimeUtc,
                                EndTime = endTime,
                                IsPriceOverride = bulkDto.FlatPriceEnabled,
                                CustomPrice = bulkDto.FlatPriceEnabled ? bulkDto.FlatPrice : null
                            };
                            batchList.Add(newShowtime);
                        }
                    }

                    if (batchList.Any())
                    {
                        await _context.Showtimes.AddRangeAsync(batchList, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);

                        // Reload and map to ShowtimeDtos
                        var insertedIds = batchList.Select(b => b.ShowtimeId).ToList();
                        var savedShowtimes = await _context.Showtimes
                            .Include(s => s.Movie)
                            .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                            .Include(s => s.Hall).ThenInclude(h => h.HallType)
                            .Include(s => s.Price)
                            .Where(s => insertedIds.Contains(s.ShowtimeId))
                            .AsNoTracking()
                            .ToListAsync(cancellationToken);

                        var dtos = await MapShowtimesToDtosAsync(savedShowtimes, cancellationToken);

                        // Queue single Outbox Event
                        var outboxEvent = new OutboxEvent
                        {
                            Id = Guid.NewGuid(),
                            EventName = ShowtimeConstants.SignalREvents.BulkShowtimesCreated,
                            Payload = System.Text.Json.JsonSerializer.Serialize(dtos),
                            OccurredOn = DateTime.UtcNow
                        };
                        await _context.OutboxEvents.AddAsync(outboxEvent, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    await transaction.CommitAsync(cancellationToken);

                    if (batchList.Count == 0)
                    {
                        var reasonMsg = skippedReasons.Any() 
                            ? string.Join(" ", skippedReasons) 
                            : "Trùng lịch chiếu, vi phạm giờ hoạt động hoặc ngày khởi chiếu.";
                        return ApiResponse.Fail<string>($"Không có suất chiếu nào được tạo. Chi tiết: {reasonMsg}");
                    }
                    string successMessage = $"Tạo thành công {batchList.Count} suất chiếu. Bỏ qua {skippedCount} suất chiếu do trùng lịch hoặc không hợp lệ.";
                    return ApiResponse.Success(successMessage);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Error occurred during bulk showtime creation for Hall ID {HallId}.", bulkDto.HallId);
                    throw;
                }
            }
        }

        public async Task<ApiResponse<ShowtimeDto>> UpdateShowtimeAsync(
            int id, 
            ShowtimeUpdateDto updateDto, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating showtime ID {ShowtimeId}", id);

            var showtime = await _context.Showtimes.FirstOrDefaultAsync(s => s.ShowtimeId == id, cancellationToken);
            if (showtime == null)
            {
                throw new NotFoundException($"Không tìm thấy suất chiếu có ID {id}.");
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (showtime.StartTime <= now)
            {
                throw new BusinessException("Không thể chỉnh sửa suất chiếu đã bắt đầu.");
            }

            var hasBookings = await _context.Bookings
                .AsNoTracking()
                .AnyAsync(b => b.ShowtimeId == id && b.BookingStatus != BookingConstants.Cancelled, cancellationToken);

            if (hasBookings)
            {
                throw new BusinessException("Không thể sửa đổi thông tin suất chiếu đã phát sinh vé đặt của khách hàng.");
            }

            var createValidationDto = new ShowtimeCreateDto
            {
                MovieId = updateDto.MovieId,
                HallId = updateDto.HallId,
                PriceId = updateDto.PriceId,
                StartTime = updateDto.StartTime
            };
            var movie = await ValidateAndLoadShowtimePrerequisitesAsync(createValidationDto, cancellationToken);
            var endTime = ShowtimeSchedulingPolicy.CalculateEndTime(movie, updateDto.StartTime);

            var lockKeys = new List<string> { $"lock:hall:{showtime.HallId}" };
            if (showtime.HallId != updateDto.HallId)
            {
                lockKeys.Add($"lock:hall:{updateDto.HallId}");
            }
            lockKeys.Sort();

            var acquiredLocks = new List<IDisposable>();
            try
            {
                foreach (var key in lockKeys)
                {
                    var l = await _distributedLock.AcquireLockAsync(key, TimeSpan.FromSeconds(10), cancellationToken);
                    acquiredLocks.Add(l);
                }

                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var conflictExists = await CheckOverlappingScheduleAsync(updateDto.HallId, updateDto.StartTime, endTime, id, cancellationToken);
                    if (conflictExists)
                    {
                        throw new BusinessException("Lịch chiếu bị trùng hoặc quá gần lịch chiếu khác trong sảnh này (yêu cầu khoảng dọn dẹp 15 phút).");
                    }

                    _mapper.Map(updateDto, showtime);
                    showtime.EndTime = endTime;

                    _context.Showtimes.Update(showtime);
                    await _context.SaveChangesAsync(cancellationToken);

                    var updated = await _context.Showtimes
                        .Include(s => s.Movie)
                        .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                        .Include(s => s.Hall).ThenInclude(h => h.HallType)
                        .Include(s => s.Price)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.ShowtimeId == id, cancellationToken);

                    var dtos = await MapShowtimesToDtosAsync(new List<Showtime> { updated ?? showtime }, cancellationToken);
                    var resultDto = dtos.First();

                    // Queue Outbox Event
                    var outboxEvent = new OutboxEvent
                    {
                        Id = Guid.NewGuid(),
                        EventName = ShowtimeConstants.SignalREvents.ShowtimeUpdated,
                        Payload = System.Text.Json.JsonSerializer.Serialize(resultDto),
                        OccurredOn = DateTime.UtcNow
                    };
                    await _context.OutboxEvents.AddAsync(outboxEvent, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Showtime updated successfully with ID {ShowtimeId}", id);
                    return ApiResponse.Success(resultDto);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Error occurred during showtime update for Showtime ID {ShowtimeId}.", id);
                    throw;
                }
            }
            finally
            {
                foreach (var l in acquiredLocks)
                {
                    l.Dispose();
                }
            }
        }

        public async Task<ApiResponse<bool>> DeleteShowtimeAsync(
            int id, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting showtime ID {ShowtimeId}", id);

            var showtime = await _context.Showtimes.FirstOrDefaultAsync(s => s.ShowtimeId == id, cancellationToken);
            if (showtime == null)
            {
                throw new NotFoundException($"Không tìm thấy suất chiếu có ID {id}.");
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (showtime.StartTime <= now)
            {
                throw new BusinessException("Không thể xóa suất chiếu đã bắt đầu.");
            }

            var hasBookings = await _context.Bookings
                .AsNoTracking()
                .AnyAsync(b => b.ShowtimeId == id && b.BookingStatus != BookingConstants.Cancelled, cancellationToken);

            if (hasBookings)
            {
                throw new BusinessException("Không thể xóa suất chiếu đã có khách hàng đặt vé.");
            }

            var lockKey = $"lock:hall:{showtime.HallId}";
            using (await _distributedLock.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10), cancellationToken))
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    showtime.IsDeleted = true;
                    showtime.Status = "Cancelled";
                    _context.Showtimes.Update(showtime);
                    await _context.SaveChangesAsync(cancellationToken);

                    // Queue Outbox Event
                    var outboxEvent = new OutboxEvent
                    {
                        Id = Guid.NewGuid(),
                        EventName = ShowtimeConstants.SignalREvents.ShowtimeDeleted,
                        Payload = System.Text.Json.JsonSerializer.Serialize(id),
                        OccurredOn = DateTime.UtcNow
                    };
                    await _context.OutboxEvents.AddAsync(outboxEvent, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Showtime deleted successfully with ID {ShowtimeId}", id);
                    return ApiResponse.Success(true, "Xóa suất chiếu thành công.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Error occurred during showtime deletion for Showtime ID {ShowtimeId}.", id);
                    throw;
                }
            }
        }

        #endregion

        #region Validation, Conflict & Helper Systems

        private async Task<Movie> ValidateAndLoadShowtimePrerequisitesAsync(
            ShowtimeCreateDto createDto, 
            CancellationToken cancellationToken = default)
        {
            if (createDto.StartTime <= _timeProvider.GetUtcNow().UtcDateTime)
            {
                throw new ValidationException("Thời gian bắt đầu của suất chiếu phải ở trong tương lai.");
            }

            var movie = await _context.Movies
                .Include(m => m.MovieFormats)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == createDto.MovieId && !m.IsDeleted, cancellationToken);
            if (movie == null)
            {
                throw new ValidationException($"Phim có ID {createDto.MovieId} không tồn tại hoặc đã bị ẩn.");
            }

            // Enforce domain release window policy
            ShowtimeSchedulingPolicy.ValidateReleaseWindow(movie, createDto.StartTime);

            var hall = await _context.Halls
                .Include(h => h.Cinema)
                .Include(h => h.HallType)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HallId == createDto.HallId, cancellationToken);
            if (hall == null || hall.IsDeleted)
            {
                throw new ValidationException($"Sảnh chiếu có ID {createDto.HallId} không tồn tại hoặc đã bị ẩn.");
            }

            if (hall.Cinema == null || hall.Cinema.IsDeleted)
            {
                throw new ValidationException("Rạp chiếu của sảnh này hiện tại đang bị dừng hoạt động.");
            }

            // Enforce operating hours policy
            var endTime = ShowtimeSchedulingPolicy.CalculateEndTime(movie, createDto.StartTime);
            ShowtimeSchedulingPolicy.ValidateOperatingHours(createDto.StartTime, endTime, hall.Cinema);

            var price = await _context.Prices.AsNoTracking().FirstOrDefaultAsync(p => p.PriceId == createDto.PriceId, cancellationToken);
            if (price == null)
            {
                throw new ValidationException($"Mức giá cấu hình có ID {createDto.PriceId} không tồn tại.");
            }

            // Validate format compatibility
            string format = "2D"; // Default fallback
            if (!string.IsNullOrEmpty(price.TicketType))
            {
                if (price.TicketType.Trim().StartsWith("{"))
                {
                    try
                    {
                        var obj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(price.TicketType);
                        if (obj != null && obj.TryGetValue("roomType", out var roomType))
                        {
                            format = roomType;
                        }
                    }
                    catch
                    {
                        // Fallback to text check
                    }
                }
                
                if (format == "2D" || format == "Standard" || format == "VIP") // If JSON parse failed or roomType is not set
                {
                    format = "2D";
                    string upperType = price.TicketType.ToUpper();
                    if (upperType.Contains("IMAX")) format = "IMAX";
                    else if (upperType.Contains("3D")) format = "3D";
                }
            }

            var supportedFormats = new List<string>();
            if (!string.IsNullOrEmpty(hall.Description) && hall.Description.Trim().StartsWith("{"))
            {
                try
                {
                    var obj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(hall.Description);
                    if (obj != null && obj.TryGetValue("supportedFormats", out var formatsObj) && formatsObj is System.Text.Json.JsonElement elem && elem.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in elem.EnumerateArray())
                        {
                            if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                supportedFormats.Add(item.GetString()!);
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback
                }
            }

            if (supportedFormats.Count == 0)
            {
                string typeName = (hall.HallType?.TypeName ?? "").ToUpper();
                if (typeName.Contains("IMAX"))
                {
                    supportedFormats.AddRange(new[] { "IMAX", "3D", "2D" });
                }
                else
                {
                    supportedFormats.AddRange(new[] { "2D", "3D" });
                }
            }

            // Check 1: Format mismatch
            if (movie.MovieFormats != null && movie.MovieFormats.Any())
            {
                var movieHasFormat = movie.MovieFormats.Any(f => f.FormatName.Equals(format, StringComparison.OrdinalIgnoreCase));
                if (!movieHasFormat)
                {
                    throw new ValidationException($"Phim '{movie.Title}' không hỗ trợ định dạng {format}.");
                }
            }

            var hallHasFormat = supportedFormats.Any(f => f.Equals(format, StringComparison.OrdinalIgnoreCase));
            if (!hallHasFormat)
            {
                throw new ValidationException($"Phòng chiếu '{hall.HallName}' không hỗ trợ định dạng {format}.");
            }

            return movie;
        }

        public async Task<ApiResponse<bool>> ValidateShowtimeAsync(
            ShowtimeCreateDto createDto, 
            CancellationToken cancellationToken = default)
        {
            await ValidateAndLoadShowtimePrerequisitesAsync(createDto, cancellationToken);
            return ApiResponse.Success(true);
        }

        public async Task<ApiResponse<bool>> CheckScheduleConflictAsync(
            int hallId, 
            DateTime startTime, 
            DateTime endTime, 
            int? excludeShowtimeId = null, 
            CancellationToken cancellationToken = default)
        {
            var conflictExists = await CheckOverlappingScheduleAsync(hallId, startTime, endTime, excludeShowtimeId, cancellationToken);
            return ApiResponse.Success(conflictExists);
        }

        private async Task<bool> CheckOverlappingScheduleAsync(
            int hallId, 
            DateTime startTime, 
            DateTime endTime, 
            int? excludeShowtimeId = null, 
            CancellationToken cancellationToken = default)
        {
            var existingShowtimes = await _context.Showtimes
                .AsNoTracking()
                .Where(s => s.HallId == hallId && s.ShowtimeId != excludeShowtimeId)
                .Select(s => new { s.StartTime, s.EndTime })
                .ToListAsync(cancellationToken);

            return existingShowtimes.Any(s => ConflictPolicy.Overlaps(startTime, endTime, s.StartTime, s.EndTime, ShowtimeSchedulingPolicy.CleanUpBufferMinutes));
        }

        public async Task<DateTime> CalculateEndTime(
            int movieId, 
            DateTime startTime, 
            CancellationToken cancellationToken = default)
        {
            var movie = await _context.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == movieId, cancellationToken);
            if (movie == null)
            {
                throw new ValidationException("Không thể tính toán thời lượng cho bộ phim không tồn tại.");
            }
            return ShowtimeSchedulingPolicy.CalculateEndTime(movie, startTime);
        }

        public async Task<ApiResponse<int>> CalculateAvailableSeatsAsync(
            int showtimeId, 
            CancellationToken cancellationToken = default)
        {
            var showtimeInfo = await _context.Showtimes
                .AsNoTracking()
                .Where(s => s.ShowtimeId == showtimeId)
                .Select(s => new { s.HallId, s.StartTime })
                .FirstOrDefaultAsync(cancellationToken);

            if (showtimeInfo == null)
            {
                throw new NotFoundException($"Không tìm thấy suất chiếu ID {showtimeId}.");
            }

            var totalSeats = await _context.Seats.CountAsync(s => s.HallId == showtimeInfo.HallId && !s.IsDeleted, cancellationToken);
            
            var bookedSeats = await _context.BookingSeats
                .CountAsync(bs => bs.Booking.ShowtimeId == showtimeId && bs.Booking.BookingStatus != BookingConstants.Cancelled, cancellationToken);

            var lockedSeats = await _seatLockService.GetLockedSeatsAsync(showtimeId, cancellationToken);
            var lockedCount = lockedSeats.Count;

            var available = Math.Max(0, totalSeats - bookedSeats - lockedCount);
            return ApiResponse.Success(available);
        }

        #endregion

        #region Internal Projection Mapping System

        private async Task<List<ShowtimeDto>> MapShowtimesToDtosAsync(
            List<Showtime> items, 
            CancellationToken cancellationToken = default)
        {
            if (!items.Any()) return new List<ShowtimeDto>();

            var showtimeIds = items.Select(s => s.ShowtimeId).ToList();

            // Bulk query booked seats count
            var bookedSeatsCounts = await _context.BookingSeats
                .Where(bs => showtimeIds.Contains(bs.Booking.ShowtimeId) && bs.Booking.BookingStatus != BookingConstants.Cancelled)
                .GroupBy(bs => bs.Booking.ShowtimeId)
                .Select(g => new { ShowtimeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ShowtimeId, x => x.Count, cancellationToken);

            // Parallel cached seat lock checks
            var lockedSeatsCounts = new Dictionary<int, int>();
            var lockTasks = showtimeIds.Select(async id =>
            {
                var locked = await _seatLockService.GetLockedSeatsAsync(id, cancellationToken);
                return new { ShowtimeId = id, Count = locked.Count };
            });

            var lockResults = await Task.WhenAll(lockTasks);
            foreach (var res in lockResults)
            {
                lockedSeatsCounts[res.ShowtimeId] = res.Count;
            }

            // Bulk query total seats count for halls in this batch
            var hallIds = items.Select(i => i.HallId).Distinct().ToList();
            var hallTotalSeats = await _context.Seats
                .Where(s => hallIds.Contains(s.HallId) && !s.IsDeleted)
                .GroupBy(s => s.HallId)
                .Select(g => new { HallId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.HallId, x => x.Count, cancellationToken);

            var dtos = new List<ShowtimeDto>();
            var nowTime = _timeProvider.GetUtcNow().UtcDateTime;

            foreach (var item in items)
            {
                var dto = _mapper.Map<ShowtimeDto>(item);

                dto.TotalSeats = hallTotalSeats.TryGetValue(item.HallId, out var tot) ? tot : 0;

                var booked = bookedSeatsCounts.TryGetValue(item.ShowtimeId, out var bCount) ? bCount : 0;
                var locked = lockedSeatsCounts.TryGetValue(item.ShowtimeId, out var lCount) ? lCount : 0;
                dto.AvailableSeats = Math.Max(0, dto.TotalSeats - booked - locked);

                // Dynamically calculate status using constant values
                dto.Status = item.StartTime > nowTime
                    ? ShowtimeConstants.Statuses.Upcoming
                    : (item.EndTime < nowTime ? ShowtimeConstants.Statuses.Ended : ShowtimeConstants.Statuses.NowShowing);

                dtos.Add(dto);
            }

            return dtos;
        }

        #endregion
    }
}