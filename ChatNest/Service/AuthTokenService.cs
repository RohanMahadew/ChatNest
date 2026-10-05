using ChatNest.Models;
using ChatNest.Repositories;
using ChatNest.Repositories.Interface;
using ChatNest.Service.Interface;
using System.Security.Cryptography;
using System.Text;

namespace ChatNest.Service
{
    public class AuthTokenService : IAuthTokenInterface
    {
        private readonly IUserRepositoryInterface _repository;
        public AuthTokenService(IUserRepositoryInterface userRepository)
        {
            _repository = userRepository;
        }

        private const string TokenKey = "_authtoken";
        private const int TokenExpirationHours = 1;

        public string GenerateToken(int userId, string username, string email)
        {
            var tokenData = $"{userId}:{username}:{email}:{DateTime.UtcNow.Ticks}:{Guid.NewGuid()}";
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(tokenData));
                return Convert.ToBase64String(hash) + ":" + userId;
            }
        }

        public async Task<User> ValidateUserCookie(HttpRequest request)
        {
            var token = GetAuthTokenFromCookie(request);

            if (string.IsNullOrEmpty(token))
                return null;

            // <hash>:<userId>
            var parts = token.Split(':');

            if (parts.Length != 2)
                return null;

            if (!int.TryParse(parts[1], out int userId))
                return null;

            User user = await _repository.ValidateToken(    
                userId,
                token
            );

            if (user == null)
                return null;

            return user;
            
        }

        public string GetAuthTokenFromCookie(HttpRequest request)
        {
            if (request == null)
                return null;

            if (request.Cookies[TokenKey] == null)
                return null;

            return request.Cookies[TokenKey].ToString();
        }

        public async Task<bool> IsAuthenticated(HttpRequest request)
        {
            return await ValidateUserCookie(request) != null;
        }

        public async Task SetAuthCookie(User user, IHttpContextAccessor httpContextAccessor)
        {
            var token = GenerateToken(user.Id, user.UserName, user.Email);
            var cookieOptions = GetCookieOptions();
            httpContextAccessor.HttpContext!.Response.Cookies.Append("_authtoken", token, cookieOptions);
            var result = await _repository.InsertAuthToken(user.Id, token);
        }

        public CookieOptions GetCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = false, 
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(TokenExpirationHours)
            };
        }
    }
}
