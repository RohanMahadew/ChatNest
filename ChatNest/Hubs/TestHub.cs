using ChatNest.Models;
using Microsoft.AspNetCore.SignalR;

namespace ChatNest.Hubs
{
    public class TestHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
