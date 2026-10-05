using ChatNest.Models;
using ChatNest.Repositories;
using ChatNest.Repositories.Interface;
using ChatNest.Service.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ChatNest.Service
{
    public class AuthTokenService : IAuthTokenInterface
    {
        public ClaimsPrincipal createClaims(string userId, string username, string email)
        {
            var claims = new List<Claim> {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, username),
                new(ClaimTypes.Email, email)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            return principal;
        }
    }
}
