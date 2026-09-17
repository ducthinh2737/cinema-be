using System;
using System.Collections.Generic;
using CinemaBooking.API.DTOs.Combos;

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
        public int? PointsRedeemed { get; set; }
        public decimal? PointsDiscountAmount { get; set; }
        public string BookingStatus { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? QRCodeUrl { get; set; }
        public List<string> Seats { get; set; } = new();
        public string? MoviePosterUrl { get; set; }
        public string? MovieBannerUrl { get; set; }
        public int MovieDuration { get; set; }
        public List<OrderComboDto> Combos { get; set; } = new();
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
        public int? PointsToRedeem { get; set; }
        public List<OrderComboInputDto> Combos { get; set; } = new();
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

    public class BookingApplyDiscountDto
    {
        public string? PromoCode { get; set; }
        public int? PointsToRedeem { get; set; }
    }
}
