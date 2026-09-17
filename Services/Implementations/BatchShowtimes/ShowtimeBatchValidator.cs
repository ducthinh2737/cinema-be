using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.Models.Cinemas;
using CinemaBooking.API.Models.Showtimes;

namespace CinemaBooking.API.Services.Implementations.BatchShowtimes
{
    public class ShowtimeBatchValidator
    {
        private readonly CinemaDbContext _context;

        public ShowtimeBatchValidator(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<(Movie movie, Hall hall, Price price, string? errorMessage, int queryCount)> ValidatePrerequisitesAsync(
            ShowtimeBatchRequestDto batchDto, 
            CancellationToken cancellationToken)
        {
            int queryCount = 0;

            if (batchDto.StartDate.Date < DateTime.UtcNow.Date.AddDays(-30))
            {
                return (null!, null!, null!, "Ngày bắt đầu không được quá xa trong quá khứ.", queryCount);
            }

            if (batchDto.StartDate > batchDto.EndDate)
            {
                return (null!, null!, null!, "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.", queryCount);
            }

            if (batchDto.Mode != "Auto" && batchDto.Mode != "Manual")
            {
                return (null!, null!, null!, "Chế độ (Mode) chỉ có thể là 'Auto' hoặc 'Manual'.", queryCount);
            }

            var movie = await _context.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == batchDto.MovieId && !m.IsDeleted, cancellationToken);
            queryCount++;
            if (movie == null)
            {
                return (null!, null!, null!, $"Phim ID {batchDto.MovieId} không tồn tại hoặc đã bị xóa.", queryCount);
            }

            var hall = await _context.Halls.Include(h => h.Cinema).AsNoTracking().FirstOrDefaultAsync(h => h.HallId == batchDto.HallId, cancellationToken);
            queryCount++;
            if (hall == null || hall.IsDeleted)
            {
                return (null!, null!, null!, $"Phòng chiếu ID {batchDto.HallId} không tồn tại hoặc đã bị ẩn.", queryCount);
            }

            if (hall.Cinema == null || hall.Cinema.IsDeleted)
            {
                return (null!, null!, null!, "Rạp chiếu của phòng chiếu này hiện tại đang bị dừng hoạt động.", queryCount);
            }

            var price = await _context.Prices.AsNoTracking().FirstOrDefaultAsync(p => p.PriceId == batchDto.PriceId, cancellationToken);
            queryCount++;
            if (price == null)
            {
                return (null!, null!, null!, $"Mức giá ID {batchDto.PriceId} không tồn tại.", queryCount);
            }

            var earliestAllowedDate = movie.ReleaseDate.Date.AddDays(-3);
            if (batchDto.StartDate.Date < earliestAllowedDate)
            {
                return (null!, null!, null!, $"Không thể tạo suất chiếu trước ngày chiếu sớm cho phép. Phim khởi chiếu ngày {movie.ReleaseDate:dd/MM/yyyy}. Chiếu sớm được bắt đầu từ {earliestAllowedDate:dd/MM/yyyy}.", queryCount);
            }

            if (movie.EndDate != default && movie.EndDate > movie.ReleaseDate)
            {
                if (batchDto.EndDate.Date > movie.EndDate.Date)
                {
                    return (null!, null!, null!, $"Không thể tạo suất chiếu sau khi phim đã ngừng chiếu. Phim kết thúc ngày {movie.EndDate:dd/MM/yyyy}.", queryCount);
                }
            }

            return (movie, hall, price, null, queryCount);
        }
    }
}
