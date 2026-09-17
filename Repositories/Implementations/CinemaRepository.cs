using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Cinemas;
using CinemaBooking.API.DTOs.Cinemas;
using CinemaBooking.API.Repositories.Interfaces;

namespace CinemaBooking.API.Repositories.Implementations
{
    public class CinemaRepository : ICinemaRepository
    {
        private readonly CinemaDbContext _context;

        public CinemaRepository(CinemaDbContext context)
        {
            _context = context;
        }

        // Cinema
        public async Task<(IEnumerable<Cinema> Cinemas, int TotalCount)> GetPagedCinemasAsync(CinemaQueryParameters queryParams)
        {
            var query = _context.Cinemas.Include(c => c.City).Include(c => c.Halls).ThenInclude(h => h.Seats).AsQueryable();

            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                query = query.Where(c => c.CinemaName.Contains(queryParams.Search) || c.Address.Contains(queryParams.Search));
            }

            if (queryParams.CityId.HasValue)
            {
                query = query.Where(c => c.CityId == queryParams.CityId.Value);
            }

            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                query = query.Where(c => c.Status == queryParams.Status);
            }

            // Apply sorting
            IOrderedQueryable<Cinema> orderedQuery;
            if (!string.IsNullOrEmpty(queryParams.SortBy))
            {
                switch (queryParams.SortBy.ToLowerInvariant())
                {
                    case "newest":
                        orderedQuery = query.OrderByDescending(c => c.CreatedAt);
                        break;
                    case "oldest":
                        orderedQuery = query.OrderBy(c => c.CreatedAt);
                        break;
                    case "mosthalls":
                        orderedQuery = query.OrderByDescending(c => c.Halls.Count);
                        break;
                    case "mostseats":
                        orderedQuery = query.OrderByDescending(c => c.Halls.SelectMany(h => h.Seats).Count());
                        break;
                    default:
                        orderedQuery = query.OrderByDescending(c => c.CinemaId);
                        break;
                }
            }
            else
            {
                orderedQuery = query.OrderByDescending(c => c.CinemaId);
            }

