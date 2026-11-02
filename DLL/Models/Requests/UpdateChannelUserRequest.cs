using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.Requests
{
    sealed record UpdateChannelUserRequest(
        int Id,
        int Role
    );
}
