using BLL.Models.Enums;
using BLL.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models
{
    public class Channel : ITimestampedEntity
    {
        public Channel(ChannelType type)
        {
            Type = type;
        }

        public Channel(string uuid, ChannelType type)
        {
            Uuid = uuid;
            Type = type;
        }

        public Channel(int id, string uuid, ChannelType type, DateTime createdAt, DateTime updatedAt)
        {
            Id = id;
            Uuid = uuid;
            Type = type;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public int Id { get; set; }
        public string Uuid { get; set; }
        public ChannelType Type { get; set; }
        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; }

    }
}
