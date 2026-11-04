using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.Requests
{
    public sealed record AuthenticateUserRequest(
        string UserName,
        string Password
    );
}
