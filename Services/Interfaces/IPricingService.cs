using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.Models.Showtimes;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface IPricingService
    {
        Task<ApiResponse<IEnumerable<PricingRuleDto>>> GetAllRulesAsync(CancellationToken cancellationToken = default);
        Task<ApiResponse<PricingRuleDto>> GetRuleByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ApiResponse<PricingRuleDto>> CreateRuleAsync(PricingRuleCreateUpdateDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<PricingRuleDto>> UpdateRuleAsync(int id, PricingRuleCreateUpdateDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> DeleteRuleAsync(int id, CancellationToken cancellationToken = default);
        Task<ApiResponse<PricingComputeResponseDto>> ComputePriceAsync(PricingComputeRequestDto request, CancellationToken cancellationToken = default);
    }
}
