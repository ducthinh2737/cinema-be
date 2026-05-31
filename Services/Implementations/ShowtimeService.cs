using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Movies;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;
using CinemaBooking.API.SignalR;

namespace CinemaBooking.API.Services.Implementations
{
    /// <summary>
    /// Enterprise Showtime Scheduling Service that ensures conflict-free scheduling,
    /// optimized database queries, transaction safety, and realtime updates.
    /// </summary>
    public class ShowtimeService : IShowtimeService
    {
        private readonly CinemaDbContext _context;
        private readonly IShowtimeRepository _showtimeRepository;
        private readonly IMovieRepository _movieRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ShowtimeService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ShowtimeService(
            CinemaDbContext context,
            IShowtimeRepository showtimeRepository,
            IMovieRepository movieRepository,
            IMapper mapper,
            ILogger<ShowtimeService> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _showtimeRepository = showtimeRepository;
            _movieRepository = movieRepository;
            _mapper = mapper;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        #region Public Interface Query Methods

        /// <summary>
        /// Retrieves paginated list of showtimes based on advanced filters, search terms, and sorting options.
        /// </summary>
        public async Task<ApiResponse<PagedResultDto<ShowtimeDto>>> GetPagedShowtimesAsync(ShowtimeQueryParameters queryParams)
        {
            _logger.LogInformation("Retrieving paginated showtimes for Page Number: {PageNumber}", queryParams.PageNumber);

            var query = _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
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
                query = query.Where(s => s.StartTime.Date == targetDate);
            }

            // Filter by dynamic Status
            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                var now = DateTime.UtcNow;
                query = queryParams.Status.ToLower() switch
                {
                    "upcoming" => query.Where(s => s.StartTime > now),
                    "nowshowing" => query.Where(s => s.StartTime <= now && s.EndTime >= now),
                    "ended" => query.Where(s => s.EndTime < now),
                    _ => query
                };
            }

            // Search support (Movie title or Hall name)
            if (!string.IsNullOrEmpty(queryParams.SearchTerm))
            {
                var search = queryParams.SearchTerm.ToLower();
                query = query.Where(s => s.Movie.Title.ToLower().Contains(search) || s.Hall.HallName.ToLower().Contains(search));
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

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            var dtos = await MapShowtimesToDtosAsync(items);

            var pagedResult = new PagedResultDto<ShowtimeDto>
            {
                Items = dtos,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse.Success(pagedResult);
        }

        /// <summary>
        /// Gets a showtime by its identifier.
        /// </summary>
        public async Task<ApiResponse<ShowtimeDto>> GetShowtimeByIdAsync(int id)
        {
            _logger.LogInformation("Retrieving showtime by ID: {ShowtimeId}", id);

            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Price)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShowtimeId == id);

            if (showtime == null)
            {
                throw new NotFoundException($"Không tìm thấy suất chiếu có ID {id}.");
            }

            var dtos = await MapShowtimesToDtosAsync(new List<Showtime> { showtime });
            return ApiResponse.Success(dtos.First());
        }

        /// <summary>
        /// Retrieves all showtimes for a specific Movie ID.
        /// </summary>
        public async Task<ApiResponse<IEnumerable<ShowtimeDto>>> GetShowtimesByMovieIdAsync(int movieId)
        {
            _logger.LogInformation("Retrieving showtimes for Movie ID: {MovieId}", movieId);

            var showtimes = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Price)
                .Where(s => s.MovieId == movieId)
                .AsNoTracking()
                .ToListAsync();

