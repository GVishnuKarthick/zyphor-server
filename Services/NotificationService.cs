using ZyphorAPI.Data;
using ZyphorAPI.Models;

namespace ZyphorAPI.Services
{
    public class NotificationService
    {
        private readonly MongoDbContext _context;

        public NotificationService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotification(
            string userId,
            string senderId,
            string type,
            string referenceId)
        {
            var notification = new Notification
            {
                UserId = userId,
                SenderId = senderId,
                Type = type,
                ReferenceId = referenceId
            };

            await _context.Notifications.InsertOneAsync(notification);
        }
    }
}