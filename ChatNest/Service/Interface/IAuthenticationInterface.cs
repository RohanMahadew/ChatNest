using ChatNest.Request.Authentication;

namespace ChatNest.Service.Interface
{
    public interface IAuthenticationInterface
    {
        Task<StoreUserResult> RegisterAsync(StoreUserRequest request);
        Task<AuthenticateUserResult> LoginAsync(AuthenticateUserRequest request);
        Task SignOutUserAsync();
    }
}
