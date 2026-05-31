using System;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.Data;
using CinemaBooking.API.Data.Seeders;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public TestController(CinemaDbContext context)
        {
            _context = context;
        }

        [HttpPost("reset")]
        public IActionResult ResetDatabase()
        {
            try
            {
                CinemaDbInitializer.ResetAndSeed(_context);
                return Ok(new
                {
                    Message = "Database has been reset and seeded successfully.",
                    Credentials = new[]
                    {
                        new { Role = "Admin", Email = "admin@gmail.com", Password = "Password123", Description = "Tài khoản quản trị quản lý phim, rạp, lịch chiếu, người dùng" },
                        new { Role = "User", Email = "user@gmail.com", Password = "Password123", Description = "Tài khoản người dùng đặt vé, xem lịch sử, gửi đánh giá" }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to reset database.", Error = ex.Message, InnerError = ex.InnerException?.Message });
            }
        }

        [HttpGet("credentials")]
        public IActionResult GetCredentials()
        {
            return Ok(new
            {
                Credentials = new[]
                {
                    new { Role = "Admin", Email = "admin@gmail.com", Password = "Password123", Description = "Tài khoản quản trị quản lý phim, rạp, lịch chiếu, người dùng" },
                    new { Role = "User", Email = "user@gmail.com", Password = "Password123", Description = "Tài khoản người dùng đặt vé, xem lịch sử, gửi đánh giá" }
                }
            });
        }
    }
}
