using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Promotions;
using CinemaBooking.API.Models.Bookings;
using CinemaBooking.API.DTOs.Promotions;
using CinemaBooking.API.Repositories.Interfaces;

namespace CinemaBooking.API.Repositories.Implementations
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly CinemaDbContext _context;

        public PromotionRepository(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Promotion> Promotions, int TotalCount)> GetPagedPromotionsAsync(PromotionQueryParameters queryParams)
        {
            var query = _context.Promotions.AsQueryable();

            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                query = query.Where(p => p.PromoCode.Contains(queryParams.Search));
            }

            if (queryParams.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == queryParams.IsActive.Value);
            }

            int totalCount = await query.CountAsync();
            var promotions = await query
                .OrderBy(p => p.PromotionId)
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return (promotions, totalCount);
        }

        public async Task<Promotion?> GetPromotionByIdAsync(int id)
        {
            return await _context.Promotions.FirstOrDefaultAsync(p => p.PromotionId == id);
        }

        public async Task<Promotion?> GetPromotionByCodeAsync(string code)
        {
            return await _context.Promotions.FirstOrDefaultAsync(p => p.PromoCode == code);
        }

        public async Task AddPromotionAsync(Promotion promotion)
        {
            await _context.Promotions.AddAsync(promotion);
        }

        public Task UpdatePromotionAsync(Promotion promotion)
        {
            _context.Promotions.Update(promotion);
            return Task.CompletedTask;
        }

        public Task DeletePromotionAsync(Promotion promotion)
        {
            promotion.IsDeleted = true;
            promotion.DeletedAt = DateTime.UtcNow;
            _context.Promotions.Update(promotion);
            return Task.CompletedTask;
        }

        // User Promotion
        public async Task<UserPromotion?> GetUserPromotionAsync(int userId, int promotionId)
        {
            return await _context.UserPromotions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PromotionId == promotionId);
        }

        public async Task AddUserPromotionAsync(UserPromotion userPromotion)
        {
            await _context.UserPromotions.AddAsync(userPromotion);
        }

        public Task UpdateUserPromotionAsync(UserPromotion userPromotion)
        {
            _context.UserPromotions.Update(userPromotion);
            return Task.CompletedTask;
        }

        // Booking & BookingPromotion
        public async Task<Booking?> GetBookingByIdAsync(int bookingId)
        {
            return await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public Task UpdateBookingAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
            return Task.CompletedTask;
        }

        public async Task AddBookingPromotionAsync(BookingPromotion bookingPromotion)
        {
            await _context.BookingPromotions.AddAsync(bookingPromotion);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
