using BLL.Models.DTOs;
using BLL.Repositories.Interfaces;
using BLL.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Services
{
    public class UserService : IUserServiceInterface
    {
        private readonly IUserRepositoryInterface _repository;

        public UserService(IUserRepositoryInterface repository)
        {
            _repository = repository;
        }

        public async Task<List<UserDTO>> SearchUsersAsync(string searchQuery, int excludedUserId)
        {
            if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery.Length > 320)
            {
                return [];
            }

            var users = await _repository.SearchUsersByUsernameOrEmail(searchQuery.Trim(), excludedUserId);
            return users.Select(user => new UserDTO(user.Id, user.UserName, user.Email, user.DateOfBirth)).ToList();
        }
    }
}
