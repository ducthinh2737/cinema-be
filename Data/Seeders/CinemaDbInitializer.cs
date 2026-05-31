using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Models.Cinemas;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.Models.Users;
using CinemaBooking.API.Models.Promotions;

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

                // Seed HallTypes individually to prevent partial seed crashes
                var hallTypesToSeed = new[] { "Standard", "VIP", "IMAX" };
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
                    new MovieFormat { FormatName = "4DX", Description = "Định dạng phòng chiếu ghế chuyển động và hiệu ứng môi trường" },
                    new MovieFormat { FormatName = "ScreenX", Description = "Màn hình chiếu 270 độ góc nhìn siêu rộng" }
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

                var standardHallType = context.HallTypes.IgnoreQueryFilters().First(t => t.TypeName == "Standard");
                var vipHallType = context.HallTypes.IgnoreQueryFilters().First(t => t.TypeName == "VIP");
                var imaxHallType = context.HallTypes.IgnoreQueryFilters().First(t => t.TypeName == "IMAX");

                // Seed Halls
                if (!context.Halls.IgnoreQueryFilters().Any())
                {
                    context.Halls.AddRange(
                        // Cinema 1 Halls
                        new Hall { CinemaId = 1, HallName = "IMAX Theater 1", HallTypeId = imaxHallType.HallTypeId },
                        new Hall { CinemaId = 1, HallName = "Standard Hall 2", HallTypeId = standardHallType.HallTypeId },
                        new Hall { CinemaId = 1, HallName = "VIP Lounge 3", HallTypeId = vipHallType.HallTypeId },
                        // Cinema 2 Halls
                        new Hall { CinemaId = 2, HallName = "Standard Hall 1", HallTypeId = standardHallType.HallTypeId },
                        new Hall { CinemaId = 2, HallName = "VIP Lounge 2", HallTypeId = vipHallType.HallTypeId }
                    );
                    context.SaveChanges();
                }

                // Seed SeatTypes individually
                var seatTypesToSeed = new (string Name, decimal Multiplier)[]
                {
                    ("Standard", 1.0m),
                    ("VIP", 1.2m),
                    ("Sweetbox", 1.5m)
                };
                bool anySeatTypeAdded = false;
                foreach (var seatType in seatTypesToSeed)
                {
                    if (!context.SeatTypes.IgnoreQueryFilters().Any(t => t.TypeName == seatType.Name))
                    {
                        context.SeatTypes.Add(new SeatType { TypeName = seatType.Name, PriceMultiplier = seatType.Multiplier });
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

                // Seed Seats (A to E, 1 to 10 for each hall)
                if (!context.Seats.IgnoreQueryFilters().Any())
                {
                    var halls = context.Halls.IgnoreQueryFilters().ToList();
                    var seats = new List<Seat>();
                    string[] rows = { "A", "B", "C", "D", "E" };

                    foreach (var hall in halls)
                    {
                        foreach (var row in rows)
                        {
                            for (int col = 1; col <= 10; col++)
                            {
                                int typeId = standardSeatType.SeatTypeId; // Standard by default
                                if (row == "D") typeId = vipSeatType.SeatTypeId; // VIP
                                else if (row == "E") typeId = sweetboxSeatType.SeatTypeId; // Sweetbox

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

                // Seed Prices individually
                var pricesToSeed = new (decimal Value, string TicketType)[]
                {
                    (80000m, "Standard Weekday"),
                    (100000m, "VIP Weekday"),
                    (120000m, "Blockbuster Weekend")
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

                var standardPrice = context.Prices.IgnoreQueryFilters().First(p => p.TicketType == "Standard Weekday");
                var vipPrice = context.Prices.IgnoreQueryFilters().First(p => p.TicketType == "VIP Weekday");
                var weekendPrice = context.Prices.IgnoreQueryFilters().First(p => p.TicketType == "Blockbuster Weekend");

                // Seed Movies (with high-res Unsplash links and embeddable trailers)
                if (!context.Movies.IgnoreQueryFilters().Any())
                {
                    context.Movies.AddRange(
                        new Movie
                        {
                            Title = "Doctor Strange in the Multiverse of Madness",
                            Slug = "doctor-strange-multiverse-madness",
                            Description = "Doctor Strange teams up with a mysterious teenage girl from his dreams who can travel across multiverses, to battle multiple threats, including other-universe versions of himself, which threaten to wipe out millions in the multiverse.",
                            Duration = 126,
                            Language = "English",
                            PosterUrl = "https://images.unsplash.com/photo-1594909122845-11baa439b7bf?q=80&w=600",
                            BannerUrl = "https://images.unsplash.com/photo-1536440136628-849c177e76a1?q=80&w=1200",
                            TrailerUrl = "https://www.youtube.com/embed/aWzlQ2N6qqg",
                            ReleaseDate = DateTime.UtcNow.AddDays(-10),
                            EndDate = DateTime.UtcNow.AddDays(20),
                            Rating = 8.5,
                            GenreId = 1, // Action
                            AgeRatingId = 1
                        },
                        new Movie
                        {
                            Title = "Spider-Man: No Way Home",
                            Slug = "spider-man-no-way-home",
                            Description = "With Spider-Man's identity now revealed, Peter asks Doctor Strange for help. When a spell goes wrong, dangerous foes from other worlds start to appear, forcing Peter to discover what it truly means to be Spider-Man.",
                            Duration = 148,
                            Language = "English",
                            PosterUrl = "https://images.unsplash.com/photo-1635805737707-575885ab0820?q=80&w=600",
                            BannerUrl = "https://images.unsplash.com/photo-1509347528160-9a9e33742cdb?q=80&w=1200",
                            TrailerUrl = "https://www.youtube.com/embed/JfVOs4VSpmA",
                            ReleaseDate = DateTime.UtcNow.AddDays(-5),
                            EndDate = DateTime.UtcNow.AddDays(25),
                            Rating = 9.0,
                            GenreId = 1, // Action
                            AgeRatingId = 1
                        },
                        new Movie
                        {
                            Title = "Top Gun: Maverick",
                            Slug = "top-gun-maverick",
                            Description = "After thirty years, Maverick is still pushing the envelope as a top naval aviator, but must confront ghosts of his past when he leads TOP GUN's elite graduates on a mission that demands the ultimate sacrifice from those chosen to fly it.",
                            Duration = 130,
                            Language = "English",
                            PosterUrl = "https://images.unsplash.com/photo-1436491865332-7a61a109cc05?q=80&w=600",
                            BannerUrl = "https://images.unsplash.com/photo-1518364538800-6bcb3f25da49?q=80&w=1200",
                            TrailerUrl = "https://www.youtube.com/embed/giXcoYnT0yY",
                            ReleaseDate = DateTime.UtcNow.AddDays(-15),
                            EndDate = DateTime.UtcNow.AddDays(15),
                            Rating = 8.8,
                            GenreId = 3, // Drama
                            AgeRatingId = 2
                        },
                        new Movie
                        {
                            Title = "The Batman",
                            Slug = "the-batman",
                            Description = "Batman ventures into Gotham City's underworld when a sadistic killer leaves behind a trail of cryptic clues. As the evidence begins to lead closer to home and the scale of the perpetrator's plans becomes clear, he must forge new relationships, unmask the culprit, and bring justice to the abuse of power and corruption that has long plagued the metropolis.",
                            Duration = 176,
                            Language = "English",
                            PosterUrl = "https://images.unsplash.com/photo-1534447677768-be436bb09401?q=80&w=600",
                            BannerUrl = "https://images.unsplash.com/photo-1509281373149-e957c6296406?q=80&w=1200",
                            TrailerUrl = "https://www.youtube.com/embed/mqqft2x_Aa4",
                            ReleaseDate = DateTime.UtcNow.AddDays(-20),
                            EndDate = DateTime.UtcNow.AddDays(10),
                            Rating = 8.3,
                            GenreId = 1, // Action
                            AgeRatingId = 2
                        },
                        new Movie
                        {
                            Title = "Avatar: The Way of Water",
                            Slug = "avatar-way-of-water",
                            Description = "Jake Sully lives with his newfound family formed on the extrasolar moon Pandora. Once a familiar threat returns to finish what was previously started, Jake must work with Neytiri and the army of the Na'vi race to protect their home.",
                            Duration = 192,
                            Language = "English",
                            PosterUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?q=80&w=600",
                            BannerUrl = "https://images.unsplash.com/photo-1478760329108-5c3ed9d495a0?q=80&w=1200",
                            TrailerUrl = "https://www.youtube.com/embed/d9MyW72ELq0",
                            ReleaseDate = DateTime.UtcNow.AddDays(5), // Upcoming
                            EndDate = DateTime.UtcNow.AddDays(35),
                            Rating = 8.0,
                            GenreId = 1, // Action
                            AgeRatingId = 1
                        }
                    );
                    context.SaveChanges();
                }

                var movie1 = context.Movies.IgnoreQueryFilters().First(m => m.Slug == "doctor-strange-multiverse-madness");
                var movie2 = context.Movies.IgnoreQueryFilters().First(m => m.Slug == "spider-man-no-way-home");
                var movie3 = context.Movies.IgnoreQueryFilters().First(m => m.Slug == "top-gun-maverick");
                var movie4 = context.Movies.IgnoreQueryFilters().First(m => m.Slug == "the-batman");
                var movie5 = context.Movies.IgnoreQueryFilters().First(m => m.Slug == "avatar-way-of-water");

                // Seed Dynamic Showtimes (always current)
                if (!context.Showtimes.IgnoreQueryFilters().Any())
                {
                    var halls = context.Halls.IgnoreQueryFilters().ToList();
                    if (halls.Count >= 5)
                    {
                        var today = DateTime.UtcNow.Date;
                        var showtimes = new List<Showtime>
                        {
                            // Doctor Strange (Movie 1)
                            new Showtime { MovieId = movie1.Id, HallId = halls[0].HallId, PriceId = weekendPrice.PriceId, StartTime = today.AddHours(10), EndTime = today.AddHours(12).AddMinutes(6) },
                            new Showtime { MovieId = movie1.Id, HallId = halls[1].HallId, PriceId = standardPrice.PriceId, StartTime = today.AddHours(14), EndTime = today.AddHours(16).AddMinutes(6) },
                            new Showtime { MovieId = movie1.Id, HallId = halls[0].HallId, PriceId = weekendPrice.PriceId, StartTime = today.AddHours(19), EndTime = today.AddHours(21).AddMinutes(6) },

                            // Spider-Man (Movie 2)
                            new Showtime { MovieId = movie2.Id, HallId = halls[0].HallId, PriceId = weekendPrice.PriceId, StartTime = today.AddHours(13), EndTime = today.AddHours(15).AddMinutes(28) },
                            new Showtime { MovieId = movie2.Id, HallId = halls[2].HallId, PriceId = vipPrice.PriceId, StartTime = today.AddHours(17), EndTime = today.AddHours(19).AddMinutes(28) },
                            new Showtime { MovieId = movie2.Id, HallId = halls[0].HallId, PriceId = weekendPrice.PriceId, StartTime = today.AddHours(22), EndTime = today.AddHours(24).AddMinutes(28) },

                            // Top Gun (Movie 3)
                            new Showtime { MovieId = movie3.Id, HallId = halls[1].HallId, PriceId = standardPrice.PriceId, StartTime = today.AddHours(11), EndTime = today.AddHours(13).AddMinutes(10) },
                            new Showtime { MovieId = movie3.Id, HallId = halls[3].HallId, PriceId = standardPrice.PriceId, StartTime = today.AddHours(16), EndTime = today.AddHours(18).AddMinutes(10) },

                            // The Batman (Movie 4)
                            new Showtime { MovieId = movie4.Id, HallId = halls[2].HallId, PriceId = vipPrice.PriceId, StartTime = today.AddHours(12), EndTime = today.AddHours(14).AddMinutes(56) },
                            new Showtime { MovieId = movie4.Id, HallId = halls[4].HallId, PriceId = vipPrice.PriceId, StartTime = today.AddHours(20), EndTime = today.AddHours(22).AddMinutes(56) },

                            // Avatar (Movie 5 - Special Screening)
                            new Showtime { MovieId = movie5.Id, HallId = halls[1].HallId, PriceId = weekendPrice.PriceId, StartTime = today.AddHours(20), EndTime = today.AddHours(23).AddMinutes(12) },

                            // Tomorrow's showtimes for all movies
                            new Showtime { MovieId = movie1.Id, HallId = halls[0].HallId, PriceId = weekendPrice.PriceId, StartTime = today.AddDays(1).AddHours(10), EndTime = today.AddDays(1).AddHours(12).AddMinutes(6) },
                            new Showtime { MovieId = movie2.Id, HallId = halls[0].HallId, PriceId = weekendPrice.PriceId, StartTime = today.AddDays(1).AddHours(13), EndTime = today.AddDays(1).AddHours(15).AddMinutes(28) },
                            new Showtime { MovieId = movie3.Id, HallId = halls[1].HallId, PriceId = standardPrice.PriceId, StartTime = today.AddDays(1).AddHours(11), EndTime = today.AddDays(1).AddHours(13).AddMinutes(10) }
                        };

                        context.Showtimes.AddRange(showtimes);
                        context.SaveChanges();
                    }
                }

                // Seed Users (Admin & regular User)
                if (!context.Users.IgnoreQueryFilters().Any())
                {
                    var adminUser = new User
                    {
                        Email = "admin@gmail.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
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
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
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

                // Seed Promotions
                if (!context.Promotions.IgnoreQueryFilters().Any())
                {
                    context.Promotions.AddRange(
                        new Promotion
                        {
                            PromoCode = "GIAMGIA10",
                            DiscountValue = 10m,
                            DiscountType = "Percentage",
                            StartDate = DateTime.UtcNow.AddDays(-10),
                            EndDate = DateTime.UtcNow.AddDays(30),
                            MaxUsage = 100,
                            CurrentUsage = 0,
                            IsActive = true
                        },
                        new Promotion
                        {
                            PromoCode = "KM50K",
                            DiscountValue = 50000m,
                            DiscountType = "FixedAmount",
                            StartDate = DateTime.UtcNow.AddDays(-10),
                            EndDate = DateTime.UtcNow.AddDays(30),
                            MaxUsage = 50,
                            CurrentUsage = 0,
                            IsActive = true
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
            // Delete in correct order to avoid foreign key violations
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
            context.Promotions.RemoveRange(context.Promotions.IgnoreQueryFilters());
            context.SaveChanges();

            // Re-seed everything
            Initialize(context);
        }
    }
}
