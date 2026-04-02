using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using ZyphorAPI.Data;
using ZyphorAPI.Models;

namespace ZyphorAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoriesController : ControllerBase
{
    private readonly MongoDbContext _context;

    public StoriesController(MongoDbContext context)
    {
        _context = context;
    }

    // GET: api/stories
[HttpGet]
public async Task<IActionResult> GetStories()
{
    var now = DateTime.UtcNow;

    var stories = await _context.Stories
        .Find(s => s.ExpiresAt > now)
        .SortByDescending(s => s.CreatedAt)
        .ToListAsync();

    var users = await _context.Users.Find(_ => true).ToListAsync();

    var result = stories.Select(s =>
    {
        var user = users.FirstOrDefault(u => u.Id == s.UserId);

        return new
        {
            s.Id,
            s.UserId,
            Username = user?.Username,
            profileImageUrl = user?.ProfileImageUrl,
            s.MediaUrl,
            s.CreatedAt
        };
    });

    return Ok(result);
}

    // POST: api/stories
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateStory([FromBody] Story story)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        story.UserId = userId;
        story.CreatedAt = DateTime.UtcNow;
        story.ExpiresAt = DateTime.UtcNow.AddHours(24);

        await _context.Stories.InsertOneAsync(story);

        return Ok(story);
    }

    // DELETE: api/stories/{id}
    //[Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStory(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var story = await _context.Stories
            .Find(s => s.Id == id)
            .FirstOrDefaultAsync();

        if (story == null)
            return NotFound("Story not found");

        if (story.UserId != userId)
           return StatusCode(403, "You can only delete your own story");

        await _context.Stories.DeleteOneAsync(s => s.Id == id);

        return Ok(new { message = "Story deleted successfully" });
    }
}