using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Users;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface ILoyaltyService
    {
        Task<ApiResponse<LoyaltyDashboardDto>> GetLoyaltyDashboardAsync(int userId, CancellationToken cancellationToken = default);
        
        Task<ApiResponse<IEnumerable<LoyaltyTransactionDto>>> GetLoyaltyTransactionsAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
        
        Task<ApiResponse<LoyaltyRedeemValidateResultDto>> ValidateRedeemPointsAsync(int userId, LoyaltyRedeemValidateRequestDto request, CancellationToken cancellationToken = default);
        
        Task<ApiResponse<LoyaltyAdminStatsDto>> GetAdminLoyaltyStatsAsync(CancellationToken cancellationToken = default);
        
        Task<ApiResponse<IEnumerable<LoyaltyTransactionDto>>> GetAllTransactionsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
        
        Task<ApiResponse<bool>> AdjustPointsManuallyAsync(LoyaltyPointsAdjustDto dto, CancellationToken cancellationToken = default);
        
        Task<ApiResponse<int>> CreatePendingEarnPointsAsync(int userId, int bookingId, decimal actualPaidAmount, CancellationToken cancellationToken = default);
        
        Task<ApiResponse<bool>> RedeemPointsForBookingAsync(int userId, int bookingId, int pointsToRedeem, CancellationToken cancellationToken = default);
        
        Task<ApiResponse<bool>> RefundRedeemedPointsAsync(int bookingId, CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> CompleteEarnPointsAsync(int bookingId, CancellationToken cancellationToken = default);
    }
}
