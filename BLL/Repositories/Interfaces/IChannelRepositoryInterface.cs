using BLL.Models;
using BLL.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace BLL.Repositories.Interfaces
{
    public interface IChannelRepositoryInterface
    {
        public Task<Models.Channel?> FindById(int Channelid);
        public Task<List<Models.Channel?>> FindByCreator(int Channelid);
        public Task<List<Models.Channel?>> FindByParticipant(int Channelid);
        public Task<List<Models.Channel?>> FindByUser(int Channelid);
        public Task<Models.Channel?> Store(Models.Channel channel);
        public Task<Models.Channel?> Update(Models.Channel channel);
        public Task<bool> Delete(int Channelid);
    }
}
