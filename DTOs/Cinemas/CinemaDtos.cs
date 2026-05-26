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
    }

    public class CinemaCreateDto
    {
        public string CinemaName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public int CityId { get; set; }
    }

    public class CinemaUpdateDto
    {
        public string CinemaName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public int CityId { get; set; }
    }

    public class CinemaQueryParameters
    {
        public string? Search { get; set; }
        public int? CityId { get; set; }
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
    }

    public class HallCreateDto
    {
        public int CinemaId { get; set; }
        public string HallName { get; set; } = null!;
        public int HallTypeId { get; set; }
    }

    public class HallUpdateDto
    {
        public string HallName { get; set; } = null!;
        public int HallTypeId { get; set; }
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
        public decimal PriceMultiplier { get; set; }
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

    // HallType DTOs
    public class HallTypeDto
    {
        public int HallTypeId { get; set; }
        public string TypeName { get; set; } = null!;
    }

    public class HallTypeCreateDto
    {
        public string TypeName { get; set; } = null!;
    }

    public class HallTypeUpdateDto
    {
        public string TypeName { get; set; } = null!;
    }

    // SeatType DTOs
    public class SeatTypeDto
    {
        public int SeatTypeId { get; set; }
        public string TypeName { get; set; } = null!;
        public decimal PriceMultiplier { get; set; }
    }

    public class SeatTypeCreateDto
    {
        public string TypeName { get; set; } = null!;
        public decimal PriceMultiplier { get; set; }
    }

    public class SeatTypeUpdateDto
    {
        public string TypeName { get; set; } = null!;
        public decimal PriceMultiplier { get; set; }
    }
}
