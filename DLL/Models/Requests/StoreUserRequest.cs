using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.Requests
{
    sealed record StoreUserRequest(
        string Email,
        string UserName, 
        DateOnly DateOfBirth,
        string Password
    );
}
