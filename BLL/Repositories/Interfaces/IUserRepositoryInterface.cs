using BLL.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Repositories.Interfaces
{
    public interface IUserRepositoryInterface
    {
        public Task<User?> FindUserByUsername(string username);
        public Task<User?> FindUserByUsernameOrEmail(string username, string email);
        public Task<bool> Store(User user);
    }
}
