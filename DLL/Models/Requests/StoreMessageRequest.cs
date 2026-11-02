using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.Requests
{
    sealed record StoreMessageRequest(
        int SenderId,
        int ChannelId,
        string Content
    );
}
