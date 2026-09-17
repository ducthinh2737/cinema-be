using System.Collections.Generic;

namespace CinemaBooking.API.DTOs.Cinemas
{
    // Cinema DTOs
    public class CinemaDto
    {
        public int CinemaId { get; set; }
        public string CinemaName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public int CityId { get; set; }
        public string? CityName { get; set; }
        public string? ImageUrl { get; set; }
        public string Status { get; set; } = "Active";
        public string? OpeningTime { get; set; }
        public string? ClosingTime { get; set; }
        public string? GoogleMapsUrl { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? GalleryUrls { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int HallCount { get; set; }
        public int SeatCount { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }

    public class CinemaCreateDto
    {
        public string CinemaName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public int CityId { get; set; }
        public string Status { get; set; } = "Active";
        public string? OpeningTime { get; set; }
        public string? ClosingTime { get; set; }
        public string? GoogleMapsUrl { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? GalleryUrls { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class CinemaUpdateDto
    {
        public string CinemaName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public int CityId { get; set; }
        public string Status { get; set; } = "Active";
        public string? OpeningTime { get; set; }
        public string? ClosingTime { get; set; }
        public string? GoogleMapsUrl { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? GalleryUrls { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class CinemaQueryParameters
    {
        public string? Search { get; set; }
        public int? CityId { get; set; }
        public string? Status { get; set; }
        public string? SortBy { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // Hall DTOs
    public class HallDto
    {
        public int HallId { get; set; }
        public int CinemaId { get; set; }
        public string? CinemaName { get; set; }
        public string HallName { get; set; } = null!;
        public int HallTypeId { get; set; }
        public string? HallTypeName { get; set; }
        public int Capacity { get; set; }
        public string? Description { get; set; }
    }

    public class HallCreateDto
    {
        public int CinemaId { get; set; }
        public string HallName { get; set; } = null!;
        public int HallTypeId { get; set; }
        public int Capacity { get; set; }
        public string? Description { get; set; }
    }

    public class HallUpdateDto
    {
        public string HallName { get; set; } = null!;
        public int HallTypeId { get; set; }
        public int Capacity { get; set; }
        public string? Description { get; set; }
    }

    // Seat DTOs
    public class SeatDto
    {
        public int SeatId { get; set; }
        public int HallId { get; set; }
        public string? HallName { get; set; }
        public string SeatCode { get; set; } = null!;
        public int SeatTypeId { get; set; }
        public string? SeatTypeName { get; set; }
        public string RowName { get; set; } = null!;
        public int SeatNumber { get; set; }
    }

    public class SeatCreateDto
    {
        public int HallId { get; set; }
        public string SeatCode { get; set; } = null!;
        public int SeatTypeId { get; set; }
    }

    public class SeatUpdateDto
    {
        public string SeatCode { get; set; } = null!;
        public int SeatTypeId { get; set; }
    }

    public class HallTypeDto
    {
        public int HallTypeId { get; set; }
        public string TypeName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class HallTypeCreateDto
    {
        public string TypeName { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class HallTypeUpdateDto
    {
        public string TypeName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
    }

    // SeatType DTOs
    public class SeatTypeDto
    {
        public int SeatTypeId { get; set; }
        public string TypeName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SeatTypeCreateDto
    {
        public string TypeName { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class SeatTypeUpdateDto
    {
        public string TypeName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
    }
}

