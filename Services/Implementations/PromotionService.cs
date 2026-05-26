using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CinemaBooking.API.Models.Promotions;
using CinemaBooking.API.Models.Bookings;
using CinemaBooking.API.DTOs.Promotions;
using CinemaBooking.API.DTOs.Common;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Services.Implementations
{
    public class PromotionService : IPromotionService
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IMapper _mapper;

        public PromotionService(IPromotionRepository promotionRepository, IMapper mapper)
        {
            _promotionRepository = promotionRepository;
            _mapper = mapper;
        }

        public async Task<PagedResultDto<PromotionDto>> GetPagedPromotionsAsync(PromotionQueryParameters queryParams)
        {
            var (promotions, totalCount) = await _promotionRepository.GetPagedPromotionsAsync(queryParams);
            var dtos = _mapper.Map<List<PromotionDto>>(promotions);

            return new PagedResultDto<PromotionDto>
            {
                Items = dtos,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PromotionDto?> GetPromotionByIdAsync(int id)
        {
            var promotion = await _promotionRepository.GetPromotionByIdAsync(id);
            return _mapper.Map<PromotionDto>(promotion);
        }

        public async Task<PromotionDto> CreatePromotionAsync(PromotionCreateDto createDto)
        {
            var promotion = _mapper.Map<Promotion>(createDto);
            promotion.CurrentUsage = 0;
            promotion.IsActive = true;

            await _promotionRepository.AddPromotionAsync(promotion);
            await _promotionRepository.SaveChangesAsync();

            return _mapper.Map<PromotionDto>(promotion);
        }

        public async Task<PromotionDto?> UpdatePromotionAsync(int id, PromotionUpdateDto updateDto)
        {
            var promotion = await _promotionRepository.GetPromotionByIdAsync(id);
            if (promotion == null) return null;

            _mapper.Map(updateDto, promotion);
            await _promotionRepository.UpdatePromotionAsync(promotion);
            await _promotionRepository.SaveChangesAsync();

            return _mapper.Map<PromotionDto>(promotion);
        }

        public async Task<bool> DeletePromotionAsync(int id)
        {
            var promotion = await _promotionRepository.GetPromotionByIdAsync(id);
            if (promotion == null) return false;

            await _promotionRepository.DeletePromotionAsync(promotion);
            return await _promotionRepository.SaveChangesAsync();
        }

        public async Task<PromotionValidateResultDto> ValidatePromotionAsync(PromotionValidateDto validateDto)
        {
            var promotion = await _promotionRepository.GetPromotionByCodeAsync(validateDto.PromoCode);
            if (promotion == null)
            {
                return new PromotionValidateResultDto
                {
                    IsValid = false,
                    Message = "Promotion code does not exist.",
                    DiscountAmount = 0,
                    FinalAmount = validateDto.OrderAmount
                };
            }

            if (!promotion.IsActive)
            {
                return new PromotionValidateResultDto
                {
                    IsValid = false,
                    Message = "This promotion is currently inactive.",
                    DiscountAmount = 0,
                    FinalAmount = validateDto.OrderAmount
                };
            }

            var now = DateTime.UtcNow;
            if (now < promotion.StartDate)
            {
                return new PromotionValidateResultDto
                {
                    IsValid = false,
                    Message = $"This promotion starts on {promotion.StartDate.ToLocalTime()}.",
                    DiscountAmount = 0,
                    FinalAmount = validateDto.OrderAmount
                };
            }

            if (now > promotion.EndDate)
            {
                return new PromotionValidateResultDto
                {
                    IsValid = false,
                    Message = "This promotion has expired.",
                    DiscountAmount = 0,
                    FinalAmount = validateDto.OrderAmount
                };
            }

            if (promotion.CurrentUsage >= promotion.MaxUsage)
            {
                return new PromotionValidateResultDto
                {
                    IsValid = false,
                    Message = "Usage limit for this promotion has been reached.",
                    DiscountAmount = 0,
                    FinalAmount = validateDto.OrderAmount
                };
            }

            // Check if user already used this voucher
            var userPromo = await _promotionRepository.GetUserPromotionAsync(validateDto.UserId, promotion.PromotionId);
            if (userPromo != null && userPromo.IsUsed)
            {
                return new PromotionValidateResultDto
                {
                    IsValid = false,
                    Message = "You have already used this voucher.",
                    DiscountAmount = 0,
                    FinalAmount = validateDto.OrderAmount
                };
            }

            // Calculate discount
            decimal discountAmount = 0;
            if (promotion.DiscountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase))
            {
                discountAmount = validateDto.OrderAmount * (promotion.DiscountValue / 100);
            }
            else if (promotion.DiscountType.Equals("FixedAmount", StringComparison.OrdinalIgnoreCase))
            {
                discountAmount = promotion.DiscountValue;
            }

            if (discountAmount > validateDto.OrderAmount)
            {
                discountAmount = validateDto.OrderAmount;
            }

            return new PromotionValidateResultDto
            {
                IsValid = true,
                Message = "Voucher successfully validated.",
                DiscountAmount = discountAmount,
                FinalAmount = validateDto.OrderAmount - discountAmount
            };
        }

        public async Task<PromotionApplyResultDto> ApplyPromotionAsync(PromotionApplyDto applyDto)
        {
            var booking = await _promotionRepository.GetBookingByIdAsync(applyDto.BookingId);
            if (booking == null)
            {
                return new PromotionApplyResultDto
                {
                    IsSuccess = false,
                    Message = "Booking not found.",
                    DiscountAmount = 0,
                    NewTotalAmount = 0
                };
            }

            var promotion = await _promotionRepository.GetPromotionByCodeAsync(applyDto.PromoCode);
            if (promotion == null)
            {
                return new PromotionApplyResultDto
                {
                    IsSuccess = false,
                    Message = "Promotion code does not exist.",
                    DiscountAmount = booking.DiscountAmount,
                    NewTotalAmount = booking.TotalAmount
                };
            }

            // Validate voucher against this user and booking amount
            var baseAmount = booking.TotalAmount + booking.DiscountAmount;
            var validateDto = new PromotionValidateDto
            {
                PromoCode = applyDto.PromoCode,
                UserId = booking.UserId,
                OrderAmount = baseAmount
            };

            var validateResult = await ValidatePromotionAsync(validateDto);
            if (!validateResult.IsValid)
            {
                return new PromotionApplyResultDto
                {
                    IsSuccess = false,
                    Message = validateResult.Message,
                    DiscountAmount = booking.DiscountAmount,
                    NewTotalAmount = booking.TotalAmount
                };
            }

            // Apply discounts
            booking.DiscountAmount = validateResult.DiscountAmount;
            booking.TotalAmount = baseAmount - validateResult.DiscountAmount;
            await _promotionRepository.UpdateBookingAsync(booking);

            // Increment usage
            promotion.CurrentUsage++;
            await _promotionRepository.UpdatePromotionAsync(promotion);

            // Mark User Promotion usage
            var userPromo = await _promotionRepository.GetUserPromotionAsync(booking.UserId, promotion.PromotionId);
            if (userPromo == null)
            {
                userPromo = new UserPromotion
                {
                    UserId = booking.UserId,
                    PromotionId = promotion.PromotionId,
                    IsUsed = true,
                    UsedAt = DateTime.UtcNow
                };
                await _promotionRepository.AddUserPromotionAsync(userPromo);
            }
            else
            {
                userPromo.IsUsed = true;
                userPromo.UsedAt = DateTime.UtcNow;
                await _promotionRepository.UpdateUserPromotionAsync(userPromo);
            }

            // Log mapping
            var bookingPromotion = new BookingPromotion
            {
                BookingId = booking.BookingId,
                PromotionId = promotion.PromotionId
            };
            await _promotionRepository.AddBookingPromotionAsync(bookingPromotion);

            await _promotionRepository.SaveChangesAsync();

            return new PromotionApplyResultDto
            {
                IsSuccess = true,
                Message = "Voucher successfully applied to booking.",
                DiscountAmount = booking.DiscountAmount,
                NewTotalAmount = booking.TotalAmount
            };
        }
    }
}
