using ChatNest.Models;
using System.Security.Claims;

namespace ChatNest.Service.Interface
{
    public interface IAuthTokenInterface
    {
        public ClaimsPrincipal createClaims(string userId, string username, string email);
    }
}
