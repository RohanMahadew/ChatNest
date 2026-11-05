using BLL.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Services.Interfaces
{
    public interface IUserServiceInterface
    {
        Task<List<UserDTO>> SearchUsersAsync(string searchQuery, int excludedUserId);
    }
}
