using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.Services.Interfaces;
using CinemaBooking.API.Domain.Exceptions;

namespace CinemaBooking.API.Services.Implementations
{
    public class PricingOptions
    {
        public decimal DefaultTicketPrice { get; set; } = 80000m;
    }

    public class PricingService : IPricingService
    {
        private readonly CinemaDbContext _context;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly PricingOptions _options;

        private const string RULE_CACHE_KEY = "PRICING_RULES_V2";

        public PricingService(
            CinemaDbContext context,
            IMapper mapper,
            IMemoryCache cache,
            IOptions<PricingOptions> options)
        {
            _context = context;
            _mapper = mapper;
            _cache = cache;
            _options = options.Value;
        }

        #region RULE CRUD (unchanged)

        public async Task<ApiResponse<IEnumerable<PricingRuleDto>>> GetAllRulesAsync(CancellationToken cancellationToken = default)
        {
            var rules = await _context.PricingRules
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return ApiResponse.Success(_mapper.Map<IEnumerable<PricingRuleDto>>(rules));
        }

        public async Task<ApiResponse<PricingRuleDto>> GetRuleByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var rule = await _context.PricingRules
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (rule == null)
                throw new NotFoundException($"Rule with ID {id} not found.");

            return ApiResponse.Success(_mapper.Map<PricingRuleDto>(rule));
        }

