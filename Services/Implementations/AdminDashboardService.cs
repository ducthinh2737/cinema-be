using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Admin;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Services.Implementations
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly CinemaDbContext _context;

        public AdminDashboardService(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardDto> GetDashboardSummaryAsync()
        {
            var payments = await _context.Payments
                .Where(p => p.PaymentStatus == "Completed" || p.PaymentStatus == "Success")
                .ToListAsync();

            var totalRevenue = payments.Sum(p => p.Amount);
            var totalBookings = await _context.Bookings.CountAsync();
            var totalUsers = await _context.Users.CountAsync();

            var confirmedBookings = await _context.Bookings.CountAsync(b => b.BookingStatus == "Confirmed");
            double conversionRate = totalBookings > 0 
                ? Math.Round(((double)confirmedBookings / totalBookings) * 100, 2) 
                : 0.0;

            var topMovies = await GetTopMoviesAsync(5);
            var topCinemas = await GetTopCinemasAsync(5);

            var monthlyRevenueChart = await _context.Payments
                .Where(p => p.PaymentStatus == "Completed" || p.PaymentStatus == "Success")
                .GroupBy(p => new { p.PaymentDate.Month, p.PaymentDate.Year })
                .Select(g => new MonthlyRevenueDto
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Revenue = g.Sum(p => p.Amount)
                })
                .OrderByDescending(g => g.Year)
                .ThenByDescending(g => g.Month)
                .Take(12)
                .ToListAsync();

            return new AdminDashboardDto
            {
                TotalRevenue = totalRevenue,
                TotalBookings = totalBookings,
                TotalUsers = totalUsers,
                ConversionRate = conversionRate,
                TopMovies = topMovies,
                TopCinemas = topCinemas,
                MonthlyRevenueChart = monthlyRevenueChart
            };
        }

        public async Task<RevenueStatsDto> GetRevenueStatisticsAsync()
        {
            var payments = await _context.Payments
                .Where(p => p.PaymentStatus == "Completed" || p.PaymentStatus == "Success")
                .ToListAsync();

            return new RevenueStatsDto
            {
                CashRevenue = payments.Where(p => p.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount),
                MomoRevenue = payments.Where(p => p.PaymentMethod.Equals("Momo", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount),
                VNPayRevenue = payments.Where(p => p.PaymentMethod.Equals("VNPay", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount),
                ZaloPayRevenue = payments.Where(p => p.PaymentMethod.Equals("ZaloPay", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount),
                PayPalRevenue = payments.Where(p => p.PaymentMethod.Equals("PayPal", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount),
                Total = payments.Sum(p => p.Amount)
            };
        }

        public async Task<List<MovieRevenueDto>> GetTopMoviesAsync(int limit = 5)
        {
            return await _context.Bookings
                .Where(b => b.BookingStatus == "Confirmed")
                .GroupBy(b => new { b.Showtime.MovieId, b.Showtime.Movie.Title })
                .Select(g => new MovieRevenueDto
                {
                    MovieId = g.Key.MovieId,
                    Title = g.Key.Title,
                    Revenue = g.Sum(b => b.TotalAmount),
                    TicketsSold = g.Sum(b => b.BookingSeats.Count)
                })
                .OrderByDescending(m => m.Revenue)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<CinemaRevenueDto>> GetTopCinemasAsync(int limit = 5)
        {
            return await _context.Bookings
                .Where(b => b.BookingStatus == "Confirmed")
                .GroupBy(b => new { b.Showtime.Hall.CinemaId, b.Showtime.Hall.Cinema.CinemaName })
                .Select(g => new CinemaRevenueDto
                {
                    CinemaId = g.Key.CinemaId,
                    Name = g.Key.CinemaName,
                    Revenue = g.Sum(b => b.TotalAmount),
                    BookingsCount = g.Count()
                })
                .OrderByDescending(c => c.Revenue)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<AdminStatisticsDto> GetGeneralStatisticsAsync()
        {
            var totalMovies = await _context.Movies.CountAsync(m => !m.IsDeleted);
            var totalCinemas = await _context.Cinemas.CountAsync();
            var totalShowtimes = await _context.Showtimes.CountAsync();
            
            // Active users defined as users who have placed bookings
            var activeUsers = await _context.Bookings
                .Select(b => b.UserId)
                .Distinct()
                .CountAsync();

            var avgRating = await _context.Reviews.AnyAsync() 
                ? await _context.Reviews.AverageAsync(r => r.Rating) 
                : 0.0;

            return new AdminStatisticsDto
            {
                TotalMovies = totalMovies,
                TotalCinemas = totalCinemas,
                TotalShowtimes = totalShowtimes,
                ActiveUsers = activeUsers,
                AverageMovieRating = Math.Round(avgRating, 1)
            };
        }
    }
}
