using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using ZyphorAPI.Data;
using ZyphorAPI.Models;

namespace ZyphorAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LikesController : ControllerBase
{
    private readonly MongoDbContext _context;

    public LikesController(MongoDbContext context)
    {
        _context = context;
    }

[Authorize]
[HttpPost("{postId}")]
public async Task<IActionResult> ToggleLike(string postId)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var existing = await _context.Likes
        .Find(l => l.PostId == postId && l.UserId == userId)
        .FirstOrDefaultAsync();

    // UNLIKE
    if (existing != null)
    {
        await _context.Likes.DeleteOneAsync(l => l.Id == existing.Id);

        var update = Builders<Post>.Update.Inc(p => p.LikeCount, -1);

        await _context.Posts.UpdateOneAsync(
            p => p.Id == postId,
            update
        );

        return Ok(new { liked = false });
    }

    // LIKE
    await _context.Likes.InsertOneAsync(new Like
    {
        PostId = postId,
        UserId = userId
    });

    var updateLike = Builders<Post>.Update.Inc(p => p.LikeCount, 1);

    await _context.Posts.UpdateOneAsync(
        p => p.Id == postId,
        updateLike
    );

    // 🔔 CREATE NOTIFICATION
    var post = await _context.Posts
        .Find(p => p.Id == postId)
        .FirstOrDefaultAsync();

    if (post != null && post.UserId != userId)
    {
        var notification = new Notification
        {
            UserId = post.UserId,     // receiver
            SenderId = userId,        // who liked
            Type = "like",
            ReferenceId = postId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Notifications.InsertOneAsync(notification);
    }

    return Ok(new { liked = true });
}
    [Authorize]
    [HttpGet("{postId}")]
    public async Task<IActionResult> GetLikes(string postId)
    {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var totalLikes = await _context.Likes
        .CountDocumentsAsync(l => l.PostId == postId);

    var isLikedByCurrentUser = await _context.Likes
        .Find(l => l.PostId == postId && l.UserId == userId)
        .AnyAsync();

    return Ok(new
    {
        postId = postId,
        totalLikes = totalLikes,
        likedByCurrentUser = isLikedByCurrentUser
    });
    }
}