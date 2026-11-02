using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.DTOs
{
    sealed record MessageDTO(
        int Id,
        int UserId,
        int ChannelId,
        string MessageStatus,
        string Content,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
