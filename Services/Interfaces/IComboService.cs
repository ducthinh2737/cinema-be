using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Bookings;
using CinemaBooking.API.DTOs.Combos;
using CinemaBooking.API.DTOs.Common;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface IComboService
    {
        // Customer Combos
        Task<ApiResponse<PagedResultDto<ComboDto>>> GetActiveCombosAsync(ComboQueryParameters queryParams);
        Task<ApiResponse<ComboDto>> GetComboByIdAsync(int id);
        
        // Booking Combos mapping
        Task<ApiResponse<BookingResponseDto>> AddCombosToBookingAsync(int bookingId, List<OrderComboInputDto> combosDto, CancellationToken cancellationToken = default);
        Task<ApiResponse<BookingResponseDto>> UpdateBookingCombosAsync(int bookingId, List<OrderComboInputDto> combosDto, CancellationToken cancellationToken = default);
        Task<ApiResponse<BookingResponseDto>> DeleteCombosFromBookingAsync(int bookingId, CancellationToken cancellationToken = default);

        // Smart Recommendations
        Task<ApiResponse<IEnumerable<ComboRecommendationDto>>> GetRecommendedCombosAsync(int showtimeId, int ticketCount, CancellationToken cancellationToken = default);

        // Admin Combos Management
        Task<ApiResponse<PagedResultDto<ComboDto>>> GetPagedCombosAdminAsync(ComboQueryParameters queryParams);
        Task<ApiResponse<ComboDto>> CreateComboAsync(ComboCreateUpdateDto createDto);
        Task<ApiResponse<ComboDto>> UpdateComboAsync(int id, ComboCreateUpdateDto updateDto);
        Task<ApiResponse<bool>> DeleteComboAsync(int id);
        Task<ApiResponse<bool>> ToggleComboStatusAsync(int id, bool isActive);
        Task<ApiResponse<bool>> UpdateComboDisplayOrderAsync(int id, int displayOrder);

        // Admin Products Management (Ingredients)
        Task<ApiResponse<IEnumerable<ProductDto>>> GetAllProductsAsync();
        Task<ApiResponse<ProductDto>> CreateProductAsync(ProductCreateUpdateDto createDto);
        Task<ApiResponse<ProductDto>> UpdateProductAsync(int id, ProductCreateUpdateDto updateDto);
        Task<ApiResponse<bool>> DeleteProductAsync(int id);
    }
}
