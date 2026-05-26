using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.SignalR
{
    public class SeatHub : Hub
    {
        private readonly ISeatLockService _seatLockService;

        public SeatHub(ISeatLockService seatLockService)
        {
            _seatLockService = seatLockService;
        }

        public async Task SelectSeat(int showtimeId, int seatId, string userId)
        {
            var locked = await _seatLockService.LockSeatAsync(showtimeId, seatId, userId);
            if (locked)
            {
                // Notify other clients that this seat is locked
                await Clients.Others.SendAsync("SeatSelected", showtimeId, seatId, userId);
            }
            else
            {
                // Notify caller that seat lock failed
                await Clients.Caller.SendAsync("SeatLockFailed", showtimeId, seatId, "Seat is already locked by another user.");
            }
        }

        public async Task ReleaseSeat(int showtimeId, int seatId, string userId)
        {
            var unlocked = await _seatLockService.UnlockSeatAsync(showtimeId, seatId, userId);
            if (unlocked)
            {
                // Notify other clients that this seat has been released
                await Clients.Others.SendAsync("SeatReleased", showtimeId, seatId);
            }
        }

        public async Task ConfirmBooking(int showtimeId, List<int> seatIds)
        {
            // Lock is finalized and permanently booked
            await Clients.Others.SendAsync("BookingConfirmed", showtimeId, seatIds);
        }
    }
}
