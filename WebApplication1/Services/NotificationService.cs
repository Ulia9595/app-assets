using Microsoft.AspNetCore.SignalR;
using WebApplication1.Hubs;
using WebApplication1.Models.Entities;

namespace WebApplication1.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(int userId, string title, string message, NotificationType type, int? referenceId = null);
        Task SendNotificationToAllAsync(string title, string message, NotificationType type, int? referenceId = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(int userId, string title, string message, NotificationType type, int? referenceId = null)
        {
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                Type = type.ToString(),
                ReferenceId = referenceId,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        public async Task SendNotificationToAllAsync(string title, string message, NotificationType type, int? referenceId = null)
        {
            Console.WriteLine($"Sending notification to all: {title}");
            await _hubContext.Clients.Group("players").SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                Type = type.ToString(),
                ReferenceId = referenceId,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            });
            Console.WriteLine("Notification sent");
        }
    }
}