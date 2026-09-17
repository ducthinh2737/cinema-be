using System;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.Models.Cinemas;
using CinemaBooking.API.Domain.Exceptions;

namespace CinemaBooking.API.Domain.Policies
{
    public class ShowtimeSchedulingPolicy
    {
        public const int CleanUpBufferMinutes = 15;
        public const int SneakPreviewMaxDaysBeforeRelease = 3;
        
        public static readonly TimeZoneInfo LocalTimeZone = GetLocalTimeZone();

        private static TimeZoneInfo GetLocalTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone("Vietnam_Standard_Time", TimeSpan.FromHours(7), "Vietnam Standard Time", "Vietnam Standard Time");
                }
            }
        }

        public static void ValidateReleaseWindow(Movie movie, DateTime startTimeUtc)
        {
            var startTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(startTimeUtc, LocalTimeZone);
            var earliestAllowedDate = movie.ReleaseDate.Date.AddDays(-SneakPreviewMaxDaysBeforeRelease);
            
            if (startTimeLocal.Date < earliestAllowedDate)
            {
                throw new ValidationException($"Không thể tạo suất chiếu quá sớm. Phim khởi chiếu ngày {movie.ReleaseDate:dd/MM/yyyy}. Suất chiếu sớm (Sneak Preview) chỉ được phép từ ngày {earliestAllowedDate:dd/MM/yyyy}.");
            }

            if (movie.EndDate != default && movie.EndDate > movie.ReleaseDate)
            {
                var latestAllowedDate = movie.EndDate.Date.AddDays(1);
                if (startTimeLocal.Date >= latestAllowedDate)
                {
                    throw new ValidationException($"Không thể tạo suất chiếu sau khi phim đã ngừng chiếu. Phim kết thúc chiếu vào ngày {movie.EndDate:dd/MM/yyyy}.");
                }
            }
        }

        public static void ValidateOperatingHours(DateTime startTimeUtc, DateTime endTimeUtc, Cinema cinema)
        {
            var startLocal = TimeZoneInfo.ConvertTimeFromUtc(startTimeUtc, LocalTimeZone);

            // Parse opening time. Default to 08:00 if not set.
            var openingTime = TimeSpan.FromHours(8);

            if (!string.IsNullOrEmpty(cinema.OpeningTime) && TimeSpan.TryParse(cinema.OpeningTime, out var openTs))
            {
                openingTime = openTs;
            }

            var startOfDay = startLocal.Date.Add(openingTime);

            if (startLocal < startOfDay)
            {
                var prevDayStart = startLocal.Date.AddDays(-1).Add(openingTime);
                if (startLocal < prevDayStart)
                {
                    throw new ValidationException($"Thời gian bắt đầu chiếu ({startLocal:HH:mm}) không được trước giờ mở cửa của rạp ({cinema.OpeningTime ?? "08:00"}).");
                }
            }
        }

        public static DateTime CalculateEndTime(Movie movie, DateTime startTimeUtc)
        {
            return startTimeUtc.AddMinutes(movie.Duration);
        }
    }
}
