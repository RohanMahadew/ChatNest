using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.DTOs
{
    sealed record UserDTO(
        int Id,
        string UserName,
        string Email,
        DateOnly DateOfBirth
    );
}
