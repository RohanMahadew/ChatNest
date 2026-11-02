using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.Requests
{
    sealed record UpdateMessageRequest(
        int MessageId,
        string Content
    );
}
