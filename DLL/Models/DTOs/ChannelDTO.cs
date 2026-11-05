using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.DTOs
{
    public sealed record ChannelDTO(
        int Id,
        string Uuid,
        string Type,
        string? Name,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    public static class ChannelDTOExtensions
    {
        public static ChannelDTO fromChannel(Channel channel)
        {
            return new ChannelDTO(
                channel.Id,
                channel.Uuid,
                channel.Type.ToString(),
                channel.Name,
                channel.CreatedAt,
                channel.UpdatedAt
            );
        }
    }
}
