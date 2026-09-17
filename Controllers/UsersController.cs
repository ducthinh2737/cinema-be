using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Users;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly CinemaDbContext _context;
        private readonly IWebHostEnvironment _env;
        private const long MaxFileBytes = 5 * 1024 * 1024; // 5 MB
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public UsersController(CinemaDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("member-tiers")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMemberTiers()
        {
            var tiers = await _context.MemberTiers.ToListAsync();
            return Ok(tiers);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.MemberTier)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            return Ok(new
            {
                user.UserId,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                user.Gender,
                user.DateOfBirth,
                user.AvatarUrl,
                user.IsEmailVerified,
                user.MembershipPoints,
                Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
                TierName = user.MemberTier?.TierName ?? "Bronze"
            });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.MemberTier)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            // Update allowed fields
            user.FullName = request.FullName ?? user.FullName;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.Gender = request.Gender ?? user.Gender;
            user.DateOfBirth = request.DateOfBirth ?? user.DateOfBirth;
            if (request.AvatarUrl != null)
            {
                user.AvatarUrl = request.AvatarUrl;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.UserId,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                user.Gender,
                user.DateOfBirth,
                user.AvatarUrl,
                user.IsEmailVerified,
                user.MembershipPoints,
                Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
                TierName = user.MemberTier?.TierName ?? "Bronze"
            });
        }

        [HttpPost("profile/avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "No file uploaded." });
            }

            if (file.Length > MaxFileBytes)
            {
                return BadRequest(new { Message = "File size exceeds limit of 5 MB." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return BadRequest(new { Message = $"Invalid file format. Allowed formats: {string.Join(", ", _allowedExtensions)}" });
            }

            // Create directories if they do not exist
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "avatars");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update in database (relative URL)
            var fileUrl = $"/uploads/avatars/{fileName}";
            user.AvatarUrl = fileUrl;
            await _context.SaveChangesAsync();

            return Ok(new { Url = fileUrl });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Email.Contains(search) || u.FullName.Contains(search) || u.PhoneNumber.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.UserId,
                    u.Email,
                    u.FullName,
                    u.PhoneNumber,
                    u.IsEmailVerified,
                    u.MembershipPoints,
                    Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserByAdmin(int id, [FromBody] AdminUpdateUserRequest request)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            user.FullName = request.FullName ?? user.FullName;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.MembershipPoints = request.MembershipPoints ?? user.MembershipPoints;

            if (request.Roles != null)
            {
                // Remove existing roles
                _context.UserRoles.RemoveRange(user.UserRoles);

                // Add new roles
                foreach (var roleName in request.Roles)
                {
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                    if (role != null)
                    {
                        _context.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.UserId,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                user.IsEmailVerified,
                user.MembershipPoints,
                Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User successfully deleted." });
        }
    }

    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class AdminUpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public int? MembershipPoints { get; set; }
        public System.Collections.Generic.List<string>? Roles { get; set; }
    }
}
