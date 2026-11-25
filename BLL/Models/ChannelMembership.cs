using BLL.Models.Enums;

namespace BLL.Models
{
    public sealed record ChannelMembership(Channel Channel, ChannelRole Role);
}
