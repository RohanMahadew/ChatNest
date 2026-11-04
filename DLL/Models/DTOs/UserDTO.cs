using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.DTOs
{
    public sealed record UserDTO(
        int Id,
        string UserName,
        string Email,
        DateOnly DateOfBirth
    );

    public static class UserDTOExtensions
    {
        public static UserDTO fromUser(User user)
        {
            return new UserDTO(
                user.Id,
                user.UserName,
                user.Email,
                user.DateOfBirth
            );
        }
    }
}
