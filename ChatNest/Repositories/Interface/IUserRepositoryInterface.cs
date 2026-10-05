using ChatNest.Models;
using Microsoft.Data.SqlClient;

namespace ChatNest.Repositories.Interface
{
    public interface IUserRepositoryInterface
    {
        public Task<User?> FindUserByUsernameAsync(string username);
        public Task<User?> FindUserByUsernameOrEmailAsync(string username, string email);
        public Task<bool> InsertUserAsync(User user);
        public Task<User> ValidateToken(int userId, string token);
        public Task<bool> InsertAuthToken(int userId, string token);
    }
}
