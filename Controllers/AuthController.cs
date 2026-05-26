using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Auth;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                SetRefreshTokenCookie(result.RefreshToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { Message = errorMsg });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var result = await _authService.LoginAsync(request, ipAddress);
                SetRefreshTokenCookie(result.RefreshToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request)
        {
            try
            {
                var token = Request.Cookies["refreshToken"] ?? request?.RefreshToken;
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { Message = "Refresh token is required." });
                }

                var ipAddress = GetIpAddress();
                var result = await _authService.RefreshTokenAsync(token, ipAddress);
                SetRefreshTokenCookie(result.RefreshToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request)
        {
            var token = Request.Cookies["refreshToken"] ?? request?.RefreshToken;
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { Message = "Refresh token is required." });
            }

            var ipAddress = GetIpAddress();
            var result = await _authService.LogoutAsync(token, ipAddress);
            if (result)
            {
                Response.Cookies.Delete("refreshToken");
                return Ok(new { Message = "Logged out successfully." });
            }

            return BadRequest(new { Message = "Invalid or expired token." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var result = await _authService.ForgotPasswordAsync(request);
            if (result)
            {
                return Ok(new { Message = "Password reset instructions have been generated. Please check user's reset token in the database." });
            }
            return BadRequest(new { Message = "Email address not found." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);
            if (result)
            {
                return Ok(new { Message = "Password has been reset successfully." });
            }
            return BadRequest(new { Message = "Invalid or expired reset token." });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);
            var result = await _authService.ChangePasswordAsync(userId, request);
            if (result)
            {
                return Ok(new { Message = "Password changed successfully." });
            }
            return BadRequest(new { Message = "Incorrect old password." });
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var result = await _authService.VerifyEmailAsync(token);
            if (result)
            {
                return Ok(new { Message = "Email verified successfully." });
            }
            return BadRequest(new { Message = "Invalid or expired verification token." });
        }

        private void SetRefreshTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.Lax,
                Secure = true
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"]!;
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
        }
    }
}
