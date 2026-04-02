using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using ZyphorAPI.Data;
using ZyphorAPI.Models;
using ZyphorAPI.DTOs;
namespace ZyphorAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly MongoDbContext _context;

    public CommentsController(MongoDbContext context)
    {
        _context = context;
    }

   [Authorize]
    [HttpPost("{postId}")]
    public async Task<IActionResult> AddComment(string postId,[FromBody] CreateCommentDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return BadRequest("UserId claim is null");
        }
        var comment = new Comment
        {
            PostId = postId,
            UserId = userId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow

        };

        await _context.Comments.InsertOneAsync(comment);

        return Ok(comment);
    }

    [HttpGet("{postId}")]
    public async Task<IActionResult> GetComments(string postId)
    {
        var comments = await _context.Comments
            .Find(c => c.PostId == postId)
            .SortByDescending(c => c.CreatedAt)
            .ToListAsync();

        var result = new List<object>();
        foreach (var c in comments)
        {
            var user = await _context.Users.Find(u => u.Id == c.UserId).FirstOrDefaultAsync();
            result.Add(new
            {
                id = c.Id,
                postId = c.PostId,
                userId = c.UserId,
                username = user?.Username ?? "unknown",
                profileImageUrl = user?.ProfileImageUrl,
                content = c.Content,
                createdAt = c.CreatedAt
            });
        }

        return Ok(result);
    }
}