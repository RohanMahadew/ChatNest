using BLL.Models.Enums;
using BLL.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models
{
    public class ChannelUser : ITimestampedEntity
    {
        public ChannelUser(int userId, int channelId, ChannelRole channelRole)
        {
            UserId = userId;
            ChannelId = channelId;
            ChannelRole = channelRole;
        }

        public ChannelUser(int id, int userId, int channelId, ChannelRole channelRole, DateTime createdAt, DateTime updatedAt)
        {
            Id = id;
            UserId = userId;
            ChannelId = channelId;
            ChannelRole = channelRole;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public int Id { get; set; }
        public int UserId { get; set; }
        public int ChannelId { get; set; }
        public ChannelRole ChannelRole { get; set; }

        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; }
    }
}
