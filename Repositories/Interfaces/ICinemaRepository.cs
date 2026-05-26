using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.Models.Cinemas;
using CinemaBooking.API.DTOs.Cinemas;

namespace CinemaBooking.API.Repositories.Interfaces
{
    public interface ICinemaRepository
    {
        // Cinema
        Task<(IEnumerable<Cinema> Cinemas, int TotalCount)> GetPagedCinemasAsync(CinemaQueryParameters queryParams);
        Task<Cinema?> GetCinemaByIdAsync(int id);
        Task AddCinemaAsync(Cinema cinema);
        Task UpdateCinemaAsync(Cinema cinema);
        Task DeleteCinemaAsync(Cinema cinema);

        // Hall
        Task<IEnumerable<Hall>> GetHallsByCinemaIdAsync(int cinemaId);
        Task<Hall?> GetHallByIdAsync(int id);
        Task AddHallAsync(Hall hall);
        Task UpdateHallAsync(Hall hall);
        Task DeleteHallAsync(Hall hall);

        // Seat
        Task<IEnumerable<Seat>> GetSeatsByHallIdAsync(int hallId);
        Task<Seat?> GetSeatByIdAsync(int id);
        Task AddSeatAsync(Seat seat);
        Task UpdateSeatAsync(Seat seat);
        Task DeleteSeatAsync(Seat seat);

        // HallType
        Task<IEnumerable<HallType>> GetHallTypesAsync();
        Task<HallType?> GetHallTypeByIdAsync(int id);
        Task AddHallTypeAsync(HallType hallType);
        Task UpdateHallTypeAsync(HallType hallType);
        Task DeleteHallTypeAsync(HallType hallType);

        // SeatType
        Task<IEnumerable<SeatType>> GetSeatTypesAsync();
        Task<SeatType?> GetSeatTypeByIdAsync(int id);
        Task AddSeatTypeAsync(SeatType seatType);
        Task UpdateSeatTypeAsync(SeatType seatType);
        Task DeleteSeatTypeAsync(SeatType seatType);

        // Save Changes
        Task<bool> SaveChangesAsync();
    }
}
