using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CinemaBooking.API.Models.Promotions;
using CinemaBooking.API.Models.Bookings;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.Models.Users;
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
            promotion.CreatedAt = DateTime.UtcNow;

            await _promotionRepository.AddPromotionAsync(promotion);
            await _promotionRepository.SaveChangesAsync();

            return _mapper.Map<PromotionDto>(promotion);
        }

        public async Task<PromotionDto?> UpdatePromotionAsync(int id, PromotionUpdateDto updateDto)
        {
            var promotion = await _promotionRepository.GetPromotionByIdAsync(id);
            if (promotion == null) return null;

            // Update main properties
            _mapper.Map(updateDto, promotion);

            // Update conditions (we clear existing ones and add new ones to avoid complex tracking)
            promotion.PromotionConditions.Clear();
            foreach (var condDto in updateDto.PromotionConditions)
            {
                var cond = _mapper.Map<PromotionCondition>(condDto);
                promotion.PromotionConditions.Add(cond);
            }

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

        public async Task<PromotionDto?> TogglePromotionStatusAsync(int id)
        {
            var promotion = await _promotionRepository.GetPromotionByIdAsync(id);
            if (promotion == null) return null;

            promotion.IsActive = !promotion.IsActive;
            await _promotionRepository.UpdatePromotionAsync(promotion);
            await _promotionRepository.SaveChangesAsync();

            return _mapper.Map<PromotionDto>(promotion);
        }

        public async Task<PromotionValidateResultDto> ValidatePromotionAsync(PromotionValidateDto validateDto)
        {
            var promotion = await _promotionRepository.GetPromotionByCodeAsync(validateDto.PromoCode);
            if (promotion == null)
            {
                return new PromotionValidateResultDto
                {
                    IsValid = false,
                    Message = "Mã khuyến mãi không tồn tại.",
                    DiscountAmount = 0,
                    FinalAmount = validateDto.OrderAmount
                };
            }

            if (!promotion.IsActive)
            {
                return new PromotionValidateResultDto
                {
                    IsValid = false,
                    Message = "Chương trình khuyến mãi này đã tạm ngưng hoạt động.",
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
                    Message = $"Chương trình chưa bắt đầu. Thời gian áp dụng từ: {promotion.StartDate.ToLocalTime():dd/MM/yyyy HH:mm}.",
                    DiscountAmount = 0,
                    FinalAmount = validateDto.OrderAmount
                };
            }

            if (now > promotion.EndDate)
            {
                return new PromotionValidateResultDto
                {
                    IsValid = false,
                    Message = "Mã khuyến mãi này đã hết hạn sử dụng.",
                    DiscountAmount = 0,
                    FinalAmount = validateDto.OrderAmount
                };
            }

            if (promotion.CurrentUsage >= promotion.MaxUsage)
            {
                return new PromotionValidateResultDto
                {
                    IsValid = false,
                    Message = "Chương trình khuyến mãi đã đạt giới hạn lượt sử dụng.",
                    DiscountAmount = 0,
                    FinalAmount = validateDto.OrderAmount
                };
            }

            if (promotion.MinimumOrderValue.HasValue && validateDto.OrderAmount < promotion.MinimumOrderValue.Value)
            {
                return new PromotionValidateResultDto
                {
                    IsValid = false,
                    Message = $"Giá trị đơn hàng chưa đạt tối thiểu {promotion.MinimumOrderValue.Value:N0}đ để áp dụng mã.",
                    DiscountAmount = 0,
                    FinalAmount = validateDto.OrderAmount
                };
            }

            // Check if user already used this voucher
            if (validateDto.UserId.HasValue && validateDto.UserId.Value > 0)
            {
                var userPromo = await _promotionRepository.GetUserPromotionAsync(validateDto.UserId.Value, promotion.PromotionId);
                if (userPromo != null && userPromo.IsUsed)
                {
                    return new PromotionValidateResultDto
                    {
                        IsValid = false,
                        Message = "Bạn đã sử dụng mã khuyến mãi này cho một giao dịch khác trước đó.",
                        DiscountAmount = 0,
                        FinalAmount = validateDto.OrderAmount
                    };
                }
            }

            // Validate custom PromotionConditions
            if (promotion.PromotionConditions != null && promotion.PromotionConditions.Any())
            {
                Showtime? showtime = null;
                if (validateDto.ShowtimeId.HasValue && validateDto.ShowtimeId.Value > 0)
                {
                    showtime = await _promotionRepository.GetShowtimeByIdAsync(validateDto.ShowtimeId.Value);
                }

                foreach (var condition in promotion.PromotionConditions)
                {
                    switch (condition.ApplyType)
                    {
                        case PromotionApplyType.Movie:
                            if (condition.MovieId.HasValue)
                            {
                                int currentMovieId = validateDto.MovieId ?? showtime?.MovieId ?? 0;
                                if (currentMovieId != condition.MovieId.Value)
                                {
                                    return new PromotionValidateResultDto
                                    {
                                        IsValid = false,
                                        Message = $"Khuyến mãi này chỉ áp dụng cho phim cụ thể.",
                                        DiscountAmount = 0,
                                        FinalAmount = validateDto.OrderAmount
                                    };
                                }
                            }
                            break;

                        case PromotionApplyType.Showtime:
                            if (condition.ShowtimeId.HasValue && validateDto.ShowtimeId.HasValue)
                            {
                                if (validateDto.ShowtimeId.Value != condition.ShowtimeId.Value)
                                {
                                    return new PromotionValidateResultDto
                                    {
                                        IsValid = false,
                                        Message = "Khuyến mãi này chỉ áp dụng cho suất chiếu được chỉ định.",
                                        DiscountAmount = 0,
                                        FinalAmount = validateDto.OrderAmount
                                    };
                                }
                            }
                            break;

                        case PromotionApplyType.MemberLevel:
                            if (condition.MemberLevelId.HasValue)
                            {
                                int userMemberTierId = 0;
                                if (validateDto.MemberLevelId.HasValue)
                                {
                                    userMemberTierId = validateDto.MemberLevelId.Value;
                                }
                                else if (validateDto.UserId.HasValue)
                                {
                                    var user = await _promotionRepository.GetUserByIdWithTierAsync(validateDto.UserId.Value);
                                    userMemberTierId = user?.MemberTierId ?? 0;
                                }

                                if (userMemberTierId != condition.MemberLevelId.Value)
                                {
                                    return new PromotionValidateResultDto
                                    {
                                        IsValid = false,
                                        Message = "Hạng thành viên của bạn chưa đủ điều kiện áp dụng khuyến mãi này.",
                                        DiscountAmount = 0,
                                        FinalAmount = validateDto.OrderAmount
                                    };
                                }
                            }
                            break;

                        case PromotionApplyType.GoldenHour:
                            // Check Hour and DaysOfWeek
                            var localNow = DateTime.Now;
                            var timeToCheck = showtime?.StartTime.TimeOfDay ?? localNow.TimeOfDay;
                            
                            if (condition.StartHour.HasValue && condition.EndHour.HasValue)
                            {
                                if (timeToCheck < condition.StartHour.Value || timeToCheck > condition.EndHour.Value)
                                {
                                    return new PromotionValidateResultDto
                                    {
                                        IsValid = false,
                                        Message = $"Khuyến mãi này chỉ áp dụng cho khung giờ vàng từ {condition.StartHour:hh\\:mm} đến {condition.EndHour:hh\\:mm}.",
                                        DiscountAmount = 0,
                                        FinalAmount = validateDto.OrderAmount
                                    };
                                }
                            }

                            if (!string.IsNullOrEmpty(condition.DaysOfWeek))
                            {
                                var dayOfWeekToCheck = showtime?.StartTime.DayOfWeek ?? localNow.DayOfWeek;
                                var todayDayStr = dayOfWeekToCheck.ToString();
                                var daysList = condition.DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(d => d.Trim());
                                
                                bool dayMatches = false;
                                foreach (var day in daysList)
                                {
                                    if (day.Equals(todayDayStr, StringComparison.OrdinalIgnoreCase) ||
                                        (day.Equals("Monday", StringComparison.OrdinalIgnoreCase) && dayOfWeekToCheck == DayOfWeek.Monday) ||
                                        (day.Equals("Tuesday", StringComparison.OrdinalIgnoreCase) && dayOfWeekToCheck == DayOfWeek.Tuesday) ||
                                        (day.Equals("Wednesday", StringComparison.OrdinalIgnoreCase) && dayOfWeekToCheck == DayOfWeek.Wednesday) ||
                                        (day.Equals("Thursday", StringComparison.OrdinalIgnoreCase) && dayOfWeekToCheck == DayOfWeek.Thursday) ||
                                        (day.Equals("Friday", StringComparison.OrdinalIgnoreCase) && dayOfWeekToCheck == DayOfWeek.Friday) ||
                                        (day.Equals("Saturday", StringComparison.OrdinalIgnoreCase) && dayOfWeekToCheck == DayOfWeek.Saturday) ||
                                        (day.Equals("Sunday", StringComparison.OrdinalIgnoreCase) && dayOfWeekToCheck == DayOfWeek.Sunday))
                                    {
                                        dayMatches = true;
                                        break;
                                    }
                                }

                                if (!dayMatches)
                                {
                                    return new PromotionValidateResultDto
                                    {
                                        IsValid = false,
                                        Message = $"Khuyến mãi giờ vàng này chỉ áp dụng vào các ngày: {condition.DaysOfWeek}.",
                                        DiscountAmount = 0,
                                        FinalAmount = validateDto.OrderAmount
                                    };
                                }
                            }
                            break;
                    }
                }
            }

            // Calculate discount
            decimal discountAmount = 0;
            if (promotion.DiscountType == PromotionType.Percentage)
            {
                discountAmount = validateDto.OrderAmount * (promotion.DiscountValue / 100m);
                if (promotion.MaxDiscountAmount.HasValue && discountAmount > promotion.MaxDiscountAmount.Value)
                {
                    discountAmount = promotion.MaxDiscountAmount.Value;
                }
            }
            else if (promotion.DiscountType == PromotionType.FixedAmount)
            {
                discountAmount = promotion.DiscountValue;
            }

            if (discountAmount > validateDto.OrderAmount)
            {
                discountAmount = validateDto.OrderAmount;
            }

            // Round to nearest VND
            discountAmount = Math.Round(discountAmount);

            return new PromotionValidateResultDto
            {
                IsValid = true,
                Message = "Áp dụng mã khuyến mãi thành công.",
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
                    Message = "Không tìm thấy giao dịch đặt vé tương ứng.",
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
                    Message = "Mã khuyến mãi không tồn tại.",
                    DiscountAmount = booking.DiscountAmount,
                    NewTotalAmount = booking.TotalAmount
                };
            }

            // Calculate original base amount (TotalAmount + DiscountAmount - ServiceFee or whatever is correct)
            var baseAmount = booking.TotalAmount + booking.DiscountAmount;
            var validateDto = new PromotionValidateDto
            {
                PromoCode = applyDto.PromoCode,
                UserId = booking.UserId,
                OrderAmount = baseAmount,
                ShowtimeId = booking.ShowtimeId
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

            // Link to Booking and update amounts
            booking.DiscountAmount = validateResult.DiscountAmount;
            booking.TotalAmount = baseAmount - validateResult.DiscountAmount;
            booking.PromotionId = promotion.PromotionId;
            await _promotionRepository.UpdateBookingAsync(booking);

            // Increment usage limit counts
            promotion.CurrentUsage++;
            await _promotionRepository.UpdatePromotionAsync(promotion);

            // Save user promotion usage log
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

            // Log mapping table BookingPromotion
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
                Message = "Áp dụng khuyến mãi thành công.",
                DiscountAmount = booking.DiscountAmount,
                NewTotalAmount = booking.TotalAmount
            };
        }
    }
}
