using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.DTOs
{
    sealed record ChannelUserDTO(
        int Id,
        int UserId,
        int ChannelId,
        string ChannelRole,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
