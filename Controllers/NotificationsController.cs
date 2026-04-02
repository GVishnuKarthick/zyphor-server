using Microsoft.AspNetCore.Mvc;
using ZyphorAPI.Data;
using ZyphorAPI.Models;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace ZyphorAPI.Controllers

{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly MongoDbContext _context;

        public NotificationsController(MongoDbContext context)
        {
            _context = context;
        }
        
        [Authorize]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserNotifications(string userId)
        {
            var notifications = await _context.Notifications
                .Find(x => x.UserId == userId)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();

            var result = new List<object>();
            foreach (var n in notifications)
            {
                var sender = await _context.Users.Find(u => u.Id == n.SenderId).FirstOrDefaultAsync();
                result.Add(new
                {
                    id = n.Id,
                    senderId = n.SenderId,
                    username = sender?.Username ?? "unknown",
                    profileImageUrl = sender?.ProfileImageUrl,
                    type = n.Type,
                    referenceId = n.ReferenceId,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt
                });
            }

            return Ok(result);
        }

        [HttpPut("read/{id}")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            var update = Builders<Notification>.Update
                .Set(x => x.IsRead, true);

            await _context.Notifications
                .UpdateOneAsync(x => x.Id == id, update);

            return Ok("Notification marked as read");
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var notifications = await _context.Notifications
                .Find(n => n.UserId == userId)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();

            var result = new List<object>();
            foreach (var n in notifications)
            {
                var sender = await _context.Users.Find(u => u.Id == n.SenderId).FirstOrDefaultAsync();
                result.Add(new
                {
                    id = n.Id,
                    senderId = n.SenderId,
                    username = sender?.Username ?? "unknown",
                    profileImageUrl = sender?.ProfileImageUrl,
                    type = n.Type,
                    referenceId = n.ReferenceId,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt
                });
            }
            return Ok(result);
        }

        [Authorize]
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var update = Builders<Notification>.Update.Set(x => x.IsRead, true);
            await _context.Notifications.UpdateManyAsync(x => x.UserId == userId, update);
            return Ok("All marked read");
        }
    [Authorize]
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearNotifications()
    {   
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    await _context.Notifications.DeleteManyAsync(n => n.UserId == userId);

    return Ok("All notifications cleared");
    } 
    }
    
}