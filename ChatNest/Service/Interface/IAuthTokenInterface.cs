using ChatNest.Models;

namespace ChatNest.Service.Interface
{
    public interface IAuthTokenInterface
    {
        public string GenerateToken(int userId, string username, string email);
        public Task<User> ValidateUserCookie(HttpRequest request);
        public String GetAuthTokenFromCookie(HttpRequest request);
        public Task<bool> IsAuthenticated(HttpRequest request);
        public Task SetAuthCookie(User user, IHttpContextAccessor httpContextAccessor);
        public CookieOptions GetCookieOptions();
    }
}
