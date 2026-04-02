using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using ZyphorAPI.Data;
using ZyphorAPI.Models;

namespace ZyphorAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationsController : ControllerBase
{
    private readonly MongoDbContext _context;

    public ConversationsController(MongoDbContext context)
    {
        _context = context;
    }

    // ===============================
    // ✅ CREATE OR GET CONVERSATION
    // ===============================
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateOrGetConversation([FromBody] List<string> members)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (members == null || members.Count < 2)
            return BadRequest("Conversation must have at least 2 members");

        if (!members.Contains(currentUserId))
            return Unauthorized("You must be part of the conversation");

        // 🔥 FIX: safer check (order independent)
        var existing = await _context.Conversations
            .Find(c => c.Members.Count == members.Count &&
                       c.Members.All(m => members.Contains(m)))
            .FirstOrDefaultAsync();

        if (existing != null)
            return Ok(existing);

        var convo = new Conversation
        {
            Members = members,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Conversations.InsertOneAsync(convo);

        return Ok(convo);
    }

    // ===============================
    // ✅ GET USER CONVERSATIONS
    // ===============================
   [Authorize]
[HttpGet]
public async Task<IActionResult> GetUserConversations()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var convos = await _context.Conversations
        .Find(c => c.Members.Contains(userId))
        .ToListAsync();

    var result = new List<object>();

    foreach (var convo in convos)
    {
        var otherUserId = convo.Members.First(m => m != userId);

        var user = await _context.Users
            .Find(u => u.Id == otherUserId)
            .FirstOrDefaultAsync();

        var lastMsg = await _context.Messages
            .Find(m => m.ConversationId == convo.Id)
            .SortByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        result.Add(new
        {
            id = convo.Id,
            userId = otherUserId,
            username = user?.Username ?? "Unknown",
            profileImageUrl = user?.ProfileImageUrl ?? "",
            lastMessage = lastMsg?.Text,
            lastMessageTime = lastMsg?.CreatedAt
        });
    }

    return Ok(result);
}
}