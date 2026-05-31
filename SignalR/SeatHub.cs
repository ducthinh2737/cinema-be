using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.SignalR
{
    public class SeatHubResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<int> SeatIds { get; set; } = new();
    }

    public class SeatHub : Hub
    {
        private readonly ISeatLockService _seatLockService;

        public SeatHub(ISeatLockService seatLockService)
        {
            _seatLockService = seatLockService;
        }

        public async Task JoinShowtime(int showtimeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Showtime_{showtimeId}");
        }

        public async Task LeaveShowtime(int showtimeId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Showtime_{showtimeId}");
        }

        public async Task<SeatHubResponse> SelectSeat(int showtimeId, int seatId, string userId, string sessionId)
        {
            var locked = await _seatLockService.LockSeatAsync(showtimeId, seatId, userId, sessionId, 5, Context.ConnectionAborted);
            if (locked != null && locked.Success)
            {
                return new SeatHubResponse 
                { 
                    Success = true, 
                    Message = "Seat locked successfully.", 
                    SeatIds = new List<int> { seatId } 
                };
            }
            else
            {
                return new SeatHubResponse 
                { 
                    Success = false, 
                    Message = locked?.Message ?? "Seat is already locked by another user.", 
                    SeatIds = new List<int> { seatId } 
                };
            }
        }

        public async Task<SeatHubResponse> ReleaseSeat(int showtimeId, int seatId, string userId, string sessionId)
        {
            var unlocked = await _seatLockService.UnlockSeatAsync(showtimeId, seatId, userId, sessionId, false, Context.ConnectionAborted);
            if (unlocked != null && unlocked.Success)
            {
                return new SeatHubResponse 
                { 
                    Success = true, 
                    Message = "Seat released successfully.", 
                    SeatIds = new List<int> { seatId } 
                };
            }
            else
            {
                return new SeatHubResponse 
                { 
                    Success = false, 
                    Message = unlocked?.Message ?? "Failed to release seat lock.", 
                    SeatIds = new List<int> { seatId } 
                };
            }
        }

        public async Task ConfirmBooking(int showtimeId, List<int> seatIds)
        {
            // Lock is finalized and permanently booked; notify other clients in showtime group
            await Clients.OthersInGroup($"Showtime_{showtimeId}").SendAsync("BookingConfirmed", showtimeId, seatIds);
        }

        public async Task<SeatHubResponse> RefreshSeatLock(int showtimeId, List<int> seatIds, string userId, string sessionId)
        {
            var refreshed = await _seatLockService.RefreshSeatLockAsync(showtimeId, seatIds, userId, sessionId, 5, Context.ConnectionAborted);
            if (refreshed != null && refreshed.Success)
            {
                return new SeatHubResponse 
                { 
                    Success = true, 
                    Message = "Seat locks refreshed successfully.", 
                    SeatIds = seatIds 
                };
            }
            else
            {
                return new SeatHubResponse 
                { 
                    Success = false, 
                    Message = refreshed?.Message ?? "Failed to refresh seat locks.", 
                    SeatIds = seatIds 
                };
            }
        }
    }
}
