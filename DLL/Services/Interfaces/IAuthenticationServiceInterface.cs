using BLL.Models.DTOs;
using BLL.Models.Requests;

namespace BLL.Services.Interfaces
{
    public interface IAuthenticationServiceInterface
    {
        Task<UserDTO> RegisterAsync(StoreUserRequest request);
        Task<UserDTO> LoginAsync(AuthenticateUserRequest request);
        Task<bool> SignOutUserAsync();
    }
}
