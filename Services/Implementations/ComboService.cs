using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Bookings;
using CinemaBooking.API.DTOs.Combos;
using CinemaBooking.API.Models.Bookings;
using CinemaBooking.API.Models.Combos;
using CinemaBooking.API.Models.Payments;
using CinemaBooking.API.Models.Promotions;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;
using CinemaBooking.API.DTOs.Common;

namespace CinemaBooking.API.Services.Implementations
{
    public class ComboService : IComboService
    {
        private readonly IComboRepository _comboRepository;
        private readonly CinemaDbContext _context;
        private readonly IMapper _mapper;
        private readonly IServiceProvider _serviceProvider;

        public ComboService(
            IComboRepository comboRepository, 
            CinemaDbContext context, 
            IMapper mapper, 
            IServiceProvider serviceProvider)
        {
            _comboRepository = comboRepository;
            _context = context;
            _mapper = mapper;
            _serviceProvider = serviceProvider;
        }

        #region Customer Combos

        public async Task<ApiResponse<PagedResultDto<ComboDto>>> GetActiveCombosAsync(ComboQueryParameters queryParams)
        {
            queryParams.IsActive = true;
            var (combos, totalCount) = await _comboRepository.GetPagedCombosAsync(queryParams);
            var dtos = _mapper.Map<List<ComboDto>>(combos);

            return ApiResponse.Success(new PagedResultDto<ComboDto>
            {
                Items = dtos,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            });
        }

        public async Task<ApiResponse<ComboDto>> GetComboByIdAsync(int id)
        {
            var combo = await _comboRepository.GetComboByIdAsync(id);
            if (combo == null || combo.IsDeleted)
            {
                throw new NotFoundException($"Combo with ID {id} not found.");
            }

            var dto = _mapper.Map<ComboDto>(combo);
            return ApiResponse.Success(dto);
        }

        #endregion

        #region Booking Combos Mapping

        public async Task<ApiResponse<BookingResponseDto>> AddCombosToBookingAsync(int bookingId, List<OrderComboInputDto> combosDto, CancellationToken cancellationToken = default)
        {
            return await UpdateBookingCombosInternalAsync(bookingId, combosDto, cancellationToken);
        }

        public async Task<ApiResponse<BookingResponseDto>> UpdateBookingCombosAsync(int bookingId, List<OrderComboInputDto> combosDto, CancellationToken cancellationToken = default)
        {
            return await UpdateBookingCombosInternalAsync(bookingId, combosDto, cancellationToken);
        }

        public async Task<ApiResponse<BookingResponseDto>> DeleteCombosFromBookingAsync(int bookingId, CancellationToken cancellationToken = default)
        {
            return await UpdateBookingCombosInternalAsync(bookingId, new List<OrderComboInputDto>(), cancellationToken);
        }

