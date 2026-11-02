using BLL.Models;
using BLL.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Repositories.Interfaces
{
    public interface IMessageRepositoryInterface
    {
        public Task<Message?> FindById(int MessageId);
        public Task<List<Message?>> FindBySender(int SenderId);
        public Task<List<Message?>> FindByReceiver(int ReceiverId);
        public Task<List<Message?>> FindByStatus(MessageStatus Status);
        public Task<List<Message?>> FindByContent(string Content);
        public Task<List<Message?>> FindByUser(int UserId);
        public Task<List<Message?>> FindByChannel(int ChannelId);
        public Task<Message?> Store(Message message);
        public Task<Message?> Update(Message message);
        public Task<bool> Delete(int MessageId);
    }
}
