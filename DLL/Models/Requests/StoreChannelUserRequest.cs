using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.Requests
{
    sealed record StoreChannelUserRequest(
        int UserId,
        int ChannelId,
        int Role
    );
}
