using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Users;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.Models.Users;
using CinemaBooking.API.Models.Bookings;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Services.Implementations
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly CinemaDbContext _context;
        private readonly ILogger<LoyaltyService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public LoyaltyService(
            CinemaDbContext context, 
            ILogger<LoyaltyService> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<ApiResponse<LoyaltyDashboardDto>> GetLoyaltyDashboardAsync(int userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving loyalty dashboard for User ID: {UserId}", userId);
            
            var user = await _context.Users
                .Include(u => u.MemberTier)
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                throw new NotFoundException($"Không tìm thấy người dùng với ID {userId}.");
            }

            // Fallback to Bronze if not set
            if (user.MemberTier == null)
            {
                user.MemberTier = await _context.MemberTiers.FirstAsync(t => t.MemberTierId == 1, cancellationToken);
                user.MemberTierId = 1;
            }

            var nextTier = await _context.MemberTiers
                .Where(t => t.MinPoints > user.LifetimePoints)
                .OrderBy(t => t.MinPoints)
                .FirstOrDefaultAsync(cancellationToken);

            var dto = new LoyaltyDashboardDto
            {
                TierName = user.MemberTier.TierName,
                LoyaltyPoints = user.MembershipPoints,
                LifetimePoints = user.LifetimePoints,
                PointMultiplier = user.MemberTier.PointMultiplier,
                BenefitsDescription = user.MemberTier.BenefitsDescription,
                CashEquivalentValue = user.MembershipPoints * 1000m,
                
                NextTierName = nextTier?.TierName,
                PointsNeededForNextTier = nextTier != null ? (nextTier.MinPoints - user.LifetimePoints) : null
            };

            if (nextTier != null)
            {
                var currentTierMin = user.MemberTier.MinPoints;
                var range = nextTier.MinPoints - currentTierMin;
                var progress = user.LifetimePoints - currentTierMin;
                dto.ProgressionPercent = range > 0 ? Math.Min(100.0, Math.Max(0.0, ((double)progress / range) * 100.0)) : 100.0;
            }
            else
            {
                dto.ProgressionPercent = 100.0;
            }

            return ApiResponse.Success(dto);
        }

        public async Task<ApiResponse<IEnumerable<LoyaltyTransactionDto>>> GetLoyaltyTransactionsAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving loyalty transactions for User ID: {UserId}", userId);

            var query = _context.LoyaltyTransactions
                .Include(t => t.Booking)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(t => new LoyaltyTransactionDto
            {
                LoyaltyTransactionId = t.LoyaltyTransactionId,
                UserId = t.UserId,
                BookingId = t.BookingId,
                BookingCode = t.Booking?.BookingCode,
                PointsChanged = t.PointsChanged,
                TransactionType = t.TransactionType,
                Status = t.Status,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            });

            return ApiResponse.Success(dtos);
        }

        public async Task<ApiResponse<LoyaltyRedeemValidateResultDto>> ValidateRedeemPointsAsync(int userId, LoyaltyRedeemValidateRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating loyalty points redemption request for User ID: {UserId}", userId);

            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException($"Người dùng với ID {userId} không tồn tại.");
            }

            if (request.PointsToRedeem < 10)
            {
                return ApiResponse.Success(new LoyaltyRedeemValidateResultDto
                {
                    IsValid = false,
                    Message = "Số điểm tối thiểu để đổi là 10 điểm (tương ứng 10.000 VNĐ)."
                });
            }

            if (user.MembershipPoints < request.PointsToRedeem)
            {
                return ApiResponse.Success(new LoyaltyRedeemValidateResultDto
                {
                    IsValid = false,
                    Message = $"Bạn không đủ điểm để đổi. Số điểm hiện tại của bạn là {user.MembershipPoints}."
                });
            }

            // Calculate booking subtotal from booking pricing logic (simulating or calling Showtime/Booking details)
            // Fallback calculation using BookingService's amount calculator if we can resolve it
            decimal subtotal = 0;
            var showtime = await _context.Showtimes
                .Include(s => s.Price)
                .FirstOrDefaultAsync(s => s.ShowtimeId == request.ShowtimeId, cancellationToken);

            if (showtime == null)
            {
                return ApiResponse.Success(new LoyaltyRedeemValidateResultDto
                {
                    IsValid = false,
                    Message = "Suất chiếu không tồn tại."
                });
            }

            var seats = await _context.Seats
                .Include(s => s.SeatType)
                .Where(s => request.SeatIds.Contains(s.SeatId))
                .ToListAsync(cancellationToken);

            var pricingService = _serviceProvider.GetService(typeof(IPricingService)) as IPricingService;

            foreach (var seat in seats)
            {
                decimal seatPrice = 0;
                if (pricingService != null)
                {
                    var computeReq = new PricingComputeRequestDto
                    {
                        SeatType = seat.SeatType?.TypeName ?? "STANDARD",
                        HallId = showtime.HallId,
                        MovieId = showtime.MovieId,
                        ShowtimeId = showtime.ShowtimeId
                    };
                    var computeRes = await pricingService.ComputePriceAsync(computeReq, cancellationToken);
                    if (computeRes != null && computeRes.IsSuccess)
                    {
                        seatPrice = computeRes.Data.FinalPrice;
                    }
                }

                if (seatPrice == 0) // Fallback
                {
                    seatPrice = showtime.Price?.Value ?? 80000m;
                }

                subtotal += seatPrice;
            }

            decimal serviceFee = request.SeatIds.Any() ? 5000m : 0m;
            decimal originalTotal = subtotal + serviceFee;

            // 1 point = 1,000 VNĐ
            decimal discountValue = request.PointsToRedeem * 1000m;
            decimal maxAllowedDiscount = originalTotal * 0.50m; // Limit to 50% max

            if (discountValue > maxAllowedDiscount)
            {
                int maxAllowedPoints = (int)Math.Floor(maxAllowedDiscount / 1000m);
                return ApiResponse.Success(new LoyaltyRedeemValidateResultDto
                {
                    IsValid = false,
                    Message = $"Số điểm sử dụng vượt quá giới hạn 50% giá trị đơn hàng. Số điểm tối đa bạn có thể sử dụng cho đơn hàng này là {maxAllowedPoints} điểm (giảm {maxAllowedPoints * 1000:N0} VNĐ)."
                });
            }

            decimal finalTotal = Math.Max(0, originalTotal - discountValue);

            return ApiResponse.Success(new LoyaltyRedeemValidateResultDto
            {
                IsValid = true,
                DiscountAmount = discountValue,
                OriginalTotalAmount = originalTotal,
                FinalTotalAmount = finalTotal,
                Message = $"Áp dụng đổi {request.PointsToRedeem} điểm thành công. Giảm {discountValue:N0} VNĐ."
            });
        }

        public async Task<ApiResponse<LoyaltyAdminStatsDto>> GetAdminLoyaltyStatsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving Admin Loyalty Program Stats");

            var stats = new LoyaltyAdminStatsDto();

            // Total points issued (Completed transaction type Earn, EventBonus, or positive ManualAdjustment)
            stats.TotalPointsIssued = await _context.LoyaltyTransactions
                .Where(t => t.Status == "Completed" && (t.TransactionType == "Earn" || t.TransactionType == "EventBonus" || (t.TransactionType == "ManualAdjustment" && t.PointsChanged > 0)))
                .SumAsync(t => (long)t.PointsChanged, cancellationToken);

            // Total points redeemed (Completed transaction type Redeem, or negative ManualAdjustment)
            var redeemedNegative = await _context.LoyaltyTransactions
                .Where(t => t.Status == "Completed" && (t.TransactionType == "Redeem" || (t.TransactionType == "ManualAdjustment" && t.PointsChanged < 0)))
                .SumAsync(t => (long)t.PointsChanged, cancellationToken);
            stats.TotalPointsRedeemed = Math.Abs(redeemedNegative);

            // Total points currently circulating (Sum of all active users' balance)
            stats.TotalPointsCirculating = await _context.Users.SumAsync(u => (long)u.MembershipPoints, cancellationToken);

            // Member breakdown by tier
            var tiers = await _context.MemberTiers.ToListAsync(cancellationToken);
            foreach (var tier in tiers)
            {
                var count = await _context.Users.CountAsync(u => u.MemberTierId == tier.MemberTierId, cancellationToken);
                stats.MemberCountByTier[tier.TierName] = count;
            }

            // Top loyal members
            var topMembers = await _context.Users
                .Include(u => u.MemberTier)
                .OrderByDescending(u => u.MembershipPoints)
                .Take(10)
                .ToListAsync(cancellationToken);

            stats.TopLoyalMembers = topMembers.Select(u => new TopMemberDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                LoyaltyPoints = u.MembershipPoints,
                LifetimePoints = u.LifetimePoints,
                TierName = u.MemberTier?.TierName ?? "Bronze"
            }).ToList();

            return ApiResponse.Success(stats);
        }

        public async Task<ApiResponse<IEnumerable<LoyaltyTransactionDto>>> GetAllTransactionsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving all loyalty transactions for Admin Panel");

            var query = _context.LoyaltyTransactions
                .Include(t => t.User)
                .Include(t => t.Booking)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.User.FullName.Contains(search) || t.User.Email.Contains(search) || (t.Booking != null && t.Booking.BookingCode.Contains(search)));
            }

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(t => new LoyaltyTransactionDto
            {
                LoyaltyTransactionId = t.LoyaltyTransactionId,
                UserId = t.UserId,
                UserFullName = t.User.FullName,
                BookingId = t.BookingId,
                BookingCode = t.Booking?.BookingCode,
                PointsChanged = t.PointsChanged,
                TransactionType = t.TransactionType,
                Status = t.Status,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            });

            return ApiResponse.Success(dtos);
        }

        public async Task<ApiResponse<bool>> AdjustPointsManuallyAsync(LoyaltyPointsAdjustDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Performing manual points adjustment for User ID: {UserId}, Points: {Points}", dto.UserId, dto.Points);

            var user = await _context.Users
                .Include(u => u.MemberTier)
                .FirstOrDefaultAsync(u => u.UserId == dto.UserId, cancellationToken);

            if (user == null)
            {
                throw new NotFoundException($"Không tìm thấy người dùng với ID {dto.UserId}.");
            }

            if (dto.Points < 0 && user.MembershipPoints < Math.Abs(dto.Points))
            {
                throw new BusinessException("Số dư điểm thành viên của người dùng không đủ để thực hiện khấu trừ.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Create LoyaltyTransaction log
                var log = new LoyaltyTransaction
                {
                    UserId = dto.UserId,
                    PointsChanged = dto.Points,
                    TransactionType = "ManualAdjustment",
                    Status = "Completed",
                    Description = dto.Description,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.LoyaltyTransactions.AddAsync(log, cancellationToken);

                // Update user points
                user.MembershipPoints += dto.Points;
                if (dto.Points > 0)
                {
                    user.LifetimePoints += dto.Points;
                }

                // Check tier progression if points added
                if (dto.Points > 0)
                {
                    await EvaluateUserTierAsync(user, cancellationToken);
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return ApiResponse.Success(true, $"Đã điều chỉnh thành công {dto.Points:+#;-#;0} điểm cho người dùng.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to manually adjust points for user {UserId}", dto.UserId);
                throw;
            }
        }

        public async Task<ApiResponse<int>> CreatePendingEarnPointsAsync(int userId, int bookingId, decimal actualPaidAmount, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating pending earn points transaction for Booking ID: {BookingId}", bookingId);

            var user = await _context.Users
                .Include(u => u.MemberTier)
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                throw new NotFoundException($"User ID {userId} not found.");
            }

            var tierMultiplier = user.MemberTier?.PointMultiplier ?? 1.0m;
            
            // 10,000 VNĐ = 1 Point
            int basePoints = (int)Math.Floor(actualPaidAmount / 10000m);
            int calculatedPoints = (int)Math.Round(basePoints * tierMultiplier);

            if (calculatedPoints <= 0)
            {
                return ApiResponse.Success(0, "Số tiền thanh toán quá nhỏ để được cộng điểm.");
            }

            var loyaltyTx = new LoyaltyTransaction
            {
                UserId = userId,
                BookingId = bookingId,
                PointsChanged = calculatedPoints,
                TransactionType = "Earn",
                Status = "Pending",
                Description = $"Tích lũy mua vé đơn hàng #{bookingId}",
                CreatedAt = DateTime.UtcNow
            };

            await _context.LoyaltyTransactions.AddAsync(loyaltyTx, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Success(calculatedPoints, "Giao dịch tích lũy điểm đang chờ xử lý.");
        }

        public async Task<ApiResponse<bool>> RedeemPointsForBookingAsync(int userId, int bookingId, int pointsToRedeem, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing points redemption for Booking ID: {BookingId}, Points: {Points}", bookingId, pointsToRedeem);

            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException($"User ID {userId} not found.");
            }

            if (user.MembershipPoints < pointsToRedeem)
            {
                throw new BusinessException("Số dư điểm của bạn không đủ.");
            }

            // Deduct immediately to prevent double spending
            user.MembershipPoints -= pointsToRedeem;
            _context.Users.Update(user);

            var loyaltyTx = new LoyaltyTransaction
            {
                UserId = userId,
                BookingId = bookingId,
                PointsChanged = -pointsToRedeem,
                TransactionType = "Redeem",
                Status = "Completed",
                Description = $"Sử dụng điểm giảm giá cho đơn vé #{bookingId}",
                CreatedAt = DateTime.UtcNow
            };

            await _context.LoyaltyTransactions.AddAsync(loyaltyTx, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Success(true, "Trừ điểm thành công.");
        }

        public async Task<ApiResponse<bool>> RefundRedeemedPointsAsync(int bookingId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing refund of redeemed points for Booking ID: {BookingId}", bookingId);

            var booking = await _context.Bookings.FindAsync(new object[] { bookingId }, cancellationToken);
            if (booking == null)
            {
                throw new NotFoundException($"Booking ID {bookingId} not found.");
            }

            if (!booking.PointsRedeemed.HasValue || booking.PointsRedeemed.Value <= 0)
            {
                return ApiResponse.Success(false, "Đơn hàng này không sử dụng điểm tích lũy.");
            }

            var pointsToRefund = booking.PointsRedeemed.Value;
            var user = await _context.Users.FindAsync(new object[] { booking.UserId }, cancellationToken);
            
            if (user != null)
            {
                user.MembershipPoints += pointsToRefund;
                _context.Users.Update(user);

                var loyaltyTx = new LoyaltyTransaction
                {
                    UserId = booking.UserId,
                    BookingId = bookingId,
                    PointsChanged = pointsToRefund,
                    TransactionType = "Refund",
                    Status = "Completed",
                    Description = $"Hoàn trả điểm tích lũy do hủy đơn vé #{bookingId}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.LoyaltyTransactions.AddAsync(loyaltyTx, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Cancel any pending earn points transactions as well
            var pendingEarn = await _context.LoyaltyTransactions
                .Where(t => t.BookingId == bookingId && t.TransactionType == "Earn" && t.Status == "Pending")
                .ToListAsync(cancellationToken);

            foreach (var earn in pendingEarn)
            {
                earn.Status = "Cancelled";
                earn.Description = "Hủy tích lũy do đơn đặt vé bị hủy hoặc hoàn trả";
                _context.LoyaltyTransactions.Update(earn);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Success(true, "Hoàn trả điểm thành viên thành công.");
        }

        public async Task<ApiResponse<bool>> CompleteEarnPointsAsync(int bookingId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Completing pending earn points transaction for Booking ID: {BookingId}", bookingId);

            var pendingEarn = await _context.LoyaltyTransactions
                .Include(t => t.User)
                .ThenInclude(u => u.MemberTier)
                .FirstOrDefaultAsync(t => t.BookingId == bookingId && t.TransactionType == "Earn" && t.Status == "Pending", cancellationToken);

            if (pendingEarn == null)
            {
                return ApiResponse.Success(false, "Không tìm thấy giao dịch tích lũy điểm chờ xử lý cho đơn hàng này.");
            }

            var user = pendingEarn.User;
            if (user == null)
            {
                throw new NotFoundException($"Không tìm thấy người dùng cho giao dịch tích lũy điểm #{pendingEarn.LoyaltyTransactionId}.");
            }

            // Update status
            pendingEarn.Status = "Completed";
            pendingEarn.Description = $"Tích lũy mua vé đơn hàng #{bookingId} thành công";
            _context.LoyaltyTransactions.Update(pendingEarn);

            // Credit points to user
            user.MembershipPoints += pendingEarn.PointsChanged;
            user.LifetimePoints += pendingEarn.PointsChanged;

            // Evaluate tier progression
            await EvaluateUserTierAsync(user, cancellationToken);

            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Success(true, $"Tích lũy thành công {pendingEarn.PointsChanged} điểm cho người dùng.");
        }

        private async Task EvaluateUserTierAsync(User user, CancellationToken cancellationToken)
        {
            var tiers = await _context.MemberTiers
                .OrderByDescending(t => t.MinPoints)
                .ToListAsync(cancellationToken);

            foreach (var tier in tiers)
            {
                if (user.LifetimePoints >= tier.MinPoints)
                {
                    if (user.MemberTierId != tier.MemberTierId)
                    {
                        user.MemberTierId = tier.MemberTierId;
                    }
                    break;
                }
            }
        }
    }
}