        public async Task<ApiResponse<PricingRuleDto>> CreateRuleAsync(PricingRuleCreateUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var rule = _mapper.Map<PricingRule>(dto);
            rule.CreatedAt = DateTime.UtcNow;

            await _context.PricingRules.AddAsync(rule, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _cache.Remove(RULE_CACHE_KEY);

            return ApiResponse.Success(_mapper.Map<PricingRuleDto>(rule));
        }

        public async Task<ApiResponse<PricingRuleDto>> UpdateRuleAsync(int id, PricingRuleCreateUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var rule = await _context.PricingRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (rule == null)
                throw new NotFoundException($"Rule with ID {id} not found.");

            _mapper.Map(dto, rule);

            await _context.SaveChangesAsync(cancellationToken);

            _cache.Remove(RULE_CACHE_KEY);

            return ApiResponse.Success(_mapper.Map<PricingRuleDto>(rule));
        }

        public async Task<ApiResponse<bool>> DeleteRuleAsync(int id, CancellationToken cancellationToken = default)
        {
            var rule = await _context.PricingRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (rule == null)
                throw new NotFoundException($"Rule with ID {id} not found.");

            _context.PricingRules.Remove(rule);
            await _context.SaveChangesAsync(cancellationToken);

            _cache.Remove(RULE_CACHE_KEY);

            return ApiResponse.Success(true);
        }

        #endregion

        #region COMPUTE ENGINE V2

        public async Task<ApiResponse<PricingComputeResponseDto>> ComputePriceAsync(
            PricingComputeRequestDto request,
            CancellationToken cancellationToken = default)
        {
            // 1. Resolve time
            var startTime = await ResolveStartTime(request, cancellationToken);

            // 2. Resolve hall
            var hall = await _context.Halls
                .Include(h => h.HallType)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HallId == request.HallId, cancellationToken);

            var hallType = hall?.HallType?.TypeName ?? "STANDARD";

            // 3. Normalize context
            var seatType = NormalizeSeatType(request.SeatType);
            var normalizedHallType = NormalizeHallType(hallType);
            var dayType = CalculateDayType(startTime);
            var timeSlot = CalculateTimeSlot(startTime);

            // Check if this seat type actually exists in this hall
            var dbSeatTypesToCheck = seatType switch
            {
                "GHẾ TIÊU CHUẨN" => new[] { "STANDARD", "STANDARD SEAT", "GHẾ TIÊU CHUẨN", "THƯỜNG" },
                "GHẾ ĐÔI" => new[] { "SWEETBOX", "COUPLE", "GHẾ ĐÔI", "ĐÔI" },
                _ => new[] { seatType }
            };

            var isSeatAvailable = await _context.Seats
                .AnyAsync(s => s.HallId == request.HallId 
                               && s.IsDeleted == false
                               && dbSeatTypesToCheck.Contains(s.SeatType.TypeName.ToUpper()), cancellationToken);

            // 4. Get rules (CACHE)
            var rules = await GetRulesAsync(cancellationToken);

            // 5. Filter matched rules
            var matchedRules = rules
                .Where(r => IsMatch(r, seatType, normalizedHallType, dayType, timeSlot))
                .ToList();

            // 6. Resolve price components dynamically
            decimal basePrice = _options.DefaultTicketPrice;
            decimal seatSurcharge = 0;
            decimal hallSurcharge = 0;

            // LUẬT GIÁ NỀN: Chỉ xét những luật không chỉ định đích danh Ghế và Phòng
            var baseRule = matchedRules
                .Where(r => r.SeatType == "ALL" && r.HallType == "ALL")
                .OrderByDescending(r => CalculateScore(r, seatType, normalizedHallType, dayType, timeSlot))
                .FirstOrDefault();

            if (baseRule != null)
            {
                basePrice = baseRule.BasePrice;
            }

            // LUẬT PHỤ THU GHẾ: Chỉ xét những luật chỉ định đích danh Loại Ghế (và không bị lẫn cấu hình Phòng độc lập)
            var seatRule = matchedRules
                .Where(r => r.SeatType == seatType && r.HallType == "ALL") // <-- Đảm bảo HallType == "ALL" để chỉ tính phụ thu ghế
                .OrderByDescending(r => CalculateScore(r, seatType, normalizedHallType, dayType, timeSlot))
                .FirstOrDefault();

            if (seatRule != null)
            {
                seatSurcharge = seatRule.BasePrice;
            }

            // LUẬT PHỤ THU PHÒNG: Chỉ xét những luật chỉ định đích danh Loại Phòng (và không bị lẫn cấu hình Ghế độc lập)
            var hallRule = matchedRules
                .Where(r => r.HallType == normalizedHallType && r.SeatType == "ALL") // <-- Đảm bảo SeatType == "ALL" để chỉ tính phụ thu phòng
                .OrderByDescending(r => CalculateScore(r, seatType, normalizedHallType, dayType, timeSlot))
                .FirstOrDefault();

            if (hallRule != null)
            {
                hallSurcharge = hallRule.BasePrice;
            }

            // (Tùy chọn nâng cao) LUẬT TỔ HỢP ĐẶC BIỆT: Ghế cụ thể nằm trong Phòng cụ thể (Ví dụ: Ghế Đôi trong phòng IMAX)
            var comboRule = matchedRules
                .Where(r => r.SeatType == seatType && r.HallType == normalizedHallType)
                .OrderByDescending(r => CalculateScore(r, seatType, normalizedHallType, dayType, timeSlot))
                .FirstOrDefault();

            if (comboRule != null)
            {
                // Nếu rạp có cấu hình riêng cho cặp bài trùng này, luật này sẽ ghi đè cả phụ thu ghế và phòng lẻ
                seatSurcharge = 0; 
                hallSurcharge = comboRule.BasePrice; // Lấy trọn gói tiền phụ thu của combo này
            }

            decimal finalPrice = basePrice + seatSurcharge + hallSurcharge;

            // 7. BREAKDOWN
            var breakdown = new List<string>
            {
                $"SeatType={seatType}, HallType={normalizedHallType}, DayType={dayType}, TimeSlot={timeSlot}",
                baseRule != null
                    ? $"Matched Base Rule ID {baseRule.Id} (BasePrice: {basePrice:F0})"
                    : $"No base rule matched, using default price: {basePrice:F0}",
                seatRule != null
                    ? $"Matched Seat Rule ID {seatRule.Id} (SeatSurcharge: {seatSurcharge:F0})"
                    : "No specific seat rule matched (SeatSurcharge: 0)",
                hallRule != null
                    ? $"Matched Hall Rule ID {hallRule.Id} (HallSurcharge: {hallSurcharge:F0})"
                    : "No specific hall rule matched (HallSurcharge: 0)",
                $"FinalPrice: {finalPrice:F0}"
            };

            return ApiResponse.Success(new PricingComputeResponseDto
            {
                FinalPrice = finalPrice,
                IsSeatTypeAvailable = isSeatAvailable,
                MatchedRule = baseRule != null ? _mapper.Map<PricingRuleDto>(baseRule) : null,
                MatchedRuleId = baseRule?.Id,
                BasePrice = basePrice,
                SeatSurcharge = seatSurcharge,
                HallSurcharge = hallSurcharge,
                Breakdown = breakdown,
                ContextSeatType = seatType,
                ContextHallType = normalizedHallType,
                ContextDayType = dayType,
                ContextTimeSlot = timeSlot
            });
        }

