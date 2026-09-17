using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Cinemas;
using CinemaBooking.API.DTOs.Common;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface ICinemaService
    {
        // Cinema
        Task<PagedResultDto<CinemaDto>> GetPagedCinemasAsync(CinemaQueryParameters queryParams);
        Task<CinemaDto?> GetCinemaByIdAsync(int id);
        Task<CinemaDto> CreateCinemaAsync(CinemaCreateDto createDto);
        Task<CinemaDto?> UpdateCinemaAsync(int id, CinemaUpdateDto updateDto);
        Task<bool> DeleteCinemaAsync(int id);
        Task<CinemaDto?> UpdateCinemaImageAsync(int id, string imageUrl);
        Task<CinemaDto?> UpdateCinemaLogoAsync(int id, string logoUrl);
        Task<CinemaDto?> UpdateCinemaBannerAsync(int id, string bannerUrl);
        Task<CinemaDto?> AddCinemaGalleryImageAsync(int id, string imageUrl);

        // Hall
        Task<IEnumerable<HallDto>> GetHallsByCinemaIdAsync(int cinemaId);
        Task<HallDto?> GetHallByIdAsync(int id);
        Task<HallDto> CreateHallAsync(HallCreateDto createDto);
        Task<HallDto?> UpdateHallAsync(int id, HallUpdateDto updateDto);
        Task<bool> DeleteHallAsync(int id);

        // Seat
        Task<IEnumerable<SeatDto>> GetSeatsByHallIdAsync(int hallId);
        Task<SeatDto?> GetSeatByIdAsync(int id);
        Task<SeatDto> CreateSeatAsync(SeatCreateDto createDto);
        Task<SeatDto?> UpdateSeatAsync(int id, SeatUpdateDto updateDto);
        Task<bool> DeleteSeatAsync(int id);

        // HallType
        Task<IEnumerable<HallTypeDto>> GetHallTypesAsync();
        Task<HallTypeDto?> GetHallTypeByIdAsync(int id);
        Task<HallTypeDto> CreateHallTypeAsync(HallTypeCreateDto createDto);
        Task<HallTypeDto?> UpdateHallTypeAsync(int id, HallTypeUpdateDto updateDto);
        Task<bool> DeleteHallTypeAsync(int id);

        // SeatType
        Task<IEnumerable<SeatTypeDto>> GetSeatTypesAsync();
        Task<SeatTypeDto?> GetSeatTypeByIdAsync(int id);
        Task<SeatTypeDto> CreateSeatTypeAsync(SeatTypeCreateDto createDto);
        Task<SeatTypeDto?> UpdateSeatTypeAsync(int id, SeatTypeUpdateDto updateDto);
        Task<bool> DeleteSeatTypeAsync(int id);
    }
}
