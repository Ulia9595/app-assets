using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace WebApplication1.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("uid")?.Value;
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            Console.WriteLine($"Client connected: userId={userId}, role={role}");

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            if (role == "player")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "players");
                Console.WriteLine($"Added to group 'players' for player {userId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("uid")?.Value;
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            if (role == "player")
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "players");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}