using System;
using System.Collections.Generic;

namespace CinemaBooking.API.DTOs.Bookings
{
    public class BookingDto
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = null!;
        public int UserId { get; set; }
        public string UserEmail { get; set; } = null!;
        public int ShowtimeId { get; set; }
        public string MovieTitle { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public string HallName { get; set; } = null!;
        public string CinemaName { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public string BookingStatus { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? QRCodeUrl { get; set; }
        public List<string> Seats { get; set; } = new();
    }

    public class BookingResponseDto : BookingDto
    {
        public string? PaymentUrl { get; set; }
    }

    public class BookingCreateDto
    {
        public int ShowtimeId { get; set; }
        public List<int> SeatIds { get; set; } = new();
        public string? PromoCode { get; set; }
        public string? SessionId { get; set; }
    }

    public class BookingConfirmDto
    {
        public int BookingId { get; set; }
        public string PaymentMethod { get; set; } = null!;
    }

    public class BookingCancelDto
    {
        public int BookingId { get; set; }
    }
}
