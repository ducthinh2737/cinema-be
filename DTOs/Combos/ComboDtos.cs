using System;
using System.Collections.Generic;

namespace CinemaBooking.API.DTOs.Combos
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProductCreateUpdateDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ComboItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int Quantity { get; set; }
    }

    public class ComboItemInputDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class ComboDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string? DiscountBadge { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public List<ComboItemDto> ComboItems { get; set; } = new();
    }

    public class ComboCreateUpdateDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string? DiscountBadge { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
        public List<ComboItemInputDto> ComboItems { get; set; } = new();
    }

    public class OrderComboDto
    {
        public int ComboId { get; set; }
        public string ComboName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal SubTotal => Price * Quantity;
    }

    public class OrderComboInputDto
    {
        public int ComboId { get; set; }
        public int Quantity { get; set; }
    }

    public class ComboQueryParameters
    {
        private const int MaxPageSize = 50;
        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; }
    }

    public class ComboRecommendationDto
    {
        public ComboDto Combo { get; set; } = null!;
        public string Reason { get; set; } = null!;
    }
}
