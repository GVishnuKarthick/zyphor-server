using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using ZyphorAPI.Data;

namespace ZyphorAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly MongoDbContext _context;

    public MessagesController(MongoDbContext context)
    {
        _context = context;
    }

    // ===============================
    // ✅ GET UNREAD MESSAGES COUNT
    // ===============================
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var currentUserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        
        var conversations = await _context.Conversations
            .Find(c => c.Members.Contains(currentUserId))
            .ToListAsync();
            
        var conversationIds = conversations.Select(c => c.Id).ToList();
        
        var unreadMessages = await _context.Messages
            .Find(m => conversationIds.Contains(m.ConversationId) && m.SenderId != currentUserId && !m.IsRead)
            .ToListAsync();

        var unreadCounts = unreadMessages
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToList();

        return Ok(unreadCounts);
    }

    // ===============================
    // ✅ GET MESSAGES BY CONVERSATION
    // ===============================
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet("{conversationId}")]
    public async Task<IActionResult> GetMessages(string conversationId)
    {
        var currentUserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

        // Mark messages from others in this conversation as read
        var updateFilter = Builders<Models.Message>.Filter.And(
            Builders<Models.Message>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<Models.Message>.Filter.Ne(m => m.SenderId, currentUserId),
            Builders<Models.Message>.Filter.Eq(m => m.IsRead, false)
        );
        var update = Builders<Models.Message>.Update.Set(m => m.IsRead, true);
        await _context.Messages.UpdateManyAsync(updateFilter, update);

        var msgs = await _context.Messages
            .Find(m => m.ConversationId == conversationId)
            .SortBy(m => m.CreatedAt)
            .ToListAsync();

        return Ok(msgs);
    }
}