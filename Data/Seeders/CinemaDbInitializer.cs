using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Models.Cinemas;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.Models.Users;
using CinemaBooking.API.Models.Promotions;
using CinemaBooking.API.Models.Combos;

namespace CinemaBooking.API.Data.Seeders
{
    public static class CinemaDbInitializer
    {
        public static void Initialize(CinemaDbContext context)
        {
            try
            {
                // Apply migrations if any pending
                if (context.Database.GetPendingMigrations().Any())
                {
                    context.Database.Migrate();
                }

                // Fix existing reviews status to Approved and IsApproved = true
                var legacyReviews = context.Reviews.IgnoreQueryFilters().Where(r => r.Status == "" || r.Status == null).ToList();
                if (legacyReviews.Any())
                {
                    foreach (var r in legacyReviews)
                    {
                        r.Status = "Approved";
                        r.IsApproved = true;
                    }
                    context.SaveChanges();
                }

                // Seed MemberTiers
                if (!context.MemberTiers.IgnoreQueryFilters().Any())
                {
                    context.Database.OpenConnection();
                    try
                    {
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT MemberTiers ON");
                        context.MemberTiers.AddRange(
                            new MemberTier { MemberTierId = 1, TierName = "Bronze", MinPoints = 0, PointMultiplier = 1.0m, BenefitsDescription = "Tích lũy cơ bản 10k = 1đ (Hệ số x1.0)" },
                            new MemberTier { MemberTierId = 2, TierName = "Silver", MinPoints = 100, PointMultiplier = 1.2m, BenefitsDescription = "Tích lũy x1.2 + Quà tặng sinh nhật" },
                            new MemberTier { MemberTierId = 3, TierName = "Gold", MinPoints = 300, PointMultiplier = 1.5m, BenefitsDescription = "Tích lũy x1.5 + Đổi vé miễn phí + Bắp rang nước ngọt" },
                            new MemberTier { MemberTierId = 4, TierName = "Platinum", MinPoints = 600, PointMultiplier = 2.0m, BenefitsDescription = "Tích lũy x2.0 + Lối đi ưu tiên + Vé VIP sneak-show" }
                        );
                        context.SaveChanges();
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT MemberTiers OFF");
                    }
                    finally
                    {
                        context.Database.CloseConnection();
                    }
                }

                // Update existing users to Bronze if not set
                var usersToUpdate = context.Users.IgnoreQueryFilters().Where(u => u.MemberTierId == null).ToList();
                if (usersToUpdate.Any())
                {
                    foreach (var u in usersToUpdate)
                    {
                        u.MemberTierId = 1;
                    }
                    context.SaveChanges();
                }

                // 1. Seed HallTypes khớp hoàn toàn với hàm NormalizeHallType dưới PricingService
                var hallTypesToSeed = new[] { "Phòng chiếu 2D", "VIP", "IMAX", "Phòng chiếu 3D" };
                bool anyHallTypeAdded = false;
                foreach (var typeName in hallTypesToSeed)
                {
                    if (!context.HallTypes.IgnoreQueryFilters().Any(t => t.TypeName == typeName))
                    {
                        context.HallTypes.Add(new HallType { TypeName = typeName });
                        anyHallTypeAdded = true;
                    }
                }
                if (anyHallTypeAdded)
                {
                    context.SaveChanges();
                }

                // Seed MovieFormats
                var formatsToSeed = new[]
                {
                    new MovieFormat { FormatName = "2D", Description = "Định dạng phim truyền thống phẳng tiêu chuẩn" },
                    new MovieFormat { FormatName = "3D", Description = "Định dạng phim nổi 3D kèm kính phân cực" },
                    new MovieFormat { FormatName = "IMAX", Description = "Định dạng màn hình cực đại âm thanh sống động" },
                };
                bool anyFormatAdded = false;
                foreach (var fmt in formatsToSeed)
                {
                    if (!context.MovieFormats.IgnoreQueryFilters().Any(f => f.FormatName == fmt.FormatName))
                    {
                        context.MovieFormats.Add(fmt);
                        anyFormatAdded = true;
                    }
                }

                // Seed Languages
                var langsToSeed = new[]
                {
                    new Language { LanguageName = "Tiếng Việt", Description = "Bản ngữ tiếng Việt chính thức" },
                    new Language { LanguageName = "English", Description = "Ngôn ngữ tiếng Anh quốc tế" },
                    new Language { LanguageName = "Korean", Description = "Tiếng Hàn Quốc nguyên bản" },
                    new Language { LanguageName = "Japanese", Description = "Tiếng Nhật Bản nguyên bản" },
                    new Language { LanguageName = "Chinese", Description = "Tiếng Trung Quốc phổ thông" }
                };
                bool anyLangAdded = false;
                foreach (var lng in langsToSeed)
                {
                    if (!context.Languages.IgnoreQueryFilters().Any(l => l.LanguageName == lng.LanguageName))
                    {
                        context.Languages.Add(lng);
                        anyLangAdded = true;
                    }
                }

                // Seed SubtitleTypes
                var subsToSeed = new[]
                {
                    new SubtitleType { SubtitleTypeName = "Vietsub", Description = "Phụ đề tiếng Việt hiển thị dưới màn hình" },
                    new SubtitleType { SubtitleTypeName = "Engsub", Description = "Phụ đề tiếng Anh dành cho khách quốc tế" },
                    new SubtitleType { SubtitleTypeName = "Lồng tiếng", Description = "Âm thanh nói tiếng Việt đè giọng diễn viên gốc" },
                    new SubtitleType { SubtitleTypeName = "Original", Description = "Phát nguyên bản không kèm phụ đề bổ sung" }
                };
                bool anySubAdded = false;
                foreach (var sb in subsToSeed)
                {
                    if (!context.SubtitleTypes.IgnoreQueryFilters().Any(s => s.SubtitleTypeName == sb.SubtitleTypeName))
                    {
                        context.SubtitleTypes.Add(sb);
                        anySubAdded = true;
                    }
                }

                // Seed AgeRatings
                var ratingsToSeed = new[]
                {
                    new AgeRating { RatingCode = "P", Description = "Mọi lứa tuổi - Thích hợp cho gia đình và trẻ em" },
                    new AgeRating { RatingCode = "K", Description = "Trẻ em dưới 13 tuổi được xem khi đi kèm người lớn" },
                    new AgeRating { RatingCode = "T13", Description = "Khán giả từ đủ 13 tuổi trở lên" },
                    new AgeRating { RatingCode = "T16", Description = "Khán giả từ đủ 16 tuổi trở lên" },
                    new AgeRating { RatingCode = "T18", Description = "Khán giả từ đủ 18 tuổi trở lên" },
                    new AgeRating { RatingCode = "C", Description = "Phim dành riêng cho khán giả người lớn (Cấm phổ biến)" }
                };
                bool anyRatingAdded = false;
                foreach (var rt in ratingsToSeed)
                {
                    if (!context.AgeRatings.IgnoreQueryFilters().Any(r => r.RatingCode == rt.RatingCode))
                    {
                        context.AgeRatings.Add(rt);
                        anyRatingAdded = true;
                    }
                }

                if (anyFormatAdded || anyLangAdded || anySubAdded || anyRatingAdded)
                {
                    context.SaveChanges();
                }

                var standardHallType = context.HallTypes.IgnoreQueryFilters().First(t => t.TypeName == "Phòng chiếu 2D");
                var vipHallType = context.HallTypes.IgnoreQueryFilters().First(t => t.TypeName == "VIP");
                var imaxHallType = context.HallTypes.IgnoreQueryFilters().First(t => t.TypeName == "IMAX");
                var room3DType = context.HallTypes.IgnoreQueryFilters().First(t => t.TypeName == "Phòng chiếu 3D");

                // // Seed Halls
                // if (!context.Halls.IgnoreQueryFilters().Any())
                // {
                //     context.Halls.AddRange(
                //         // Cinema 1 Halls
                //         new Hall { CinemaId = 1, HallName = "Phòng 01 (IMAX)", HallTypeId = imaxHallType.HallTypeId },
                //         new Hall { CinemaId = 1, HallName = "Phòng 02 (VIP)", HallTypeId = vipHallType.HallTypeId },
                //         new Hall { CinemaId = 1, HallName = "Phòng 03 (Thường 2D)", HallTypeId = standardHallType.HallTypeId },
                //         // Cinema 2 Halls
                //         new Hall { CinemaId = 2, HallName = "Phòng 01 (Thường 2D)", HallTypeId = standardHallType.HallTypeId },
                //         new Hall { CinemaId = 2, HallName = "Phòng 02 (3D Special)", HallTypeId = room3DType.HallTypeId }
                //     );
                //     context.SaveChanges();
                // }

                // Seed SeatTypes
                var seatTypesToSeed = new string[]
                {
                    "Standard",
                    "VIP",
                    "Sweetbox"
                };
                bool anySeatTypeAdded = false;
                foreach (var seatTypeName in seatTypesToSeed)
                {
                    if (!context.SeatTypes.IgnoreQueryFilters().Any(t => t.TypeName == seatTypeName))
                    {
                        context.SeatTypes.Add(new SeatType { TypeName = seatTypeName });
                        anySeatTypeAdded = true;
                    }
                }
                if (anySeatTypeAdded)
                {
                    context.SaveChanges();
                }

                var standardSeatType = context.SeatTypes.IgnoreQueryFilters().First(t => t.TypeName == "Standard");
                var vipSeatType = context.SeatTypes.IgnoreQueryFilters().First(t => t.TypeName == "VIP");
                var sweetboxSeatType = context.SeatTypes.IgnoreQueryFilters().First(t => t.TypeName == "Sweetbox");

                // Seed Seats (A to F, 1 to 6 khớp với sơ đồ ảnh đặt ghế của bạn)
                if (!context.Seats.IgnoreQueryFilters().Any())
                {
                    var halls = context.Halls.IgnoreQueryFilters().ToList();
                    var seats = new List<Seat>();
                    string[] rows = { "A", "B", "C", "D", "E", "F" };

                    foreach (var hall in halls)
                    {
                        foreach (var row in rows)
                        {
                            for (int col = 1; col <= 6; col++)
                            {
                                int typeId = standardSeatType.SeatTypeId;
                                if (row == "C" || row == "D") typeId = vipSeatType.SeatTypeId; // C, D cấu hình ghế VIP
                                else if (row == "F") typeId = sweetboxSeatType.SeatTypeId; // F cấu hình ghế đôi Sweetbox

                                seats.Add(new Seat
                                {
                                    HallId = hall.HallId,
                                    SeatCode = $"{row}{col}",
                                    SeatTypeId = typeId,
                                    IsDeleted = false,
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }
                    context.Seats.AddRange(seats);
                    context.SaveChanges();
                }

                // Seed Prices
                var pricesToSeed = new (decimal Value, string TicketType)[]
                {
                    (50000m, "2D"),
                    (65000m, "3D"),
                    (85000m, "IMAX")
                };
                bool anyPriceAdded = false;
                foreach (var price in pricesToSeed)
                {
                    if (!context.Prices.IgnoreQueryFilters().Any(p => p.TicketType == price.TicketType))
                    {
                        context.Prices.Add(new Price { Value = price.Value, TicketType = price.TicketType });
                        anyPriceAdded = true;
                    }
                }
                if (anyPriceAdded)
                {
                    context.SaveChanges();
                }

                // 2. Seed PricingRules chuẩn hóa hoàn toàn theo cấu trúc cộng dồn Additive Ma trận giá mới
                if (!context.PricingRules.IgnoreQueryFilters().Any())
                {
                    context.PricingRules.AddRange(new List<PricingRule>
                    {
                        // Giá vé nền cố định cơ sở (Base price)
                        new PricingRule { SeatType = "ALL", HallType = "ALL", DayType = "WEEKDAY", TimeSlot = "ALL", BasePrice = 50000m, Status = "ACTIVE", Priority = 1 },
                        new PricingRule { SeatType = "ALL", HallType = "ALL", DayType = "WEEKEND", TimeSlot = "ALL", BasePrice = 70000m, Status = "ACTIVE", Priority = 1 },
                        
                        // Luật cộng dồn theo khung giờ (Suất chiếu muộn sau 22h đêm)
                        new PricingRule { SeatType = "ALL", HallType = "ALL", DayType = "ALL", TimeSlot = "NIGHT", BasePrice = 45000m, Status = "ACTIVE", Priority = 2 },
                        
                        // Luật cộng dồn Phụ thu theo loại ghế (Surcharge)
                        new PricingRule { SeatType = "VIP", HallType = "ALL", DayType = "ALL", TimeSlot = "ALL", BasePrice = 20000m, Status = "ACTIVE", Priority = 1 },
                        new PricingRule { SeatType = "GHẾ ĐÔI", HallType = "ALL", DayType = "ALL", TimeSlot = "ALL", BasePrice = 40000m, Status = "ACTIVE", Priority = 1 },
                        
                        // Luật cộng dồn Phụ thu theo loại sảnh/phòng chiếu (Surcharge)
                        new PricingRule { SeatType = "ALL", HallType = "PHÒNG CHIẾU 3D", DayType = "ALL", TimeSlot = "ALL", BasePrice = 15000m, Status = "ACTIVE", Priority = 1 },
                        new PricingRule { SeatType = "ALL", HallType = "VIP", DayType = "ALL", TimeSlot = "ALL", BasePrice = 30000m, Status = "ACTIVE", Priority = 1 },
                        new PricingRule { SeatType = "ALL", HallType = "IMAX", DayType = "ALL", TimeSlot = "ALL", BasePrice = 35000m, Status = "ACTIVE", Priority = 1 }
                    });
                    context.SaveChanges();
                }

                var standardPrice = context.Prices.IgnoreQueryFilters().First(p => p.TicketType == "2D");
                var format2D = context.MovieFormats.IgnoreQueryFilters().First(f => f.FormatName == "2D");
                var format3D = context.MovieFormats.IgnoreQueryFilters().First(f => f.FormatName == "3D");
                var formatIMAX = context.MovieFormats.IgnoreQueryFilters().First(f => f.FormatName == "IMAX");

                // Seed Movies
                if (!context.Movies.IgnoreQueryFilters().Any())
                {
                    var movie1 = new Movie
                    {
                        Title = "Doctor Strange in the Multiverse of Madness",
                        Slug = "doctor-strange-multiverse-madness",
                        Description = "Doctor Strange teams up with a mysterious teenage girl from his dreams who can travel across multiverses...",
                        Duration = 126,
                        Language = "English",
                        PosterUrl = "https://images.unsplash.com/photo-1594909122845-11baa439b7bf?q=80&w=600",
                        BannerUrl = "https://images.unsplash.com/photo-1536440136628-849c177e76a1?q=80&w=1200",
                        TrailerUrl = "https://www.youtube.com/embed/aWzlQ2N6qqg",
                        ReleaseDate = DateTime.UtcNow.AddDays(-10),
                        EndDate = DateTime.UtcNow.AddDays(20),
                        Rating = 8.5,
                        GenreId = 1,
                        AgeRatingId = 1
                    };

                    var movie2 = new Movie
                    {
                        Title = "Top Gun: Maverick",
                        Slug = "top-gun-maverick",
                        Description = "After thirty years, Maverick is still pushing the envelope as a top naval aviator...",
                        Duration = 130,
                        Language = "English",
                        PosterUrl = "https://images.unsplash.com/photo-1436491865332-7a61a109cc05?q=80&w=600",
                        BannerUrl = "https://images.unsplash.com/photo-1518364538800-6bcb3f25da49?q=80&w=1200",
                        TrailerUrl = "https://www.youtube.com/embed/giXcoYnT0yY",
                        ReleaseDate = DateTime.UtcNow.AddDays(-15),
                        EndDate = DateTime.UtcNow.AddDays(15),
                        Rating = 8.8,
                        GenreId = 3,
                        AgeRatingId = 2
                    };

                    context.Movies.AddRange(movie1, movie2);
                    context.SaveChanges();

                    var format2DId = format2D.MovieFormatId;
                    var format3DId = format3D.MovieFormatId;
                    var formatIMAXId = formatIMAX.MovieFormatId;

                    context.Database.ExecuteSqlRaw($"INSERT INTO MovieMovieFormats (MovieId, MovieFormatId) VALUES ({movie1.Id}, {formatIMAXId})");
                    context.Database.ExecuteSqlRaw($"INSERT INTO MovieMovieFormats (MovieId, MovieFormatId) VALUES ({movie1.Id}, {format2DId})");
                    context.Database.ExecuteSqlRaw($"INSERT INTO MovieMovieFormats (MovieId, MovieFormatId) VALUES ({movie2.Id}, {format2DId})");
                }

                var movie1Data = context.Movies.IgnoreQueryFilters().First(m => m.Slug == "doctor-strange-multiverse-madness");
                var movie2Data = context.Movies.IgnoreQueryFilters().First(m => m.Slug == "top-gun-maverick");

                // Seed Dynamic Showtimes
                if (!context.Showtimes.IgnoreQueryFilters().Any())
                {
                    var halls = context.Halls.IgnoreQueryFilters().ToList();
                    if (halls.Count >= 3)
                    {
                        var today = DateTime.UtcNow.Date;
                        var showtimes = new List<Showtime>
                        {
                            new Showtime { MovieId = movie1Data.Id, HallId = halls[0].HallId, PriceId = standardPrice.PriceId, StartTime = today.AddHours(13).AddMinutes(45), EndTime = today.AddHours(15).AddMinutes(55) },
                            new Showtime { MovieId = movie2Data.Id, HallId = halls[1].HallId, PriceId = standardPrice.PriceId, StartTime = today.AddHours(13).AddMinutes(45), EndTime = today.AddHours(15).AddMinutes(55) }
                        };

                        context.Showtimes.AddRange(showtimes);
                        context.SaveChanges();
                    }
                }

                // Seed Users
                if (!context.Users.IgnoreQueryFilters().Any())
                {
                    var adminUser = new User
                    {
                        Email = "admin@gmail.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                        FullName = "Quản Trị Viên",
                        PhoneNumber = "0987654321",
                        Gender = "Male",
                        DateOfBirth = new DateTime(1990, 1, 1),
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        IsEmailVerified = true
                    };
                    adminUser.UserRoles.Add(new UserRole { RoleId = 1 });

                    var regularUser = new User
                    {
                        Email = "user@gmail.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                        FullName = "Nguyễn Văn A",
                        PhoneNumber = "0912345678",
                        Gender = "Male",
                        DateOfBirth = new DateTime(1995, 5, 15),
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        IsEmailVerified = true
                    };
                    regularUser.UserRoles.Add(new UserRole { RoleId = 2 });

                    context.Users.AddRange(adminUser, regularUser);
                    context.SaveChanges();
                }

                // Seed Products
                if (!context.Products.IgnoreQueryFilters().Any())
                {
                    var p1 = new Product { Name = "Bắp rang ngọt 69oz", Description = "Bắp rang bơ vị ngọt thơm ngon cỡ lớn", Price = 50000m, IsActive = true };
                    var p2 = new Product { Name = "Bắp rang phô mai 69oz", Description = "Bắp rang bơ vị phô mai mặn cỡ lớn", Price = 60000m, IsActive = true };
                    var p3 = new Product { Name = "Nước Pepsi 22oz", Description = "Nước ngọt Pepsi mát lạnh cỡ vừa", Price = 25000m, IsActive = true };
                    var p4 = new Product { Name = "Nước Coca-Cola 22oz", Description = "Nước ngọt Coca-Cola mát lạnh cỡ vừa", Price = 25000m, IsActive = true };
                    var p5 = new Product { Name = "Ly nước Minion đặc biệt", Description = "Ly nhựa hình Minion ngộ nghĩnh giữ nhiệt", Price = 120000m, IsActive = true };
                    var p6 = new Product { Name = "Snack khoai tây Oishi", Description = "Snack khoai tây giòn rụm", Price = 20000m, IsActive = true };

                    context.Products.AddRange(p1, p2, p3, p4, p5, p6);
                    context.SaveChanges();
                }

                // Seed Combos
                if (!context.Combos.IgnoreQueryFilters().Any())
                {
                    var pSweetPopcorn = context.Products.IgnoreQueryFilters().First(p => p.Name == "Bắp rang ngọt 69oz");
                    var pPepsi = context.Products.IgnoreQueryFilters().First(p => p.Name == "Nước Pepsi 22oz");
                    var pMinionCup = context.Products.IgnoreQueryFilters().First(p => p.Name == "Ly nước Minion đặc biệt");

                    var comboBeta = new Combo
                    {
                        Name = "Beta Combo",
                        Description = "1 Bắp ngọt 69oz + 1 Pepsi 22oz. Tiết kiệm hơn khi mua lẻ.",
                        Price = 65000m,
                        OriginalPrice = 75000m,
                        DiscountBadge = "Tiết kiệm 13%",
                        ImageUrl = "https://images.unsplash.com/photo-1578849278619-e73505e9610f?q=80&w=400",
                        IsActive = true,
                        DisplayOrder = 1
                    };
                    comboBeta.ComboItems.Add(new ComboItem { ProductId = pSweetPopcorn.Id, Quantity = 1 });
                    comboBeta.ComboItems.Add(new ComboItem { ProductId = pPepsi.Id, Quantity = 1 });

                    var comboCouple = new Combo
                    {
                        Name = "Couple Combo",
                        Description = "1 Bắp ngọt 69oz + 2 Pepsi 22oz. Thích hợp cho cặp đôi.",
                        Price = 89000m,
                        OriginalPrice = 100000m,
                        DiscountBadge = "Tiết kiệm 11%",
                        ImageUrl = "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?q=80&w=400",
                        IsActive = true,
                        DisplayOrder = 2
                    };
                    comboCouple.ComboItems.Add(new ComboItem { ProductId = pSweetPopcorn.Id, Quantity = 1 });
                    comboCouple.ComboItems.Add(new ComboItem { ProductId = pPepsi.Id, Quantity = 2 });

                    var comboFamily = new Combo
                    {
                        Name = "Family Combo",
                        Description = "2 Bắp ngọt 69oz + 3 Pepsi 22oz. Thích hợp cho nhóm bạn hoặc gia đình.",
                        Price = 139000m,
                        OriginalPrice = 160000m,
                        DiscountBadge = "Tiết kiệm 15%",
                        ImageUrl = "https://images.unsplash.com/photo-1513104890138-7c749659a591?q=80&w=400",
                        IsActive = true,
                        DisplayOrder = 3
                    };
                    comboFamily.ComboItems.Add(new ComboItem { ProductId = pSweetPopcorn.Id, Quantity = 2 });
                    comboFamily.ComboItems.Add(new ComboItem { ProductId = pPepsi.Id, Quantity = 3 });

                    var comboMinion = new Combo
                    {
                        Name = "Minion Special Combo",
                        Description = "1 Ly nước Minion đặc biệt + 1 Bắp ngọt 69oz + 1 Pepsi 22oz. Số lượng giới hạn!",
                        Price = 179000m,
                        OriginalPrice = 195000m,
                        DiscountBadge = "Limited Edition",
                        ImageUrl = "https://images.unsplash.com/photo-1593085512500-5d55148d6f0d?q=80&w=400",
                        IsActive = true,
                        DisplayOrder = 4
                    };
                    comboMinion.ComboItems.Add(new ComboItem { ProductId = pMinionCup.Id, Quantity = 1 });
                    comboMinion.ComboItems.Add(new ComboItem { ProductId = pSweetPopcorn.Id, Quantity = 1 });
                    comboMinion.ComboItems.Add(new ComboItem { ProductId = pPepsi.Id, Quantity = 1 });

                    context.Combos.AddRange(comboBeta, comboCouple, comboFamily, comboMinion);
                    context.SaveChanges();
                }

                // Seed Promotions
                if (!context.Promotions.IgnoreQueryFilters().Any())
                {
                    context.Promotions.AddRange(
                        new Promotion
                        {
                            PromoCode = "GIAMGIA10",
                            Name = "Giảm giá 10%",
                            Description = "Chương trình giảm giá 10% cho toàn bộ vé",
                            DiscountValue = 10m, // Percentage is 10%
                            DiscountType = PromotionType.Percentage,
                            StartDate = DateTime.UtcNow.AddDays(-10),
                            EndDate = DateTime.UtcNow.AddDays(30),
                            MaxUsage = 100,
                            CurrentUsage = 0,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Promotion
                        {
                            PromoCode = "KM50K",
                            Name = "Khuyến mãi 50K",
                            Description = "Giảm ngay 50.000đ cho hóa đơn đạt yêu cầu",
                            DiscountValue = 50000m,
                            DiscountType = PromotionType.FixedAmount,
                            StartDate = DateTime.UtcNow.AddDays(-10),
                            EndDate = DateTime.UtcNow.AddDays(30),
                            MaxUsage = 50,
                            CurrentUsage = 0,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        }
                    );
                    context.SaveChanges();
                }

                // Seed NewsItems
                if (!context.NewsItems.IgnoreQueryFilters().Any())
                {
                    context.NewsItems.AddRange(
                        new Models.News.NewsItem
                        {
                            Title = "Bom Tấn Doctor Strange Phá Đảo Doanh Thu Phòng Vé Toàn Cầu",
                            ShortDesc = "Được đánh giá là siêu phẩm Marvel xuất sắc nhất năm, phần mới của Phù Thủy Tối Thượng liên tục xô đổ các kỷ lục doanh thu phòng vé.",
                            Content = "<h1>Doctor Strange phá đảo phòng vé</h1><p>Doctor Strange in the Multiverse of Madness đã tạo nên một cơn sốt phòng vé cực kỳ lớn trên toàn cầu...</p>",
                            Image = "https://images.unsplash.com/photo-1509281373149-e957c6296406?q=80&w=600",
                            Category = "news",
                            CategoryLabel = "Tin điện ảnh",
                            Slug = "doctor-strange-pha-dao-doanh-thu",
                            Trending = true,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow.AddDays(-1)
                        },
                        new Models.News.NewsItem
                        {
                            Title = "Top 5 Bộ Phim Chiếu Rạp Đáng Xem Nhất Trong Mùa Hè Này",
                            ShortDesc = "Điểm danh những cái tên đình đám sắp sửa đổ bộ phòng vé CinemaPass từ hoạt hình vui nhộn đến hành động giả tưởng kịch tính.",
                            Content = "<h1>Những bộ phim đáng xem nhất hè 2026</h1><p>Mùa hè năm nay hứa hẹn sẽ mang đến hàng loạt trải nghiệm điện ảnh đa dạng...</p>",
                            Image = "https://images.unsplash.com/photo-1440404653325-ab127d49abc1?q=80&w=600",
                            Category = "news",
                            CategoryLabel = "Tin điện ảnh",
                            Slug = "top-5-phim-rap-dang-xem-mua-he",
                            Trending = false,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow.AddDays(-2)
                        },
                        new Models.News.NewsItem
                        {
                            Title = "Lễ Hội Anime Nhật Bản - Suất Chiếu Đặc Biệt & Giao Lưu Cosplay",
                            ShortDesc = "Sự kiện điện ảnh lớn nhất dành cho các fan Anime với suất chiếu sớm các bom tấn chưa từng công bố và buổi giao lưu, tặng quà lưu niệm vô cùng độc đáo.",
                            Content = "<h1>Lễ hội Anime hoành tráng</h1><p>Đến với CinemaPass để tham gia ngày hội Anime Nhật Bản lớn nhất năm...</p>",
                            Image = "https://images.unsplash.com/photo-1578632767115-351597cf2477?q=80&w=1200",
                            Category = "events",
                            CategoryLabel = "Sự kiện",
                            Slug = "le-hoi-anime-nhat-ban-cosplay",
                            Trending = true,
                            Tag = "FESTIVAL",
                            EventDate = "05/06/2026 - 08/06/2026",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow.AddDays(-3)
                        },
                        new Models.News.NewsItem
                        {
                            Title = "Đêm Chiếu Phim Kinh Dị Giữa Đêm Khuya - Thách Thức Lòng Dũng Cảm",
                            ShortDesc = "Trải nghiệm rùng rợn độc quyền tại phòng chiếu Dolby Atmos với combo phim kinh dị bất ngờ chiếu liên tục từ 23h00 đến sáng.",
                            Content = "<h1>Thách thức lòng dũng cảm</h1><p>Bạn có dám ở lại rạp chiếu phim sau 23h để xem những tác phẩm kinh dị kinh điển...</p>",
                            Image = "https://images.unsplash.com/photo-1505686994434-e3cc5abf1330?q=80&w=1200",
                            Category = "events",
                            CategoryLabel = "Sự kiện",
                            Slug = "dem-chieu-phim-kinh-di-nua-dem",
                            Trending = false,
                            Tag = "SPECIAL NIGHT",
                            EventDate = "31/10/2026",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow.AddDays(-4)
                        }
                    );
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database seeding failed: {ex.Message}. Inner Exception: {ex.InnerException?.Message}", ex);
            }
        }

        public static void ResetAndSeed(CinemaDbContext context)
        {
            context.BookingSeats.RemoveRange(context.BookingSeats.IgnoreQueryFilters());
            context.BookingPromotions.RemoveRange(context.BookingPromotions.IgnoreQueryFilters());
            context.Bookings.RemoveRange(context.Bookings.IgnoreQueryFilters());
            context.Payments.RemoveRange(context.Payments.IgnoreQueryFilters());
            context.PaymentTransactions.RemoveRange(context.PaymentTransactions.IgnoreQueryFilters());
            context.Refunds.RemoveRange(context.Refunds.IgnoreQueryFilters());
            context.Reviews.RemoveRange(context.Reviews.IgnoreQueryFilters());
            context.Notifications.RemoveRange(context.Notifications.IgnoreQueryFilters());
            context.RefreshTokens.RemoveRange(context.RefreshTokens.IgnoreQueryFilters());
            context.UserRoles.RemoveRange(context.UserRoles.IgnoreQueryFilters());
            context.Users.RemoveRange(context.Users.IgnoreQueryFilters());
            context.Showtimes.RemoveRange(context.Showtimes.IgnoreQueryFilters());
            context.Movies.RemoveRange(context.Movies.IgnoreQueryFilters());
            context.Seats.RemoveRange(context.Seats.IgnoreQueryFilters());
            context.Halls.RemoveRange(context.Halls.IgnoreQueryFilters());
            context.HallTypes.RemoveRange(context.HallTypes.IgnoreQueryFilters());
            context.SeatTypes.RemoveRange(context.SeatTypes.IgnoreQueryFilters());
            context.Prices.RemoveRange(context.Prices.IgnoreQueryFilters());
            context.PricingRules.RemoveRange(context.PricingRules.IgnoreQueryFilters());
            context.Promotions.RemoveRange(context.Promotions.IgnoreQueryFilters());
            context.MovieFormats.RemoveRange(context.MovieFormats.IgnoreQueryFilters());
            context.Languages.RemoveRange(context.Languages.IgnoreQueryFilters());
            context.SubtitleTypes.RemoveRange(context.SubtitleTypes.IgnoreQueryFilters());
            context.AgeRatings.RemoveRange(context.AgeRatings.IgnoreQueryFilters());
            context.NewsItems.RemoveRange(context.NewsItems.IgnoreQueryFilters());
            context.OrderCombos.RemoveRange(context.OrderCombos.IgnoreQueryFilters());
            context.ComboItems.RemoveRange(context.ComboItems.IgnoreQueryFilters());
            context.Combos.RemoveRange(context.Combos.IgnoreQueryFilters());
            context.Products.RemoveRange(context.Products.IgnoreQueryFilters());
            context.SaveChanges();

            Initialize(context);
        }
    }
}