        #endregion

        #region RULE ENGINE CORE

        private int CalculateScore(PricingRule r, string seat, string hall, string day, string time)
        {
            int score = r.Priority;

            if (r.SeatType == seat) score += 100;
            if (r.HallType == hall) score += 100;
            if (r.DayType == day) score += 50;
            if (r.TimeSlot == time) score += 50;

            return score;
        }

        private bool IsMatch(PricingRule r, string seat, string hall, string day, string time)
        {
            return (r.SeatType == "ALL" || r.SeatType == seat)
                && (r.HallType == "ALL" || r.HallType == hall)
                && (r.DayType == "ALL" || r.DayType == day)
                && (r.TimeSlot == "ALL" || r.TimeSlot == time);
        }

        private async Task<List<PricingRule>> GetRulesAsync(CancellationToken ct)
        {
            return await _cache.GetOrCreateAsync(RULE_CACHE_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                return await _context.PricingRules
                    .Where(r => r.Status == "ACTIVE")
                    .AsNoTracking()
                    .ToListAsync(ct);
            });
        }

        #endregion

        #region CONTEXT RESOLVERS

        private async Task<DateTime> ResolveStartTime(PricingComputeRequestDto request, CancellationToken ct)
        {
            if (request.ShowtimeId > 0)
            {
                var showtime = await _context.Showtimes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ShowtimeId == request.ShowtimeId, ct);

                if (showtime != null)
                    return showtime.StartTime;
            }

            return request.StartTime ?? DateTime.UtcNow;
        }

        private static string CalculateDayType(DateTime dt)
        {
            if ((dt.Month == 1 && dt.Day == 1) ||
                (dt.Month == 4 && dt.Day == 30) ||
                (dt.Month == 5 && dt.Day == 1) ||
                (dt.Month == 9 && dt.Day == 2))
                return "HOLIDAY";

            return (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday)
                ? "WEEKEND"
                : "WEEKDAY";
        }

        private static string CalculateTimeSlot(DateTime dt)
        {
            return dt.Hour switch
            {
                >= 6 and < 12 => "MORNING",
                >= 12 and < 17 => "AFTERNOON",
                >= 17 and < 22 => "EVENING",
                _ => "NIGHT"
            };
        }

        private static string NormalizeSeatType(string seat)
        {
            if (string.IsNullOrWhiteSpace(seat)) return "GHẾ TIÊU CHUẨN";
            seat = seat.Trim().ToUpper();

            return seat switch
            {
                "THƯỜNG" or "STANDARD" or "NORMAL" or "GHẾ TIÊU CHUẨN" => "GHẾ TIÊU CHUẨN",
                "GHẾ ĐÔI" or "COUPLE" or "ĐÔI" => "GHẾ ĐÔI",
                _ => seat
            };
        }

   private static string NormalizeHallType(string hall)
{
    if (string.IsNullOrWhiteSpace(hall)) return "PHÒNG CHIẾU 2D TIÊU CHUẨN";
    hall = hall.Trim().ToUpper();           

    if (hall.Contains("IMAX")) return "IMAX";
    if (hall.Contains("VIP")) return "VIP";
    if (hall.Contains("3D") || hall.Contains("PHÒNG CHIẾU 3D")) return "PHÒNG CHIẾU 3D";

    if (hall.Contains("2D") || hall.Contains("STANDARD") || hall.Contains("NORMAL") || hall.Contains("THƯỜNG"))
    {
        return "PHÒNG CHIẾU 2D TIÊU CHUẨN";
    }

    return hall;
}
    
        #endregion
    }
}