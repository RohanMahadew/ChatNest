using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.Requests
{
    sealed record StoreGroupChannelRequest(
        int CreatorId,
        bool isPrivate,
        string ChannelName
    );
}