        private async Task<ApiResponse<BookingResponseDto>> UpdateBookingCombosInternalAsync(int bookingId, List<OrderComboInputDto> combosDto, CancellationToken cancellationToken)
        {
            // 1. Get booking
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.OrderCombos)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.Cinema)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId, cancellationToken);

            if (booking == null)
            {
                throw new NotFoundException($"Booking with ID {bookingId} not found.");
            }

            // 2. Business Check: Can only modify combos on pending bookings
            if (booking.BookingStatus != BookingStatus.Pending.ToString())
            {
                throw new BusinessException("Chỉ có thể thay đổi bắp nước cho đơn hàng ở trạng thái Chờ thanh toán.");
            }

            // 3. Clear existing combos
            _context.OrderCombos.RemoveRange(booking.OrderCombos);
            booking.OrderCombos.Clear();

            // 4. Add new combos
            foreach (var input in combosDto)
            {
                if (input.Quantity <= 0) continue;

                var combo = await _context.Combos.FirstOrDefaultAsync(c => c.Id == input.ComboId && c.IsActive && !c.IsDeleted, cancellationToken);
                if (combo == null)
                {
                    throw new BusinessException($"Combo ID {input.ComboId} không khả dụng.");
                }

                booking.OrderCombos.Add(new OrderCombo
                {
                    BookingId = booking.BookingId,
                    ComboId = combo.Id,
                    Quantity = input.Quantity,
                    Price = combo.Price
                });
            }

            // 5. Recalculate TotalAmount
            decimal seatSubtotal = booking.BookingSeats.Sum(bs => bs.UnitPrice);
            decimal serviceFee = booking.BookingSeats.Any() ? 5000m : 0m;
            decimal promoDiscount = 0;

            if (booking.PromotionId.HasValue)
            {
                var promotion = await _context.Promotions.FindAsync(new object[] { booking.PromotionId.Value }, cancellationToken);
                if (promotion != null)
                {
                    if (promotion.DiscountType == PromotionType.Percentage)
                    {
                        promoDiscount = seatSubtotal * (promotion.DiscountValue / 100m);
                    }
                    else
                    {
                        promoDiscount = promotion.DiscountValue;
                    }
                }
            }

            decimal combosTotal = booking.OrderCombos.Sum(oc => oc.Price * oc.Quantity);
            decimal pointsDiscount = booking.PointsDiscountAmount ?? 0m;

            booking.TotalAmount = Math.Max(0, seatSubtotal + serviceFee - promoDiscount + combosTotal - pointsDiscount);

            // Update pending payment amount
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == booking.BookingId && p.PaymentStatus == PaymentStatus.Pending.ToString(), cancellationToken);
            if (payment != null)
            {
                payment.Amount = booking.TotalAmount;
                _context.Payments.Update(payment);
            }

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync(cancellationToken);

            // 6. Return updated booking details
            var bookingDto = _mapper.Map<BookingDto>(booking);
            
            // Map the QR URL dynamically
            var response = new BookingResponseDto();
            _mapper.Map(bookingDto, response);
            response.QRCodeUrl = booking.QRCodeUrl;

            // Check if payment URL needs to be updated
            var paymentService = _serviceProvider.GetService(typeof(IPaymentService)) as IPaymentService;
            string? paymentUrl = null;
            if (paymentService != null)
            {
                try
                {
                    paymentUrl = await paymentService.CreateVietQRPaymentUrlAsync(booking.BookingId);
                }
                catch
                {
                    // Fail silently, fallback
                }
            }
            
            response.PaymentUrl = paymentUrl ?? $"/api/payments/retry?bookingId={booking.BookingId}&gateway=VietQR";

            // Map combos list manually to ensure populated
            response.Combos = booking.OrderCombos.Select(oc => new OrderComboDto
            {
                ComboId = oc.ComboId,
                ComboName = oc.Combo != null ? oc.Combo.Name : _context.Combos.Find(oc.ComboId)?.Name ?? "Combo",
                Quantity = oc.Quantity,
                Price = oc.Price
            }).ToList();

            return ApiResponse.Success(response, "Cập nhật bắp nước thành công.");
        }

        #endregion

        #region Smart Recommendations

        public async Task<ApiResponse<IEnumerable<ComboRecommendationDto>>> GetRecommendedCombosAsync(int showtimeId, int ticketCount, CancellationToken cancellationToken = default)
        {
            var recommended = new List<ComboRecommendationDto>();

            // Load active combos
            var combos = await _context.Combos
                .Include(c => c.ComboItems)
                .ThenInclude(ci => ci.Product)
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync(cancellationToken);

            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId, cancellationToken);

            var comboDtos = _mapper.Map<List<ComboDto>>(combos);

            // Recommendation 1: Base recommendations on ticket count (1-2 seats -> Couple Combo, 3-5 seats -> Family Combo)
            if (ticketCount >= 1 && ticketCount <= 2)
            {
                var coupleCombo = comboDtos.FirstOrDefault(c => c.Name.ToLower().Contains("couple"));
                if (coupleCombo != null)
                {
                    recommended.Add(new ComboRecommendationDto
                    {
                        Combo = coupleCombo,
                        Reason = "Phù hợp cho buổi xem phim 2 người, tiết kiệm hơn 11%!"
                    });
                }
            }
            else if (ticketCount >= 3 && ticketCount <= 5)
            {
                var familyCombo = comboDtos.FirstOrDefault(c => c.Name.ToLower().Contains("family"));
                if (familyCombo != null)
                {
                    recommended.Add(new ComboRecommendationDto
                    {
                        Combo = familyCombo,
                        Reason = "Phù hợp cho nhóm bạn & gia đình, siêu tiết kiệm 15%!"
                    });
                }
            }

            // Recommendation 2: Special limited movie edition combos (if movie is a campaign blockbuster like Doctor Strange or Minions)
            if (showtime?.Movie != null)
            {
                var movieTitle = showtime.Movie.Title.ToLower();
                if (movieTitle.Contains("strange") || movieTitle.Contains("marvel") || movieTitle.Contains("maverick") || movieTitle.Contains("minion"))
                {
                    var specialCombo = comboDtos.FirstOrDefault(c => c.Name.ToLower().Contains("special") || c.Name.ToLower().Contains("limited") || c.Name.ToLower().Contains("minion"));
                    if (specialCombo != null && recommended.All(r => r.Combo.Id != specialCombo.Id))
                    {
                        recommended.Add(new ComboRecommendationDto
                        {
                            Combo = specialCombo,
                            Reason = $"Combo độc quyền phiên bản giới hạn theo phim {showtime.Movie.Title}!"
                        });
                    }
                }
            }

            // Recommendation 3: Happy Hour/Promo Hours (Morning showtimes before 12:00 or Late Night after 22:00)
            if (showtime != null)
            {
                var hour = showtime.StartTime.Hour;
                if (hour < 12 || hour >= 22)
                {
                    var betaCombo = comboDtos.FirstOrDefault(c => c.Name.ToLower().Contains("beta"));
                    if (betaCombo != null && recommended.All(r => r.Combo.Id != betaCombo.Id))
                    {
                        recommended.Add(new ComboRecommendationDto
                        {
                            Combo = betaCombo,
                            Reason = "Suất chiếu khung giờ vàng - Ưu đãi bắp nước giá tốt nhất!"
                        });
                    }
                }
            }

            // Fallback: If no recommendations matching, return top 2 display order combos
            if (!recommended.Any() && comboDtos.Any())
            {
                var defaults = comboDtos.Take(2);
                foreach (var c in defaults)
                {
                    recommended.Add(new ComboRecommendationDto
                    {
                        Combo = c,
                        Reason = "Bắp nước bán chạy nhất tuần này tại rạp!"
                    });
                }
            }

            return ApiResponse.Success(recommended.AsEnumerable());
        }

        #endregion

        #region Admin Combos Management

        public async Task<ApiResponse<PagedResultDto<ComboDto>>> GetPagedCombosAdminAsync(ComboQueryParameters queryParams)
        {
            var (combos, totalCount) = await _comboRepository.GetPagedCombosAsync(queryParams);
            var dtos = _mapper.Map<List<ComboDto>>(combos);

            return ApiResponse.Success(new PagedResultDto<ComboDto>
            {
                Items = dtos,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            });
        }

        public async Task<ApiResponse<ComboDto>> CreateComboAsync(ComboCreateUpdateDto createDto)
        {
            // Validation
            ValidateComboInput(createDto);

            var combo = new Combo
            {
                Name = createDto.Name,
                Description = createDto.Description,
                ImageUrl = createDto.ImageUrl,
                Price = createDto.Price,
                OriginalPrice = createDto.OriginalPrice,
                DiscountBadge = createDto.DiscountBadge,
                IsActive = createDto.IsActive,
                DisplayOrder = createDto.DisplayOrder
            };

            // Map items
            foreach (var item in createDto.ComboItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    throw new BusinessException($"Sản phẩm nguyên liệu ID {item.ProductId} không tồn tại.");
                }

                combo.ComboItems.Add(new ComboItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
            }

            await _comboRepository.AddComboAsync(combo);
            await _comboRepository.SaveChangesAsync();

            // Reload to get references mapped
            var dbCombo = await _comboRepository.GetComboByIdAsync(combo.Id);
            return ApiResponse.Success(_mapper.Map<ComboDto>(dbCombo));
        }

        public async Task<ApiResponse<ComboDto>> UpdateComboAsync(int id, ComboCreateUpdateDto updateDto)
        {
            // Validation
            ValidateComboInput(updateDto);

            var combo = await _context.Combos
                .Include(c => c.ComboItems)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (combo == null)
            {
                throw new NotFoundException($"Combo với ID {id} không tồn tại.");
            }

            combo.Name = updateDto.Name;
            combo.Description = updateDto.Description;
            combo.ImageUrl = updateDto.ImageUrl;
            combo.Price = updateDto.Price;
            combo.OriginalPrice = updateDto.OriginalPrice;
            combo.DiscountBadge = updateDto.DiscountBadge;
            combo.IsActive = updateDto.IsActive;
            combo.DisplayOrder = updateDto.DisplayOrder;

            // Reset and update ComboItems
            _context.ComboItems.RemoveRange(combo.ComboItems);
            combo.ComboItems.Clear();

            foreach (var item in updateDto.ComboItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    throw new BusinessException($"Sản phẩm nguyên liệu ID {item.ProductId} không tồn tại.");
                }

                combo.ComboItems.Add(new ComboItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
            }

            await _comboRepository.UpdateComboAsync(combo);
            await _comboRepository.SaveChangesAsync();

            var dbCombo = await _comboRepository.GetComboByIdAsync(combo.Id);
            return ApiResponse.Success(_mapper.Map<ComboDto>(dbCombo));
        }

        public async Task<ApiResponse<bool>> DeleteComboAsync(int id)
        {
            var combo = await _comboRepository.GetComboByIdAsync(id);
            if (combo == null)
            {
                throw new NotFoundException($"Combo với ID {id} không tồn tại.");
            }

            await _comboRepository.DeleteComboAsync(combo);
            var result = await _comboRepository.SaveChangesAsync();
            return ApiResponse.Success(result);
        }

        public async Task<ApiResponse<bool>> ToggleComboStatusAsync(int id, bool isActive)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo == null || combo.IsDeleted)
            {
                throw new NotFoundException($"Combo với ID {id} không tồn tại.");
            }

            combo.IsActive = isActive;
            _context.Entry(combo).Property(c => c.IsActive).IsModified = true;
            var result = await _context.SaveChangesAsync() > 0;
            return ApiResponse.Success(result);
        }

        public async Task<ApiResponse<bool>> UpdateComboDisplayOrderAsync(int id, int displayOrder)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo == null || combo.IsDeleted)
            {
                throw new NotFoundException($"Combo với ID {id} không tồn tại.");
            }

            combo.DisplayOrder = displayOrder;
            _context.Entry(combo).Property(c => c.DisplayOrder).IsModified = true;
            var result = await _context.SaveChangesAsync() > 0;
            return ApiResponse.Success(result);
        }

        private void ValidateComboInput(ComboCreateUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ValidationException("Tên Combo không được để trống.");
            }

            if (dto.Price < 0)
            {
                throw new ValidationException("Giá bán Combo không được nhỏ hơn 0.");
            }

            if (dto.OriginalPrice.HasValue && dto.OriginalPrice.Value < dto.Price)
            {
                throw new ValidationException("Giá gốc không được nhỏ hơn giá bán.");
            }

            if (dto.ComboItems == null || !dto.ComboItems.Any())
            {
                throw new ValidationException("Combo phải chứa ít nhất một sản phẩm.");
            }

            foreach (var item in dto.ComboItems)
            {
                if (item.Quantity <= 0)
                {
                    throw new ValidationException("Số lượng sản phẩm trong Combo phải lớn hơn 0.");
                }
            }
        }

        #endregion

        #region Admin Products Management

        public async Task<ApiResponse<IEnumerable<ProductDto>>> GetAllProductsAsync()
        {
            var products = await _comboRepository.GetAllProductsAsync();
            var dtos = _mapper.Map<List<ProductDto>>(products);
            return ApiResponse.Success(dtos.AsEnumerable());
        }

        public async Task<ApiResponse<ProductDto>> CreateProductAsync(ProductCreateUpdateDto createDto)
        {
            if (string.IsNullOrWhiteSpace(createDto.Name))
            {
                throw new ValidationException("Tên sản phẩm không được rỗng.");
            }
            if (createDto.Price < 0)
            {
                throw new ValidationException("Giá sản phẩm không được âm.");
            }

            var product = _mapper.Map<Product>(createDto);
            await _comboRepository.AddProductAsync(product);
            await _comboRepository.SaveChangesAsync();

            return ApiResponse.Success(_mapper.Map<ProductDto>(product));
        }

        public async Task<ApiResponse<ProductDto>> UpdateProductAsync(int id, ProductCreateUpdateDto updateDto)
        {
            if (string.IsNullOrWhiteSpace(updateDto.Name))
            {
                throw new ValidationException("Tên sản phẩm không được rỗng.");
            }
            if (updateDto.Price < 0)
            {
                throw new ValidationException("Giá sản phẩm không được âm.");
            }

            var product = await _comboRepository.GetProductByIdAsync(id);
            if (product == null || product.IsDeleted)
            {
                throw new NotFoundException($"Sản phẩm với ID {id} không tồn tại.");
            }

            _mapper.Map(updateDto, product);
            await _comboRepository.UpdateProductAsync(product);
            await _comboRepository.SaveChangesAsync();

            return ApiResponse.Success(_mapper.Map<ProductDto>(product));
        }

        public async Task<ApiResponse<bool>> DeleteProductAsync(int id)
        {
            var product = await _comboRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                throw new NotFoundException($"Sản phẩm với ID {id} không tồn tại.");
            }

            // Check if product is in use by active combos
            bool inUse = await _context.ComboItems.AnyAsync(ci => ci.ProductId == id && !ci.Combo.IsDeleted);
            if (inUse)
            {
                throw new BusinessException("Không thể xóa sản phẩm này vì đang được sử dụng trong các Combo hiện tại.");
            }

            await _comboRepository.DeleteProductAsync(product);
            var result = await _comboRepository.SaveChangesAsync();
            return ApiResponse.Success(result);
        }

        #endregion
    }
}