            int totalCount = await query.CountAsync();
            var cinemas = await orderedQuery
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return (cinemas, totalCount);
        }

        public async Task<Cinema?> GetCinemaByIdAsync(int id)
        {
            return await _context.Cinemas
                .Include(c => c.City)
                .FirstOrDefaultAsync(c => c.CinemaId == id);
        }

        public async Task AddCinemaAsync(Cinema cinema)
        {
            cinema.CreatedAt = DateTime.UtcNow;
            await _context.Cinemas.AddAsync(cinema);
        }

        public Task UpdateCinemaAsync(Cinema cinema)
        {
            cinema.LastModifiedAt = DateTime.UtcNow;
            _context.Cinemas.Update(cinema);
            return Task.CompletedTask;
        }

        public async Task DeleteCinemaAsync(Cinema cinema)
        {
            var hallsExist = await _context.Halls
                .AnyAsync(h => h.CinemaId == cinema.CinemaId && !h.IsDeleted);
            if (hallsExist)
            {
                throw new InvalidOperationException("Không thể xóa rạp chiếu vì còn các phòng chiếu hoạt động.");
            }

            var showtimesExist = await _context.Showtimes
                .AnyAsync(s => s.Hall.CinemaId == cinema.CinemaId);
            if (showtimesExist)
            {
                throw new InvalidOperationException("Không thể xóa rạp chiếu vì đang có lịch chiếu phim.");
            }

            var ticketsSoldExist = await _context.Bookings
                .AnyAsync(b => b.Showtime.Hall.CinemaId == cinema.CinemaId && b.BookingStatus != "Cancelled");
            if (ticketsSoldExist)
            {
                throw new InvalidOperationException("Không thể xóa rạp chiếu vì đã bán vé.");
            }

            cinema.IsDeleted = true;
            cinema.DeletedAt = DateTime.UtcNow;
            _context.Cinemas.Update(cinema);
        }

        // Hall
        public async Task<IEnumerable<Hall>> GetHallsByCinemaIdAsync(int cinemaId)
        {
            return await _context.Halls
                .Include(h => h.Cinema)
                .Include(h => h.HallType)
                .Where(h => h.CinemaId == cinemaId)
                .ToListAsync();
        }

        public async Task<Hall?> GetHallByIdAsync(int id)
        {
            return await _context.Halls
                .Include(h => h.Cinema)
                .Include(h => h.HallType)
                .FirstOrDefaultAsync(h => h.HallId == id);
        }

        public async Task AddHallAsync(Hall hall)
        {
            hall.CreatedAt = DateTime.UtcNow;
            await _context.Halls.AddAsync(hall);
        }

        public Task UpdateHallAsync(Hall hall)
        {
            hall.LastModifiedAt = DateTime.UtcNow;
            _context.Halls.Update(hall);
            return Task.CompletedTask;
        }

        public async Task DeleteHallAsync(Hall hall)
        {
            var showtimesExist = await _context.Showtimes
                .AnyAsync(s => s.HallId == hall.HallId);
            if (showtimesExist)
            {
                throw new InvalidOperationException("Không thể xóa phòng chiếu vì đang có lịch chiếu hoạt động.");
            }

            var ticketsSoldExist = await _context.Bookings
                .AnyAsync(b => b.Showtime.HallId == hall.HallId && b.BookingStatus != "Cancelled");
            if (ticketsSoldExist)
            {
                throw new InvalidOperationException("Không thể xóa phòng chiếu vì đã bán vé.");
            }

            hall.IsDeleted = true;
            hall.DeletedAt = DateTime.UtcNow;
            _context.Halls.Update(hall);
        }

        // Seat
        public async Task<IEnumerable<Seat>> GetSeatsByHallIdAsync(int hallId)
        {
            return await _context.Seats
                .Include(s => s.Hall)
                .Include(s => s.SeatType)
                .Where(s => s.HallId == hallId)
                .ToListAsync();
        }

        public async Task<Seat?> GetSeatByIdAsync(int id)
        {
            return await _context.Seats
                .Include(s => s.Hall)
                .Include(s => s.SeatType)
                .FirstOrDefaultAsync(s => s.SeatId == id);
        }

        public async Task AddSeatAsync(Seat seat)
        {
            seat.CreatedAt = DateTime.UtcNow;
            await _context.Seats.AddAsync(seat);
        }

        public Task UpdateSeatAsync(Seat seat)
        {
            seat.LastModifiedAt = DateTime.UtcNow;
            _context.Seats.Update(seat);
            return Task.CompletedTask;
        }

        public Task DeleteSeatAsync(Seat seat)
        {
            seat.IsDeleted = true;
            seat.DeletedAt = DateTime.UtcNow;
            _context.Seats.Update(seat);
            return Task.CompletedTask;
        }

        // HallType
        public async Task<IEnumerable<HallType>> GetHallTypesAsync()
        {
            return await _context.HallTypes.ToListAsync();
        }

        public async Task<HallType?> GetHallTypeByIdAsync(int id)
        {
            return await _context.HallTypes.FirstOrDefaultAsync(ht => ht.HallTypeId == id);
        }

        public async Task AddHallTypeAsync(HallType hallType)
        {
            hallType.CreatedAt = DateTime.UtcNow;
            await _context.HallTypes.AddAsync(hallType);
        }

        public Task UpdateHallTypeAsync(HallType hallType)
        {
            hallType.LastModifiedAt = DateTime.UtcNow;
            _context.HallTypes.Update(hallType);
            return Task.CompletedTask;
        }

        public async Task DeleteHallTypeAsync(HallType hallType)
        {
            var hallsExist = await _context.Halls
                .AnyAsync(h => h.HallTypeId == hallType.HallTypeId && !h.IsDeleted);
            if (hallsExist)
            {
                throw new InvalidOperationException("Không thể xóa loại phòng chiếu vì vẫn còn phòng chiếu đang sử dụng loại này.");
            }

            hallType.IsDeleted = true;
            hallType.DeletedAt = DateTime.UtcNow;
            _context.HallTypes.Update(hallType);
        }

        // SeatType
        public async Task<IEnumerable<SeatType>> GetSeatTypesAsync()
        {
            return await _context.SeatTypes.ToListAsync();
        }

        public async Task<SeatType?> GetSeatTypeByIdAsync(int id)
        {
            return await _context.SeatTypes.FirstOrDefaultAsync(st => st.SeatTypeId == id);
        }

        public async Task AddSeatTypeAsync(SeatType seatType)
        {
            seatType.CreatedAt = DateTime.UtcNow;
            await _context.SeatTypes.AddAsync(seatType);
        }

        public Task UpdateSeatTypeAsync(SeatType seatType)
        {
            seatType.LastModifiedAt = DateTime.UtcNow;
            _context.SeatTypes.Update(seatType);
            return Task.CompletedTask;
        }

        public async Task DeleteSeatTypeAsync(SeatType seatType)
        {
            var seatsExist = await _context.Seats
                .AnyAsync(s => s.SeatTypeId == seatType.SeatTypeId && !s.IsDeleted);
            if (seatsExist)
            {
                throw new InvalidOperationException("Không thể xóa loại ghế vì vẫn còn ghế đang sử dụng loại này.");
            }

            seatType.IsDeleted = true;
            seatType.DeletedAt = DateTime.UtcNow;
            _context.SeatTypes.Update(seatType);
        }

        // Save
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
