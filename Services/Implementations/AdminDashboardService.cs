using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Admin;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Services.Implementations
{
    /// <summary>
    /// High-performance enterprise dashboard service that leverages SQL-side aggregations,
    /// MemoryCache layers, and granular date filtering to deliver rapid and scalable metrics.
    /// </summary>
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly CinemaDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AdminDashboardService> _logger;

        public AdminDashboardService(
            CinemaDbContext context,
            IMemoryCache cache,
            ILogger<AdminDashboardService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        #region Public API Endpoints

        /// <summary>
        /// Retrieves the dashboard summary card metrics, top performance lists, and monthly trend graphs.
        /// </summary>
        public async Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryAsync(DateTime? from = null, DateTime? to = null)
        {
            var cacheKey = $"DashboardSummary_{GetDateRangeCacheSuffix(from, to)}";
            if (_cache.TryGetValue(cacheKey, out DashboardSummaryDto? cachedSummary) && cachedSummary != null)
            {
                _logger.LogInformation("Returning cached dashboard summary.");
                return ApiResponse.Success(cachedSummary);
            }

            _logger.LogInformation("Generating dashboard summary from DB. Filter: {From} to {To}", from, to);

            var start = from ?? DateTime.UtcNow.AddMonths(-1);
            var end = to ?? DateTime.UtcNow;

            // 1. Total Revenue
            var totalRevenue = await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end && (p.PaymentStatus == "Completed" || p.PaymentStatus == "Success"))
                .SumAsync(p => p.Amount);

            // 2. Bookings count
            var totalBookings = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end)
                .CountAsync();

            // 3. User growth
            var totalUsers = await _context.Users
                .AsNoTracking()
                .Where(u => u.CreatedAt <= end)
                .CountAsync();

            // 4. Conversion rate
            var confirmedBookings = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && b.BookingStatus == "Confirmed")
                .CountAsync();

            double conversionRate = totalBookings > 0 
                ? Math.Round(((double)confirmedBookings / totalBookings) * 100, 2) 
                : 0.0;

            // 5. Cancellation rate
            var cancelledBookings = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && b.BookingStatus == "Cancelled")
                .CountAsync();

            double cancellationRate = totalBookings > 0
                ? Math.Round(((double)cancelledBookings / totalBookings) * 100, 2)
                : 0.0;

            // 6. Top Movies
            var topMovies = (await GetTopMoviesAsync(5, start, end)).Data;

            // 7. Top Cinemas
            var topCinemas = (await GetTopCinemasAsync(5, start, end)).Data;

            // 8. Monthly Revenue Chart
            var monthlyChart = (await GetRevenueChartAsync(start, end)).Data;

            // 9. Occupancy Rate
            double occupancyRate = (await GetOccupancyRateAsync(start, end)).Data;

            // 10. Active users today (users placing bookings today)
            var today = DateTime.UtcNow.Date;
            var activeUsersToday = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= today)
                .Select(b => b.UserId)
                .Distinct()
                .CountAsync();

            // Calculate Growth % compared to the previous same-sized window
            var windowSize = end - start;
            var prevStart = start - windowSize;
            var prevEnd = start;
            var prevRevenue = await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= prevStart && p.PaymentDate <= prevEnd && (p.PaymentStatus == "Completed" || p.PaymentStatus == "Success"))
                .SumAsync(p => p.Amount);

            decimal growthPercentage = 0m;
            if (prevRevenue > 0m)
            {
                growthPercentage = Math.Round(((totalRevenue - prevRevenue) / prevRevenue) * 100, 2);
            }

            var summary = new DashboardSummaryDto
            {
                TotalRevenue = totalRevenue,
                TotalBookings = totalBookings,
                TotalUsers = totalUsers,
                ConversionRate = conversionRate,
                CancellationRate = cancellationRate,
                OccupancyRate = occupancyRate,
                RevenueGrowthPercentage = growthPercentage,
                ActiveUsersToday = activeUsersToday,
                TopMovies = topMovies,
                TopCinemas = topCinemas,
                MonthlyRevenueChart = monthlyChart
            };

            _cache.Set(cacheKey, summary, TimeSpan.FromMinutes(5));
            return ApiResponse.Success(summary);
        }

        /// <summary>
        /// Aggregates revenue statistics broken down by VietQR and Cash payment methods.
        /// </summary>
        public async Task<ApiResponse<RevenueStatsDto>> GetRevenueStatisticsAsync(DateTime? from = null, DateTime? to = null)
        {
            var cacheKey = $"RevenueStatistics_{GetDateRangeCacheSuffix(from, to)}";
            if (_cache.TryGetValue(cacheKey, out RevenueStatsDto? cachedStats) && cachedStats != null)
            {
                return ApiResponse.Success(cachedStats);
            }

            var start = from ?? DateTime.MinValue;
            var end = to ?? DateTime.UtcNow;

            var paymentsQuery = _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end && (p.PaymentStatus == "Completed" || p.PaymentStatus == "Success"));

            // Project to payment groups to aggregate SQL-side
            var groupedMethods = await paymentsQuery
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new { Method = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToListAsync();

            decimal cash = 0m;
            decimal vietQR = 0m;

            foreach (var group in groupedMethods)
            {
                if (group.Method.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                {
                    cash = group.Amount;
                }
                else if (group.Method.Equals("VietQR", StringComparison.OrdinalIgnoreCase))
                {
                    vietQR = group.Amount;
                }
                else
                {
                    // Map other legacy methods to VietQR for compatibility/migration purposes
                    vietQR += group.Amount;
                }
            }

            var stats = new RevenueStatsDto
            {
                CashRevenue = cash,
                VietQRRevenue = vietQR,
                Total = cash + vietQR
            };

            _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(5));
            return ApiResponse.Success(stats);
        }

        /// <summary>
        /// Fetches the top selling movies sorted by total box office revenue.
        /// </summary>
        public async Task<ApiResponse<List<MovieRevenueDto>>> GetTopMoviesAsync(int limit = 5, DateTime? from = null, DateTime? to = null)
        {
            var start = from ?? DateTime.MinValue;
            var end = to ?? DateTime.UtcNow;

            // Group by Showtime.MovieId to avoid complex multi-join EF Core issues
            var topMoviesGrouped = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && b.BookingStatus == "Confirmed")
                .GroupBy(b => b.Showtime.MovieId)
                .Select(g => new
                {
                    MovieId = g.Key,
                    Revenue = g.Sum(b => b.TotalAmount),
                    TicketsSold = _context.BookingSeats.Count(bs => bs.Booking.Showtime.MovieId == g.Key && bs.Booking.BookingStatus == "Confirmed" && bs.Booking.CreatedAt >= start && bs.Booking.CreatedAt <= end)
                })
                .OrderByDescending(m => m.Revenue)
                .Take(limit)
                .ToListAsync();

            var movieIds = topMoviesGrouped.Select(x => x.MovieId).ToList();
            var movies = await _context.Movies
                .IgnoreQueryFilters() // Fetch even if soft-deleted to keep history reports intact
                .Where(m => movieIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.Title);

            var topMovies = topMoviesGrouped.Select(x => new MovieRevenueDto
            {
                MovieId = x.MovieId,
                Title = movies.TryGetValue(x.MovieId, out var title) ? title : "Phim đã xóa",
                Revenue = x.Revenue,
                TicketsSold = x.TicketsSold
            }).ToList();

            return ApiResponse.Success(topMovies);
        }

        /// <summary>
        /// Fetches top cinemas ranked by sales revenue.
        /// </summary>
        public async Task<ApiResponse<List<CinemaRevenueDto>>> GetTopCinemasAsync(int limit = 5, DateTime? from = null, DateTime? to = null)
        {
            var start = from ?? DateTime.MinValue;
            var end = to ?? DateTime.UtcNow;

            var topCinemas = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && b.BookingStatus == "Confirmed")
                .GroupBy(b => new { b.Showtime.Hall.CinemaId, b.Showtime.Hall.Cinema.CinemaName })
                .Select(g => new CinemaRevenueDto
                {
                    CinemaId = g.Key.CinemaId,
                    Name = g.Key.CinemaName,
                    Revenue = g.Sum(b => b.TotalAmount),
                    BookingsCount = g.Count(),
                    OccupancyRate = 0.0 // Populated below
                })
                .OrderByDescending(c => c.Revenue)
                .Take(limit)
                .ToListAsync();

            // Calculate occupancy rate for each top cinema
            foreach (var tc in topCinemas)
            {
                tc.OccupancyRate = await CalculateCinemaOccupancyRateAsync(tc.CinemaId, start, end);
            }

            return ApiResponse.Success(topCinemas);
        }

        /// <summary>
        /// General catalog counters and active metrics.
        /// </summary>
        public async Task<ApiResponse<AdminStatisticsDto>> GetGeneralStatisticsAsync()
        {
            var cacheKey = "GeneralStatistics";
            if (_cache.TryGetValue(cacheKey, out AdminStatisticsDto? cachedStats) && cachedStats != null)
            {
                return ApiResponse.Success(cachedStats);
            }

            var totalMovies = await _context.Movies.CountAsync(m => !m.IsDeleted);
            var totalCinemas = await _context.Cinemas.CountAsync();
            var totalShowtimes = await _context.Showtimes.CountAsync();

            var activeUsers = await _context.Bookings
                .AsNoTracking()
                .Select(b => b.UserId)
                .Distinct()
                .CountAsync();

            var avgRating = await _context.Reviews.AnyAsync()
                ? await _context.Reviews.AverageAsync(r => r.Rating)
                : 0.0;

            var stats = new AdminStatisticsDto
            {
                TotalMovies = totalMovies,
                TotalCinemas = totalCinemas,
                TotalShowtimes = totalShowtimes,
                ActiveUsers = activeUsers,
                AverageMovieRating = Math.Round(avgRating, 1)
            };

            _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(5));
            return ApiResponse.Success(stats);
        }

        /// <summary>
        /// Generates monthly revenue and ticket sales trend lines.
        /// </summary>
        public async Task<ApiResponse<List<MonthlyRevenueDto>>> GetRevenueChartAsync(DateTime? from = null, DateTime? to = null)
        {
            var start = from ?? DateTime.UtcNow.AddMonths(-12);
            var end = to ?? DateTime.UtcNow;

            var monthlyRevenue = await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end && (p.PaymentStatus == "Completed" || p.PaymentStatus == "Success"))
                .GroupBy(p => new { Month = p.PaymentDate.Month, Year = p.PaymentDate.Year })
                .Select(g => new MonthlyRevenueDto
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Revenue = g.Sum(p => p.Amount),
                    BookingsCount = g.Count()
                })
                .OrderByDescending(g => g.Year)
                .ThenByDescending(g => g.Month)
                .ToListAsync();

            return ApiResponse.Success(monthlyRevenue);
        }

        /// <summary>
        /// Comprehensive booking conversion and peak hours performance report.
        /// </summary>
        public async Task<ApiResponse<BookingAnalyticsDto>> GetBookingAnalyticsAsync(DateTime? from = null, DateTime? to = null)
        {
            var start = from ?? DateTime.UtcNow.AddDays(-30);
            var end = to ?? DateTime.UtcNow;

            var total = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end)
                .CountAsync();

            var confirmed = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && b.BookingStatus == "Confirmed")
                .CountAsync();

            var pending = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && b.BookingStatus == "Pending")
                .CountAsync();

            var cancelled = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && b.BookingStatus == "Cancelled")
                .CountAsync();

            // SQL side GroupBy hour of the day
            var peakHours = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end)
                .GroupBy(b => b.CreatedAt.Hour)
                .Select(g => new PeakHourDto
                {
                    Hour = g.Key,
                    BookingsCount = g.Count()
                })
                .OrderByDescending(p => p.BookingsCount)
                .ToListAsync();

            var analytics = new BookingAnalyticsDto
            {
                TotalBookings = total,
                ConfirmedBookings = confirmed,
                PendingBookings = pending,
                CancelledBookings = cancelled,
                ConversionRate = total > 0 ? Math.Round(((double)confirmed / total) * 100, 2) : 0.0,
                CancellationRate = total > 0 ? Math.Round(((double)cancelled / total) * 100, 2) : 0.0,
                PeakBookingHours = peakHours
            };

            return ApiResponse.Success(analytics);
        }

        /// <summary>
        /// Calculates theater seat occupancy percentage.
        /// </summary>
        public async Task<ApiResponse<double>> GetOccupancyRateAsync(DateTime? from = null, DateTime? to = null)
        {
            var start = from ?? DateTime.UtcNow.AddDays(-30);
            var end = to ?? DateTime.UtcNow;

            // Total tickets sold
            var bookedSeatsCount = await _context.BookingSeats
                .AsNoTracking()
                .Where(bs => bs.Booking.CreatedAt >= start && bs.Booking.CreatedAt <= end && bs.Booking.BookingStatus == "Confirmed")
                .CountAsync();

            // Total scheduled capacity
            var showtimes = await _context.Showtimes
                .AsNoTracking()
                .Where(s => s.StartTime >= start && s.StartTime <= end)
                .ToListAsync();

            var totalCapacity = 0;
            if (showtimes.Any())
            {
                var hallIds = showtimes.Select(s => s.HallId).Distinct().ToList();
                var capacityMap = await _context.Seats
                    .AsNoTracking()
                    .Where(s => hallIds.Contains(s.HallId) && !s.IsDeleted)
                    .GroupBy(s => s.HallId)
                    .Select(g => new { HallId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.HallId, x => x.Count);

                foreach (var s in showtimes)
                {
                    if (capacityMap.TryGetValue(s.HallId, out var capacity))
                    {
                        totalCapacity += capacity;
                    }
                }
            }

            double occupancyRate = totalCapacity > 0
                ? Math.Round(((double)bookedSeatsCount / totalCapacity) * 100, 2)
                : 0.0;

            return ApiResponse.Success(occupancyRate);
        }

        #endregion

        #region Private Helper Methods

        private string GetDateRangeCacheSuffix(DateTime? from, DateTime? to)
        {
            var startStr = from?.ToString("yyyyMMdd") ?? "min";
            var endStr = to?.ToString("yyyyMMdd") ?? "now";
            return $"{startStr}_{endStr}";
        }

        private async Task<double> CalculateCinemaOccupancyRateAsync(int cinemaId, DateTime start, DateTime end)
        {
            var bookedSeatsCount = await _context.BookingSeats
                .AsNoTracking()
                .Where(bs => bs.Booking.Showtime.Hall.CinemaId == cinemaId && bs.Booking.CreatedAt >= start && bs.Booking.CreatedAt <= end && bs.Booking.BookingStatus == "Confirmed")
                .CountAsync();

            var showtimes = await _context.Showtimes
                .AsNoTracking()
                .Where(s => s.Hall.CinemaId == cinemaId && s.StartTime >= start && s.StartTime <= end)
                .ToListAsync();

            var totalCapacity = 0;
            if (showtimes.Any())
            {
                var hallIds = showtimes.Select(s => s.HallId).Distinct().ToList();
                var capacityMap = await _context.Seats
                    .AsNoTracking()
                    .Where(s => hallIds.Contains(s.HallId) && !s.IsDeleted)
                    .GroupBy(s => s.HallId)
                    .Select(g => new { HallId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.HallId, x => x.Count);

                foreach (var s in showtimes)
                {
                    if (capacityMap.TryGetValue(s.HallId, out var capacity))
                    {
                        totalCapacity += capacity;
                    }
                }
            }

            return totalCapacity > 0
                ? Math.Round(((double)bookedSeatsCount / totalCapacity) * 100, 2)
                : 0.0;
        }

        #endregion
    }
}
