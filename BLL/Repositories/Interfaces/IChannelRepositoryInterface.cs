using BLL.Models;
using BLL.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using BLL.Models;

namespace BLL.Repositories.Interfaces
{
    public interface IChannelRepositoryInterface
    {
        public Task<Models.Channel?> FindById(int Channelid);
        public Task<List<Models.Channel?>> FindByCreator(int Channelid);
        public Task<List<Models.Channel?>> FindByParticipant(int Channelid);
        public Task<List<Models.Channel?>> FindByUser(int Channelid);
        public Task<Channel?> GetOrCreateDirectChannelAsync(Channel channel, ChannelUser firstMember, ChannelUser secondMember);
        public Task<List<Channel>> FindDirectChannelsForUserAsync(int userId);
        public Task<Channel?> FindDirectChannelForUserAsync(string uuid, int userId);
        public Task<Models.Channel?> Store(Models.Channel channel);
        public Task<Models.Channel?> Update(Models.Channel channel);
        public Task<bool> Delete(int Channelid);
    }
}
