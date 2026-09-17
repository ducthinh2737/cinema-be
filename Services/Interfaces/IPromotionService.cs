using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Promotions;
using CinemaBooking.API.DTOs.Common;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface IPromotionService
    {
        Task<PagedResultDto<PromotionDto>> GetPagedPromotionsAsync(PromotionQueryParameters queryParams);
        Task<PromotionDto?> GetPromotionByIdAsync(int id);
        Task<PromotionDto> CreatePromotionAsync(PromotionCreateDto createDto);
        Task<PromotionDto?> UpdatePromotionAsync(int id, PromotionUpdateDto updateDto);
        Task<bool> DeletePromotionAsync(int id);
        Task<PromotionDto?> TogglePromotionStatusAsync(int id);

        // Validation & Application
        Task<PromotionValidateResultDto> ValidatePromotionAsync(PromotionValidateDto validateDto);
        Task<PromotionApplyResultDto> ApplyPromotionAsync(PromotionApplyDto applyDto);
    }
}
