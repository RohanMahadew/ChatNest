using BLL.Models;
using BLL.Models.Enums;

namespace BLL.Repositories.Interfaces
{
    public interface IChannelUserRepositoryInterface
    {
        public Task<ChannelUser?> FindById(int ChannelUserId);
        public Task<List<ChannelUser?>> FindByUser(int UserId);
        public Task<List<ChannelUser?>> FindByChannel(int ChannelId);
        public Task<List<ChannelUser?>> FindByRole(ChannelRole Role);
        public Task<ChannelUser?> Store(ChannelUser channelUser);
        public Task<ChannelUser?> Update(ChannelUser channelUser);
        public Task<bool> Delete(int ChannelUserId);
    }
}
