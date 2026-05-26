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
            var query = _context.Cinemas.Include(c => c.City).AsQueryable();

            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                query = query.Where(c => c.CinemaName.Contains(queryParams.Search) || c.Address.Contains(queryParams.Search));
            }

            if (queryParams.CityId.HasValue)
            {
                query = query.Where(c => c.CityId == queryParams.CityId.Value);
            }

            int totalCount = await query.CountAsync();
            var cinemas = await query
                .OrderBy(c => c.CinemaId)
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

        public Task DeleteCinemaAsync(Cinema cinema)
        {
            cinema.IsDeleted = true;
            cinema.DeletedAt = DateTime.UtcNow;
            _context.Cinemas.Update(cinema);
            return Task.CompletedTask;
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

        public Task DeleteHallAsync(Hall hall)
        {
            hall.IsDeleted = true;
            hall.DeletedAt = DateTime.UtcNow;
            _context.Halls.Update(hall);
            return Task.CompletedTask;
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

        public Task DeleteHallTypeAsync(HallType hallType)
        {
            hallType.IsDeleted = true;
            hallType.DeletedAt = DateTime.UtcNow;
            _context.HallTypes.Update(hallType);
            return Task.CompletedTask;
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

        public Task DeleteSeatTypeAsync(SeatType seatType)
        {
            seatType.IsDeleted = true;
            seatType.DeletedAt = DateTime.UtcNow;
            _context.SeatTypes.Update(seatType);
            return Task.CompletedTask;
        }

        // Save
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
