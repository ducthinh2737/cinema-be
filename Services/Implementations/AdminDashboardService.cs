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
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end && (p.PaymentStatus == "Completed" || p.PaymentStatus == "Success" || p.PaymentStatus == "Paid"))
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
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && (b.BookingStatus == "Confirmed" || b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn"))
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
                .Where(p => p.PaymentDate >= prevStart && p.PaymentDate <= prevEnd && (p.PaymentStatus == "Completed" || p.PaymentStatus == "Success" || p.PaymentStatus == "Paid"))
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
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end && (p.PaymentStatus == "Completed" || p.PaymentStatus == "Success" || p.PaymentStatus == "Paid"));

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
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && (b.BookingStatus == "Confirmed" || b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn"))
                .GroupBy(b => b.Showtime.MovieId)
                .Select(g => new
                {
                    MovieId = g.Key,
                    Revenue = g.Sum(b => b.TotalAmount),
                    TicketsSold = _context.BookingSeats.Count(bs => bs.Booking.Showtime.MovieId == g.Key && (bs.Booking.BookingStatus == "Confirmed" || bs.Booking.BookingStatus == "Paid" || bs.Booking.BookingStatus == "CheckedIn") && bs.Booking.CreatedAt >= start && bs.Booking.CreatedAt <= end)
                })
                .OrderByDescending(m => m.Revenue)
                .Take(limit)
                .ToListAsync();

            var movieIds = topMoviesGrouped.Select(x => x.MovieId).ToList();
            var movies = await _context.Movies
                .IgnoreQueryFilters() // Fetch even if soft-deleted to keep history reports intact
                .Where(m => movieIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.Title);

            var topMovies = new List<MovieRevenueDto>();
            foreach (var x in topMoviesGrouped)
            {
                var showtimes = await _context.Showtimes
                    .AsNoTracking()
                    .Where(s => s.MovieId == x.MovieId && s.StartTime >= start && s.StartTime <= end)
                    .Select(s => new { s.ShowtimeId, s.HallId })
                    .ToListAsync();

                int showtimesCount = showtimes.Count;
                double occupancyRate = 0.0;

                if (showtimes.Any())
                {
                    var hallIds = showtimes.Select(s => s.HallId).Distinct().ToList();
                    var capacityMap = await _context.Seats
                        .AsNoTracking()
                        .Where(s => hallIds.Contains(s.HallId) && !s.IsDeleted)
                        .GroupBy(s => s.HallId)
                        .Select(g => new { HallId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(h => h.HallId, h => h.Count);

                    int totalCapacity = 0;
                    foreach (var s in showtimes)
                    {
                        if (capacityMap.TryGetValue(s.HallId, out var capacity))
                        {
                            totalCapacity += capacity;
                        }
                    }

                    if (totalCapacity > 0)
                    {
                        occupancyRate = Math.Round(((double)x.TicketsSold / totalCapacity) * 100, 2);
                    }
                }

                topMovies.Add(new MovieRevenueDto
                {
                    MovieId = x.MovieId,
                    Title = movies.TryGetValue(x.MovieId, out var title) ? title : "Phim đã xóa",
                    Revenue = x.Revenue,
                    TicketsSold = x.TicketsSold,
                    OccupancyRate = occupancyRate,
                    ShowtimesCount = showtimesCount
                });
            }

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
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && (b.BookingStatus == "Confirmed" || b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn"))
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
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end && (p.PaymentStatus == "Completed" || p.PaymentStatus == "Success" || p.PaymentStatus == "Paid"))
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
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && (b.BookingStatus == "Confirmed" || b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn"))
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
                .Where(bs => bs.Booking.CreatedAt >= start && bs.Booking.CreatedAt <= end && (bs.Booking.BookingStatus == "Confirmed" || bs.Booking.BookingStatus == "Paid" || bs.Booking.BookingStatus == "CheckedIn"))
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

        public async Task<ApiResponse<DashboardDataDto>> GetUnifiedDashboardDataAsync(DateTime? from = null, DateTime? to = null)
        {
            var end = to ?? DateTime.UtcNow;
            var start = from ?? end.AddDays(-30);

            // --- 1. KPIs ---
            var totalRevenue = await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end && (p.PaymentStatus == "Completed" || p.PaymentStatus == "Success" || p.PaymentStatus == "Paid"))
                .SumAsync(p => p.Amount);

            var ticketsSold = await _context.BookingSeats
                .AsNoTracking()
                .Where(bs => bs.Booking.CreatedAt >= start && bs.Booking.CreatedAt <= end && (bs.Booking.BookingStatus == "Confirmed" || bs.Booking.BookingStatus == "Paid" || bs.Booking.BookingStatus == "CheckedIn"))
                .CountAsync();

            var activePromotions = await _context.Promotions.CountAsync(p => p.IsActive && !p.IsDeleted);

            var windowSize = end - start;
            var prevStart = start - windowSize;
            var prevEnd = start;

            var prevRevenue = await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= prevStart && p.PaymentDate <= prevEnd && (p.PaymentStatus == "Completed" || p.PaymentStatus == "Success" || p.PaymentStatus == "Paid"))
                .SumAsync(p => p.Amount);

            var prevTicketsSold = await _context.BookingSeats
                .AsNoTracking()
                .Where(bs => bs.Booking.CreatedAt >= prevStart && bs.Booking.CreatedAt <= prevEnd && (bs.Booking.BookingStatus == "Confirmed" || bs.Booking.BookingStatus == "Paid" || bs.Booking.BookingStatus == "CheckedIn"))
                .CountAsync();

            var prevActiveMembers = await _context.Users
                .Where(u => u.CreatedAt <= prevEnd)
                .CountAsync();

            var curActiveMembersCount = await _context.Users
                .Where(u => u.CreatedAt <= end)
                .CountAsync();

            decimal revenueGrowth = prevRevenue > 0m ? Math.Round(((totalRevenue - prevRevenue) / prevRevenue) * 100, 2) : 0m;
            decimal ticketGrowth = prevTicketsSold > 0 ? Math.Round(((decimal)(ticketsSold - prevTicketsSold) / prevTicketsSold) * 100, 2) : 0m;
            decimal memberGrowth = prevActiveMembers > 0 ? Math.Round(((decimal)(curActiveMembersCount - prevActiveMembers) / prevActiveMembers) * 100, 2) : 0m;

            // Calculate new KPIs
            double occupancyRate = (await GetOccupancyRateAsync(start, end)).Data;

            var localEnd = DateTime.SpecifyKind(end, DateTimeKind.Utc).ToLocalTime();
            var todayDate = localEnd.Date;
            var todayDateEnd = todayDate.AddDays(1).AddTicks(-1);
            var todayShowtimes = await _context.Showtimes
                .CountAsync(s => s.StartTime >= todayDate && s.StartTime <= todayDateEnd);

            var pendingPaymentsCount = await _context.Bookings
                .CountAsync(b => b.BookingStatus == "Pending");

            var activeMoviesCount = await _context.Movies
                .CountAsync(m => !m.IsDeleted);

            // Compute historical comparison / growths
            double prevOccupancyRate = (await GetOccupancyRateAsync(prevStart, prevEnd)).Data;
            decimal occupancyGrowth = prevOccupancyRate > 0 ? Math.Round((decimal)((occupancyRate - prevOccupancyRate) / prevOccupancyRate) * 100, 2) : 0m;

            var yesterdayDate = todayDate.AddDays(-1);
            var yesterdayDateEnd = todayDate.AddTicks(-1);
            var yesterdayShowtimesCount = await _context.Showtimes
                .CountAsync(s => s.StartTime >= yesterdayDate && s.StartTime <= yesterdayDateEnd);
            decimal showtimesGrowth = yesterdayShowtimesCount > 0 ? Math.Round(((decimal)(todayShowtimes - yesterdayShowtimesCount) / yesterdayShowtimesCount) * 100, 2) : 0m;

            var prevPendingPaymentsCount = await _context.Bookings
                .CountAsync(b => b.BookingStatus == "Pending" && b.CreatedAt >= prevStart && b.CreatedAt <= prevEnd);
            decimal pendingPaymentsGrowth = prevPendingPaymentsCount > 0 ? Math.Round(((decimal)(pendingPaymentsCount - prevPendingPaymentsCount) / prevPendingPaymentsCount) * 100, 2) : 0m;

            var prevActiveMoviesCount = await _context.Movies
                .CountAsync(m => !m.IsDeleted && m.CreatedAt <= prevEnd);
            decimal activeMoviesGrowth = prevActiveMoviesCount > 0 ? Math.Round(((decimal)(activeMoviesCount - prevActiveMoviesCount) / prevActiveMoviesCount) * 100, 2) : 0m;

            var kpis = new DashboardKpisDto
            {
                TotalRevenue = totalRevenue,
                RevenueGrowth = revenueGrowth,
                TicketsSold = ticketsSold,
                TicketGrowth = ticketGrowth,
                ActiveMembers = curActiveMembersCount,
                MemberGrowth = memberGrowth,
                ActivePromotions = activePromotions,
                AverageOccupancyRate = occupancyRate,
                OccupancyGrowth = occupancyGrowth,
                TodayShowtimes = todayShowtimes,
                ShowtimesGrowth = showtimesGrowth,
                PendingPaymentsCount = pendingPaymentsCount,
                PendingPaymentsGrowth = pendingPaymentsGrowth,
                ActiveMoviesCount = activeMoviesCount,
                ActiveMoviesGrowth = activeMoviesGrowth
            };

            // --- 2. Revenue7Days ---
            var last7DaysStart = end.AddDays(-7);
            var dailyPayments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= last7DaysStart && p.PaymentDate <= end && (p.PaymentStatus == "Completed" || p.PaymentStatus == "Success" || p.PaymentStatus == "Paid"))
                .ToListAsync();

            var revenue7Days = new List<DailyRevenueDto>();
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = localEnd.AddDays(-i).Date;
                var dailySum = dailyPayments.Where(p => DateTime.SpecifyKind(p.PaymentDate, DateTimeKind.Utc).ToLocalTime().Date == targetDate).Sum(p => p.Amount);
                var dailyCount = dailyPayments.Where(p => DateTime.SpecifyKind(p.PaymentDate, DateTimeKind.Utc).ToLocalTime().Date == targetDate).Count();
                
                revenue7Days.Add(new DailyRevenueDto
                {
                    Date = targetDate,
                    Revenue = dailySum,
                    BookingsCount = dailyCount
                });
            }

            // --- 3. Top Movies ---
            var topMovies = (await GetTopMoviesAsync(5, start, end)).Data;

            // --- 4. OccupancyBySeatType ---
            var seatTypeOccupancy = await _context.BookingSeats
                .AsNoTracking()
                .Where(bs => bs.Booking.CreatedAt >= start && bs.Booking.CreatedAt <= end && (bs.Booking.BookingStatus == "Confirmed" || bs.Booking.BookingStatus == "Paid" || bs.Booking.BookingStatus == "CheckedIn"))
                .GroupBy(bs => bs.Seat.SeatType.TypeName)
                .Select(g => new SeatTypeOccupancyDto
                {
                    SeatType = g.Key,
                    Value = g.Count()
                })
                .ToListAsync();

            if (!seatTypeOccupancy.Any())
            {
                seatTypeOccupancy = new List<SeatTypeOccupancyDto>
                {
                    new() { SeatType = "Thường", Value = 0 },
                    new() { SeatType = "VIP", Value = 0 },
                    new() { SeatType = "Sweetbox", Value = 0 }
                };
            }

            // --- 5. Top Customers ---
            var topCustomers = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && (b.BookingStatus == "Confirmed" || b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn"))
                .GroupBy(b => new { b.UserId, b.User.FullName, b.User.Email })
                .Select(g => new TopCustomerDto
                {
                    UserId = g.Key.UserId,
                    Name = g.Key.FullName ?? "Khách vãng lai",
                    Email = g.Key.Email ?? "no-email@cinemapass.vn",
                    TicketsBought = _context.BookingSeats.Count(bs => bs.Booking.UserId == g.Key.UserId && (bs.Booking.BookingStatus == "Confirmed" || bs.Booking.BookingStatus == "Paid" || bs.Booking.BookingStatus == "CheckedIn") && bs.Booking.CreatedAt >= start && bs.Booking.CreatedAt <= end),
                    TotalSpent = g.Sum(b => b.TotalAmount)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(5)
                .ToListAsync();

            // --- 6. RevenueByHall ---
            var revenueByHall = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && (b.BookingStatus == "Confirmed" || b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn"))
                .GroupBy(b => new { b.Showtime.HallId, b.Showtime.Hall.HallName, b.Showtime.Hall.Cinema.CinemaName })
                .Select(g => new HallRevenueDto
                {
                    HallId = g.Key.HallId,
                    HallName = g.Key.HallName,
                    CinemaName = g.Key.CinemaName,
                    Revenue = g.Sum(b => b.TotalAmount),
                    TicketsSold = _context.BookingSeats.Count(bs => bs.Booking.Showtime.HallId == g.Key.HallId && (bs.Booking.BookingStatus == "Confirmed" || bs.Booking.BookingStatus == "Paid" || bs.Booking.BookingStatus == "CheckedIn") && bs.Booking.CreatedAt >= start && bs.Booking.CreatedAt <= end),
                    OccupancyRate = 0.0
                })
                .OrderByDescending(h => h.Revenue)
                .Take(5)
                .ToListAsync();

            foreach (var h in revenueByHall)
            {
                var showtimes = await _context.Showtimes
                    .AsNoTracking()
                    .Where(s => s.HallId == h.HallId && s.StartTime >= start && s.StartTime <= end)
                    .Select(s => s.ShowtimeId)
                    .ToListAsync();
                
                if (showtimes.Any())
                {
                    var capacity = await _context.Seats.CountAsync(x => x.HallId == h.HallId && !x.IsDeleted);
                    var totalCapacity = capacity * showtimes.Count;
                    if (totalCapacity > 0)
                    {
                        h.OccupancyRate = Math.Round(((double)h.TicketsSold / totalCapacity) * 100, 2);
                    }
                }
            }

            // --- 7. Top Showtimes ---
            var topShowtimesGrouped = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && (b.BookingStatus == "Confirmed" || b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn"))
                .GroupBy(b => b.ShowtimeId)
                .Select(g => new
                {
                    ShowtimeId = g.Key,
                    TicketsSold = _context.BookingSeats.Count(bs => bs.Booking.ShowtimeId == g.Key && (bs.Booking.BookingStatus == "Confirmed" || bs.Booking.BookingStatus == "Paid" || bs.Booking.BookingStatus == "CheckedIn") && bs.Booking.CreatedAt >= start && bs.Booking.CreatedAt <= end)
                })
                .OrderByDescending(s => s.TicketsSold)
                .Take(5)
                .ToListAsync();

            var topShowtimeIds = topShowtimesGrouped.Select(x => x.ShowtimeId).ToList();
            var showtimesMap = await _context.Showtimes
                .AsNoTracking()
                .Where(s => topShowtimeIds.Contains(s.ShowtimeId))
                .Select(s => new
                {
                    s.ShowtimeId,
                    MovieTitle = s.Movie.Title,
                    HallName = s.Hall.HallName,
                    CinemaName = s.Hall.Cinema.CinemaName,
                    s.StartTime,
                    HallId = s.HallId
                })
                .ToDictionaryAsync(s => s.ShowtimeId);

            var topShowtimes = new List<TopShowtimeDto>();
            foreach (var tg in topShowtimesGrouped)
            {
                if (showtimesMap.TryGetValue(tg.ShowtimeId, out var s))
                {
                    var capacity = await _context.Seats.CountAsync(x => x.HallId == s.HallId && !x.IsDeleted);
                    topShowtimes.Add(new TopShowtimeDto
                    {
                        ShowtimeId = tg.ShowtimeId,
                        MovieTitle = s.MovieTitle,
                        HallName = s.HallName,
                        CinemaName = s.CinemaName,
                        StartTime = s.StartTime,
                        TicketsSold = tg.TicketsSold,
                        OccupancyRate = capacity > 0 ? Math.Round(((double)tg.TicketsSold / capacity) * 100, 2) : 0.0
                    });
                }
            }

            // --- 8. GoldenHours ---
            var allBookings = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && (b.BookingStatus == "Confirmed" || b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn"))
                .Select(b => new
                {
                    b.CreatedAt,
                    b.TotalAmount,
                    TicketCount = _context.BookingSeats.Count(bs => bs.BookingId == b.BookingId)
                })
                .ToListAsync();

            var goldenHourRanges = new[]
            {
                new { Range = "08:00 - 11:00", StartHour = 8, EndHour = 11 },
                new { Range = "11:00 - 14:00", StartHour = 11, EndHour = 14 },
                new { Range = "14:00 - 17:00", StartHour = 14, EndHour = 17 },
                new { Range = "17:00 - 20:00", StartHour = 17, EndHour = 20 },
                new { Range = "20:00 - 23:00", StartHour = 20, EndHour = 23 },
                new { Range = "23:00 - 02:00", StartHour = 23, EndHour = 26 }
            };

            var goldenHoursList = new List<GoldenHourRevenueDto>();
            foreach (var r in goldenHourRanges)
            {
                var bookingsInRange = allBookings.Where(b => {
                    var h = DateTime.SpecifyKind(b.CreatedAt, DateTimeKind.Utc).ToLocalTime().Hour;
                    if (r.StartHour == 23)
                    {
                        return h >= 23 || h < 2;
                    }
                    return h >= r.StartHour && h < r.EndHour;
                });

                goldenHoursList.Add(new GoldenHourRevenueDto
                {
                    HourRange = r.Range,
                    Revenue = bookingsInRange.Sum(b => b.TotalAmount),
                    TicketsSold = bookingsInRange.Sum(b => b.TicketCount)
                });
            }

            // --- 9. Alerts ---
            var now = DateTime.UtcNow;
            var alerts = new List<DashboardAlertDto>();
            int alertId = 1;

            // 1. Promotions
            var activePromos = await _context.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive && !p.IsDeleted && p.EndDate >= now)
                .OrderBy(p => p.EndDate)
                .Take(2)
                .ToListAsync();
            foreach (var p in activePromos)
            {
                var remainingDays = (p.EndDate - now).TotalDays;
                if (remainingDays <= 30)
                {
                    alerts.Add(new DashboardAlertDto
                    {
                        Id = alertId++,
                        Category = "Sắp hết khuyến mãi",
                        Title = p.PromoCode,
                        Detail = remainingDays <= 0 ? "Hết hạn hôm nay" : $"Còn {Math.Ceiling(remainingDays)} ngày hết hạn",
                        Severity = "warning",
                        Type = "promotion"
                    });
                }
            }

            // 2. Low occupancy showtimes (< 20%)
            var upcomingShowtimes = await _context.Showtimes
                .AsNoTracking()
                .Include(s => s.Movie)
                .Where(s => s.StartTime >= now && s.StartTime <= now.AddDays(1))
                .OrderBy(s => s.StartTime)
                .Take(5)
                .ToListAsync();
            foreach (var s in upcomingShowtimes)
            {
                var totalSeats = await _context.Seats.CountAsync(x => x.HallId == s.HallId && !x.IsDeleted);
                var bookedSeats = await _context.BookingSeats.CountAsync(bs => bs.Booking.ShowtimeId == s.ShowtimeId && (bs.Booking.BookingStatus == "Confirmed" || bs.Booking.BookingStatus == "Paid" || bs.Booking.BookingStatus == "CheckedIn"));
                double occRate = totalSeats > 0 ? (double)bookedSeats / totalSeats : 0.0;
                if (occRate < 0.2)
                {
                    alerts.Add(new DashboardAlertDto
                    {
                        Id = alertId++,
                        Category = "Suất chiếu ít khách",
                        Title = s.Movie.Title,
                        Subtitle = DateTime.SpecifyKind(s.StartTime, DateTimeKind.Utc).ToLocalTime().ToString("HH:mm"),
                        Detail = $"Chỉ bán {bookedSeats}/{totalSeats} ghế (Lấp đầy {Math.Round(occRate * 100)}%)",
                        Severity = "danger",
                        Type = "occupancy"
                    });
                }
            }

            // 3. Movies ending soon
            var endingMovies = await _context.Movies
                .AsNoTracking()
                .Where(m => !m.IsDeleted && m.EndDate >= now && m.EndDate <= now.AddDays(30))
                .OrderBy(m => m.EndDate)
                .Take(2)
                .ToListAsync();
            foreach (var m in endingMovies)
            {
                var remainingDays = (m.EndDate - now).TotalDays;
                alerts.Add(new DashboardAlertDto
                {
                    Id = alertId++,
                    Category = "Phim sắp ngừng chiếu",
                    Title = m.Title,
                    Detail = remainingDays <= 0 ? "Ngừng chiếu hôm nay" : $"Còn {Math.Ceiling(remainingDays)} ngày",
                    Severity = "info",
                    Type = "movie"
                });
            }

            // 4. Halls under maintenance
            var maintenanceHalls = await _context.Halls
                .AsNoTracking()
                .Where(h => !h.IsDeleted && h.Description != null && (h.Description.Contains("bảo trì") || h.Description.Contains("bảo dưỡng")))
                .ToListAsync();
            foreach (var h in maintenanceHalls)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Id = alertId++,
                    Category = "Phòng chiếu bảo trì",
                    Title = h.HallName,
                    Detail = h.Description ?? "Đang tiến hành bảo trì định kỳ",
                    Severity = "warning",
                    Type = "maintenance"
                });
            }

            // 5. Long pending bookings (> 15 minutes)
            var pendingLimitTime = now.AddMinutes(-15);
            var longPendingBookings = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.BookingStatus == "Pending" && b.CreatedAt <= pendingLimitTime)
                .OrderBy(b => b.CreatedAt)
                .Take(3)
                .ToListAsync();
            foreach (var b in longPendingBookings)
            {
                var duration = now - b.CreatedAt;
                alerts.Add(new DashboardAlertDto
                {
                    Id = alertId++,
                    Category = "Vé chờ thanh toán lâu",
                    Title = b.BookingCode,
                    Detail = $"Đã chờ {Math.Round(duration.TotalMinutes)} phút (Tổng: {b.TotalAmount:N0}đ)",
                    Severity = "danger",
                    Type = "pending_payment"
                });
            }

            // --- 9. Concession Analytics ---
            var confirmedBookingIdsQuery = _context.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end && (b.BookingStatus == "Confirmed" || b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn"))
                .Select(b => b.BookingId);

            var totalConfirmedBookingsCount = await confirmedBookingIdsQuery.CountAsync();

            var orderCombosQuery = _context.OrderCombos
                .AsNoTracking()
                .Where(oc => confirmedBookingIdsQuery.Contains(oc.BookingId));

            var totalComboRevenue = await orderCombosQuery.SumAsync(oc => oc.Price * oc.Quantity);

            var bookingsWithCombosCount = await orderCombosQuery
                .Select(oc => oc.BookingId)
                .Distinct()
                .CountAsync();

            double attachRate = totalConfirmedBookingsCount > 0 
                ? Math.Round(((double)bookingsWithCombosCount / totalConfirmedBookingsCount) * 100, 2)
                : 0.0;

            var avgBookingWithComboAmount = bookingsWithCombosCount > 0
                ? await _context.Bookings
                    .AsNoTracking()
                    .Where(b => confirmedBookingIdsQuery.Contains(b.BookingId) && _context.OrderCombos.Any(oc => oc.BookingId == b.BookingId))
                    .AverageAsync(b => b.TotalAmount)
                : 0m;

            // Group by Combo
            var comboSalesGrouped = await orderCombosQuery
                .GroupBy(oc => new { oc.ComboId, Name = oc.Combo.Name })
                .Select(g => new ComboSalesDto
                {
                    ComboId = g.Key.ComboId,
                    Name = g.Key.Name,
                    QuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Price * x.Quantity)
                })
                .ToListAsync();

            // Handle potential combos that haven't sold anything yet, so we show them in slow selling
            var allActiveCombos = await _context.Combos
                .AsNoTracking()
                .Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            var allSalesMap = comboSalesGrouped.ToDictionary(x => x.ComboId);
            var completeSalesList = new List<ComboSalesDto>();
            foreach (var c in allActiveCombos)
            {
                if (allSalesMap.TryGetValue(c.Id, out var sales))
                {
                    completeSalesList.Add(sales);
                }
                else
                {
                    completeSalesList.Add(new ComboSalesDto
                    {
                        ComboId = c.Id,
                        Name = c.Name,
                        QuantitySold = 0,
                        TotalRevenue = 0m
                    });
                }
            }

            var topSelling = completeSalesList
                .OrderByDescending(c => c.QuantitySold)
                .Take(5)
                .ToList();

            var slowSelling = completeSalesList
                .OrderBy(c => c.QuantitySold)
                .Take(5)
                .ToList();

            // Daily chart for the last 7 days
            var dailyComboRevenueChart = new List<DailyComboRevenueDto>();
            var last7DaysComboPayments = await _context.OrderCombos
                .AsNoTracking()
                .Where(oc => confirmedBookingIdsQuery.Contains(oc.BookingId) && oc.Booking.CreatedAt >= last7DaysStart)
                .Select(oc => new { oc.Booking.CreatedAt, oc.Price, oc.Quantity })
                .ToListAsync();

            for (int i = 6; i >= 0; i--)
            {
                var targetDate = localEnd.AddDays(-i).Date;
                var dailySum = last7DaysComboPayments
                    .Where(oc => DateTime.SpecifyKind(oc.CreatedAt, DateTimeKind.Utc).ToLocalTime().Date == targetDate)
                    .Sum(oc => oc.Price * oc.Quantity);

                var dailyQty = last7DaysComboPayments
                    .Where(oc => DateTime.SpecifyKind(oc.CreatedAt, DateTimeKind.Utc).ToLocalTime().Date == targetDate)
                    .Sum(oc => oc.Quantity);

                dailyComboRevenueChart.Add(new DailyComboRevenueDto
                {
                    Date = targetDate,
                    Revenue = dailySum,
                    QuantitySold = dailyQty
                });
            }

            var concessionAnalytics = new ConcessionAnalyticsDto
            {
                TotalComboRevenue = totalComboRevenue,
                ComboAttachRate = attachRate,
                AverageBookingComboValue = Math.Round(avgBookingWithComboAmount, 2),
                TopSellingCombos = topSelling,
                SlowSellingCombos = slowSelling,
                DailyComboRevenueChart = dailyComboRevenueChart
            };

            var dashboardData = new DashboardDataDto
            {
                Kpis = kpis,
                Revenue7Days = revenue7Days,
                TopMovies = topMovies,
                OccupancyBySeatType = seatTypeOccupancy,
                TopCustomers = topCustomers,
                RevenueByHall = revenueByHall,
                TopShowtimes = topShowtimes,
                GoldenHours = goldenHoursList.OrderByDescending(g => g.Revenue).ToList(),
                Alerts = alerts,
                ConcessionAnalytics = concessionAnalytics
            };

            return ApiResponse.Success(dashboardData);
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
                .Where(bs => bs.Booking.Showtime.Hall.CinemaId == cinemaId && bs.Booking.CreatedAt >= start && bs.Booking.CreatedAt <= end && (bs.Booking.BookingStatus == "Confirmed" || bs.Booking.BookingStatus == "Paid" || bs.Booking.BookingStatus == "CheckedIn"))
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
