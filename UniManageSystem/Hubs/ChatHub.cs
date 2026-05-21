using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace UniManageSystem.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public async Task SendPrivateMessage(string receiverId, string message)
        {
            var senderId = Context.UserIdentifier;
            if (senderId != null)
            {
                // Send to the receiver
                await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);
            }
        }
    }
}