using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Auth;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress);
        Task<AuthResponse> RefreshTokenAsync(string token, string ipAddress);
        Task<bool> LogoutAsync(string token, string ipAddress);
        Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<bool> VerifyEmailAsync(string token);
    }
}
