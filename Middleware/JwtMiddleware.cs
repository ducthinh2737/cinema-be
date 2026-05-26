using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CinemaBooking.API.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var emailClaim = context.User.FindFirst(ClaimTypes.Email)?.Value;
                var rolesClaims = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    context.Items["UserId"] = userIdClaim;
                }
                if (!string.IsNullOrEmpty(emailClaim))
                {
                    context.Items["UserEmail"] = emailClaim;
                }
                if (rolesClaims.Any())
                {
                    context.Items["UserRoles"] = rolesClaims;
                }
            }

            await _next(context);
        }
    }
}
