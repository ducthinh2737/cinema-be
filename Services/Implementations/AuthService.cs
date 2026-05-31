using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Auth;
using CinemaBooking.API.Helpers;
using CinemaBooking.API.Models.Users;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtHelper _jwtHelper;

        public AuthService(IUserRepository userRepository, JwtHelper jwtHelper)
        {
            _userRepository = userRepository;
            _jwtHelper = jwtHelper;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new Exception("Email is already registered.");
            }

            var user = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsEmailVerified = false,
                EmailVerificationToken = Guid.NewGuid().ToString()
            };

            // Assign default User role (RoleId = 2)
            user.UserRoles.Add(new UserRole { RoleId = 2 });

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // Fetch user with roles again (to get RoleName properly populated)
            var createdUser = await _userRepository.GetByIdAsync(user.UserId);
            var roles = createdUser!.UserRoles.Select(ur => ur.Role.RoleName).ToList();

            var (accessToken, expiresAt) = _jwtHelper.GenerateAccessToken(createdUser, roles);
            var refreshToken = _jwtHelper.GenerateRefreshToken("0.0.0.0");

            createdUser.RefreshTokens.Add(refreshToken);
            await _userRepository.SaveChangesAsync();

            return new AuthResponse
            {
                UserId = createdUser.UserId,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = expiresAt,
                Email = createdUser.Email,
                FullName = createdUser.FullName,
                Roles = roles
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new Exception("Invalid email or password.");
            }

            if (!user.IsActive)
            {
                throw new Exception("User account is disabled.");
            }

            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
            var (accessToken, expiresAt) = _jwtHelper.GenerateAccessToken(user, roles);
            var refreshToken = _jwtHelper.GenerateRefreshToken(ipAddress);

            user.RefreshTokens.Add(refreshToken);
            await _userRepository.SaveChangesAsync();

            return new AuthResponse
            {
                UserId = user.UserId,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = expiresAt,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string token, string ipAddress)
        {
            var user = await _userRepository.GetByRefreshTokenAsync(token);
            if (user == null)
            {
                throw new Exception("Invalid refresh token.");
            }

            var refreshToken = user.RefreshTokens.Single(rt => rt.Token == token);
            if (!refreshToken.IsActive)
            {
                throw new Exception("Refresh token has expired or been revoked.");
            }

            // Revoke current token
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;

            // Generate new ones
            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
            var (accessToken, expiresAt) = _jwtHelper.GenerateAccessToken(user, roles);
            var newRefreshToken = _jwtHelper.GenerateRefreshToken(ipAddress);

            refreshToken.ReplacedByToken = newRefreshToken.Token;
            user.RefreshTokens.Add(newRefreshToken);

            await _userRepository.SaveChangesAsync();

            return new AuthResponse
            {
                UserId = user.UserId,
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = expiresAt,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles
            };
        }

        public async Task<bool> LogoutAsync(string token, string ipAddress)
        {
            var user = await _userRepository.GetByRefreshTokenAsync(token);
            if (user == null) return false;

            var refreshToken = user.RefreshTokens.SingleOrDefault(rt => rt.Token == token);
            if (refreshToken != null && refreshToken.IsActive)
            {
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;
                await _userRepository.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null) return false;

            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(2);

            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userRepository.GetByResetTokenAsync(request.Token);
            if (user == null || user.ResetTokenExpires < DateTime.UtcNow)
            {
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.ResetTokenExpires = null;

            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            {
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            var user = await _userRepository.GetByVerificationTokenAsync(token);
            if (user == null) return false;

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;

            await _userRepository.SaveChangesAsync();
            return true;
        }
    }
}
