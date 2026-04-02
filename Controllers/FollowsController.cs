using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using ZyphorAPI.Data;
using ZyphorAPI.Models;

namespace ZyphorAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FollowsController : ControllerBase
{
    private readonly MongoDbContext _context;

    public FollowsController(MongoDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpPost("{userId}")]
    public async Task<IActionResult> FollowUser(string userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (currentUserId == userId)
            return BadRequest("You cannot follow yourself");

        var existing = await _context.Follows
            .Find(f => f.FollowerId == currentUserId && f.FollowingId == userId)
            .FirstOrDefaultAsync();

        if (existing != null)
            return BadRequest("Already following");

        var follow = new Follow
        {
            FollowerId = currentUserId,
            FollowingId = userId
        };
        await _context.Follows.InsertOneAsync(follow);
        var notification = new Notification
        {
            UserId = userId,        // receiver
            SenderId = currentUserId,  // who followed
            Type = "follow",
            ReferenceId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

    await _context.Notifications.InsertOneAsync(notification);
        
        
        return Ok("Followed successfully");
    }
    [Authorize]
    [HttpDelete("{userId}")]
    public async Task<IActionResult> UnfollowUser(string userId)
    {
    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var result = await _context.Follows.DeleteOneAsync(
        f => f.FollowerId == currentUserId && f.FollowingId == userId);
    if (result.DeletedCount == 0)
        return BadRequest("You are not following this user");
    return Ok("Unfollowed successfully");
    }
    [HttpGet("{userId}/followers")]
    public async Task<IActionResult> GetFollowers(string userId)
    {
    var followers = await _context.Follows
        .Find(f => f.FollowingId == userId)
        .ToListAsync();
    return Ok(followers);
    }
    [HttpGet("{userId}/following")]
    public async Task<IActionResult> GetFollowing(string userId)
    {
    var following = await _context.Follows
        .Find(f => f.FollowerId == userId)
        .ToListAsync();
    return Ok(following);
    }
    [HttpGet("{userId}/count")]
    public async Task<IActionResult> GetFollowCounts(string userId)
    {
    var followersCount = await _context.Follows
        .CountDocumentsAsync(f => f.FollowingId == userId);
    var followingCount = await _context.Follows
        .CountDocumentsAsync(f => f.FollowerId == userId);
    return Ok(new
    {
        Followers = followersCount,
        Following = followingCount
    });
    }

    [Authorize]
    [HttpGet("{userId}/status")]
    public async Task<IActionResult> CheckFollowStatus(string userId)
    {
    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var follow = await _context.Follows
        .Find(f => f.FollowerId == currentUserId && f.FollowingId == userId)
        .FirstOrDefaultAsync();

    return Ok(new
    {
        following = follow != null
    });
    }
}