            var dtos = await MapShowtimesToDtosAsync(showtimes);
            return ApiResponse.Success<IEnumerable<ShowtimeDto>>(dtos);
        }

        /// <summary>
        /// Retrieves all showtimes for a specific Cinema ID.
        /// </summary>
        public async Task<ApiResponse<IEnumerable<ShowtimeDto>>> GetShowtimesByCinemaIdAsync(int cinemaId)
        {
            _logger.LogInformation("Retrieving showtimes for Cinema ID: {CinemaId}", cinemaId);

            var showtimes = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Price)
                .Where(s => s.Hall.CinemaId == cinemaId)
                .AsNoTracking()
                .ToListAsync();

            var dtos = await MapShowtimesToDtosAsync(showtimes);
            return ApiResponse.Success<IEnumerable<ShowtimeDto>>(dtos);
        }

        #endregion

        #region Public Mutation Methods

        /// <summary>
        /// Creates a new showtime and schedules it in the system.
        /// Checks for conflicts and broadcasts updates via SignalR.
        /// </summary>
        public async Task<ApiResponse<ShowtimeDto>> CreateShowtimeAsync(ShowtimeCreateDto createDto)
        {
            _logger.LogInformation("Creating showtime in Hall {HallId} at {StartTime}", createDto.HallId, createDto.StartTime);

            // Business validation
            await ValidateShowtimeAsync(createDto);

            var endTime = await CalculateEndTime(createDto.MovieId, createDto.StartTime);

            // Double conflict check
            var conflictResult = await CheckScheduleConflictAsync(createDto.HallId, createDto.StartTime, endTime);
            if (conflictResult.Data)
            {
                throw new BusinessException("Lịch chiếu bị trùng hoặc quá gần lịch chiếu khác trong sảnh này (yêu cầu khoảng dọn dẹp 15 phút).");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var showtime = _mapper.Map<Showtime>(createDto);
                showtime.EndTime = endTime;

                await _context.Showtimes.AddAsync(showtime);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Load with details for response
                var savedShowtime = await _context.Showtimes
                    .Include(s => s.Movie)
                    .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                    .Include(s => s.Price)
                    .FirstOrDefaultAsync(s => s.ShowtimeId == showtime.ShowtimeId);

                var dtos = await MapShowtimesToDtosAsync(new List<Showtime> { savedShowtime ?? showtime });
                var resultDto = dtos.First();

                // Broadcast change via SignalR
                var hubContext = _serviceProvider.GetService<IHubContext<SeatHub>>();
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync("ShowtimeCreated", resultDto);
                }

                _logger.LogInformation("Showtime created successfully with ID {ShowtimeId}", showtime.ShowtimeId);
                return ApiResponse.Success(resultDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred during showtime creation.");
                throw;
            }
        }

        /// <summary>
        /// Updates an existing showtime schedule.
        /// </summary>
        public async Task<ApiResponse<ShowtimeDto>> UpdateShowtimeAsync(int id, ShowtimeUpdateDto updateDto)
        {
            _logger.LogInformation("Updating showtime ID {ShowtimeId}", id);

            var showtime = await _context.Showtimes
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.ShowtimeId == id);

            if (showtime == null)
            {
                throw new NotFoundException($"Không tìm thấy suất chiếu có ID {id}.");
            }

            // Business check: Prevent changing scheduled details if active bookings exist
            var hasBookings = showtime.Bookings.Any(b => b.BookingStatus != "Cancelled");
            if (hasBookings)
            {
                throw new BusinessException("Không thể sửa đổi thông tin suất chiếu đã phát sinh vé đặt của khách hàng.");
            }

            // Business validation
            var createValidationDto = new ShowtimeCreateDto
            {
                MovieId = updateDto.MovieId,
                HallId = updateDto.HallId,
                PriceId = updateDto.PriceId,
                StartTime = updateDto.StartTime
            };
            await ValidateShowtimeAsync(createValidationDto);

            var endTime = await CalculateEndTime(updateDto.MovieId, updateDto.StartTime);

            // Conflict check
            var conflictResult = await CheckScheduleConflictAsync(updateDto.HallId, updateDto.StartTime, endTime, id);
            if (conflictResult.Data)
            {
                throw new BusinessException("Lịch chiếu bị trùng hoặc quá gần lịch chiếu khác trong sảnh này (yêu cầu khoảng dọn dẹp 15 phút).");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _mapper.Map(updateDto, showtime);
                showtime.EndTime = endTime;

                _context.Showtimes.Update(showtime);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Load detail
                var updated = await _context.Showtimes
                    .Include(s => s.Movie)
                    .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                    .Include(s => s.Price)
                    .FirstOrDefaultAsync(s => s.ShowtimeId == id);

                var dtos = await MapShowtimesToDtosAsync(new List<Showtime> { updated ?? showtime });
                var resultDto = dtos.First();

                // Broadcast change via SignalR
                var hubContext = _serviceProvider.GetService<IHubContext<SeatHub>>();
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync("ShowtimeUpdated", resultDto);
                }

                _logger.LogInformation("Showtime updated successfully with ID {ShowtimeId}", id);
                return ApiResponse.Success(resultDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred during showtime update.");
                throw;
            }
        }

        /// <summary>
        /// Deletes an empty showtime schedule. Blocks deletion if showtime has active bookings.
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteShowtimeAsync(int id)
        {
            _logger.LogInformation("Deleting showtime ID {ShowtimeId}", id);

            var showtime = await _context.Showtimes
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.ShowtimeId == id);

            if (showtime == null)
            {
                throw new NotFoundException($"Không tìm thấy suất chiếu có ID {id}.");
            }

            var hasBookings = showtime.Bookings.Any(b => b.BookingStatus != "Cancelled");
            if (hasBookings)
            {
                throw new BusinessException("Không thể xóa suất chiếu đã có khách hàng đặt vé.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Showtimes.Remove(showtime);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Broadcast deletion via SignalR
                var hubContext = _serviceProvider.GetService<IHubContext<SeatHub>>();
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync("ShowtimeDeleted", id);
                }

                _logger.LogInformation("Showtime deleted successfully with ID {ShowtimeId}", id);
                return ApiResponse.Success(true, "Xóa suất chiếu thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred during showtime deletion.");
                throw;
            }
        }

        #endregion

        #region Validation, Conflict & Helper Systems

        /// <summary>
        /// Validates that movies, halls, prices exist, are active, and scheduled in the future.
        /// </summary>
        public async Task<ApiResponse<bool>> ValidateShowtimeAsync(ShowtimeCreateDto createDto)
        {
            if (createDto.StartTime <= DateTime.UtcNow)
            {
                throw new ValidationException("Thời gian bắt đầu của suất chiếu phải ở trong tương lai.");
            }

            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == createDto.MovieId && !m.IsDeleted);
            if (movie == null)
            {
                throw new ValidationException($"Phim có ID {createDto.MovieId} không tồn tại hoặc đã bị ẩn.");
            }

            var hall = await _context.Halls.Include(h => h.Cinema).FirstOrDefaultAsync(h => h.HallId == createDto.HallId);
            if (hall == null || hall.IsDeleted)
            {
                throw new ValidationException($"Sảnh chiếu có ID {createDto.HallId} không tồn tại hoặc đã bị ẩn.");
            }

            if (hall.Cinema == null || hall.Cinema.IsDeleted)
            {
                throw new ValidationException("Rạp chiếu của sảnh này hiện tại đang bị dừng hoạt động.");
            }

            var price = await _context.Prices.FindAsync(createDto.PriceId);
            if (price == null)
            {
                throw new ValidationException($"Mức giá cấu hình có ID {createDto.PriceId} không tồn tại.");
            }

            return ApiResponse.Success(true);
        }

        /// <summary>
        /// Evaluates scheduling overlaps inside the selected hall. Requires 15-minute gap for cleanup.
        /// </summary>
        public async Task<ApiResponse<bool>> CheckScheduleConflictAsync(int hallId, DateTime startTime, DateTime endTime, int? excludeShowtimeId = null)
        {
            var endTimeWithBuffer = endTime.AddMinutes(15);

            var conflictExists = await _context.Showtimes
                .Where(s => s.HallId == hallId && s.ShowtimeId != excludeShowtimeId)
                .Where(s => s.StartTime < endTimeWithBuffer && startTime < s.EndTime.AddMinutes(15))
                .AnyAsync();

            return ApiResponse.Success(conflictExists);
        }

        /// <summary>
        /// Calculates showtime end time dynamically based on movie duration.
        /// </summary>
        public async Task<DateTime> CalculateEndTime(int movieId, DateTime startTime)
        {
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null)
            {
                throw new ValidationException("Không thể tính toán thời lượng cho bộ phim không tồn tại.");
            }
            return startTime.AddMinutes(movie.Duration);
        }

        /// <summary>
        /// Calculates available seats for a showtime, taking into account booked seats and active locks.
        /// </summary>
        public async Task<ApiResponse<int>> CalculateAvailableSeatsAsync(int showtimeId)
        {
            var showtime = await _context.Showtimes.FindAsync(showtimeId);
            if (showtime == null)
            {
                throw new NotFoundException($"Không tìm thấy suất chiếu ID {showtimeId}.");
            }

            var totalSeats = await _context.Seats.CountAsync(s => s.HallId == showtime.HallId && !s.IsDeleted);
            var bookedSeats = await _context.BookingSeats
                .CountAsync(bs => bs.Booking.ShowtimeId == showtimeId && bs.Booking.BookingStatus != "Cancelled");

            var lockedSeatsCount = 0;
            var seatLockService = _serviceProvider.GetService<ISeatLockService>();
            if (seatLockService != null)
            {
                var lockedSeats = await seatLockService.GetLockedSeatsAsync(showtimeId);
                lockedSeatsCount = lockedSeats.Count();
            }

            var available = Math.Max(0, totalSeats - bookedSeats - lockedSeatsCount);
            return ApiResponse.Success(available);
        }

        #endregion

        #region Internal Projection Mapping System

        private async Task<List<ShowtimeDto>> MapShowtimesToDtosAsync(List<Showtime> items)
        {
            if (!items.Any()) return new List<ShowtimeDto>();

            var showtimeIds = items.Select(s => s.ShowtimeId).ToList();

            // Bulk query booked seats count
            var bookedSeatsCounts = await _context.BookingSeats
                .Where(bs => showtimeIds.Contains(bs.Booking.ShowtimeId) && bs.Booking.BookingStatus != "Cancelled")
                .GroupBy(bs => bs.Booking.ShowtimeId)
                .Select(g => new { ShowtimeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ShowtimeId, x => x.Count);

            // Bulk query locked seats count
            var lockedSeatsCounts = new Dictionary<int, int>();
            var seatLockService = _serviceProvider.GetService<ISeatLockService>();
            if (seatLockService != null)
            {
                foreach (var id in showtimeIds)
                {
                    var locked = await seatLockService.GetLockedSeatsAsync(id);
                    lockedSeatsCounts[id] = locked.Count();
                }
            }

            // Bulk query total seats count for halls in this batch
            var hallIds = items.Select(i => i.HallId).Distinct().ToList();
            var hallTotalSeats = await _context.Seats
                .Where(s => hallIds.Contains(s.HallId) && !s.IsDeleted)
                .GroupBy(s => s.HallId)
                .Select(g => new { HallId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.HallId, x => x.Count);

            var dtos = new List<ShowtimeDto>();
            var nowTime = DateTime.UtcNow;

            foreach (var item in items)
            {
                var dto = _mapper.Map<ShowtimeDto>(item);

                dto.TotalSeats = hallTotalSeats.TryGetValue(item.HallId, out var tot) ? tot : 0;

                var booked = bookedSeatsCounts.TryGetValue(item.ShowtimeId, out var bCount) ? bCount : 0;
                var locked = lockedSeatsCounts.TryGetValue(item.ShowtimeId, out var lCount) ? lCount : 0;
                dto.AvailableSeats = Math.Max(0, dto.TotalSeats - booked - locked);

                // Dynamically calculate status
                dto.Status = item.StartTime > nowTime ? "Upcoming" : (item.EndTime < nowTime ? "Ended" : "NowShowing");

                dtos.Add(dto);
            }

            return dtos;
        }

        #endregion
    }
}
