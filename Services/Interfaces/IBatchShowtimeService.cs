using System.Threading;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Showtimes;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface IBatchShowtimeService
    {
        Task<ShowtimeBatchResponseDto> CreateShowtimeBatchAsync(ShowtimeBatchRequestDto batchDto, CancellationToken cancellationToken = default);
    }
}
