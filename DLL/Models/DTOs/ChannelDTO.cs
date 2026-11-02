using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.DTOs
{
    sealed record ChannelDTO(
        int Id,
        string Uuid,
        string Type,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
