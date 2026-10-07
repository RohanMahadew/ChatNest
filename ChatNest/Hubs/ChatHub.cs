using Microsoft.AspNetCore.SignalR;

namespace ChatNest.Hubs
{
    public class ChatHub : Hub
    {
        //for multiple connection change to ConcurrentDictionary?
        private static readonly Dictionary<string, string> _userConnections = [];

        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier; // Check if Context is actually set
            if (!string.IsNullOrEmpty(userId))
            {
                lock (_userConnections) 
                {
                    _userConnections[userId] = Context.ConnectionId;
                }   
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                lock (_userConnections)
                {
                    _userConnections.Remove(userId);
                }
            }

            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string receiverId, string message)
        {
            if (_userConnections.TryGetValue(receiverId, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", Context.UserIdentifier, message);
            }
        }
    }
}
