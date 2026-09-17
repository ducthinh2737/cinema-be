using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.Models.Promotions;
using CinemaBooking.API.Models.Bookings;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.Models.Users;
using CinemaBooking.API.DTOs.Promotions;

namespace CinemaBooking.API.Repositories.Interfaces
{
    public interface IPromotionRepository
    {
        Task<(IEnumerable<Promotion> Promotions, int TotalCount)> GetPagedPromotionsAsync(PromotionQueryParameters queryParams);
        Task<Promotion?> GetPromotionByIdAsync(int id);
        Task<Promotion?> GetPromotionByCodeAsync(string code);
        Task AddPromotionAsync(Promotion promotion);
        Task UpdatePromotionAsync(Promotion promotion);
        Task DeletePromotionAsync(Promotion promotion);

        // User Promotion
        Task<UserPromotion?> GetUserPromotionAsync(int userId, int promotionId);
        Task AddUserPromotionAsync(UserPromotion userPromotion);
        Task UpdateUserPromotionAsync(UserPromotion userPromotion);

        // Booking & BookingPromotion
        Task<Booking?> GetBookingByIdAsync(int bookingId);
        Task UpdateBookingAsync(Booking booking);
        Task AddBookingPromotionAsync(BookingPromotion bookingPromotion);
        Task<Showtime?> GetShowtimeByIdAsync(int showtimeId);
        Task<User?> GetUserByIdWithTierAsync(int userId);

        Task<bool> SaveChangesAsync();
    }
}
