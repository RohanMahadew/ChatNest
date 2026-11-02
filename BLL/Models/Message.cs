using BLL.Models.Enums;
using BLL.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models
{
    public class Message : ITimestampedEntity
    {
        public Message(int userId, int channelId, MessageStatus messageStatus, string content)
        {
            UserId = userId;
            ChannelId = channelId;
            MessageStatus = messageStatus;
            Content = content;
        }

        public Message(int id, int userId, int channelId, MessageStatus messageStatus, string content, DateTime createdAt, DateTime updatedAt)
        {
            Id = id;
            UserId = userId;
            ChannelId = channelId;
            MessageStatus = messageStatus;
            Content = content;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public int Id { get; set; }  
        public int UserId { get; set; }
        public int ChannelId { get; set; }
        public MessageStatus MessageStatus { get; set; }
        public string Content { get; set; }

        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; }
    }
}
