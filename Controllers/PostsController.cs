using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using ZyphorAPI.Data;
using ZyphorAPI.Models;
using ZyphorAPI.DTOs;
using ZyphorAPI.Services;

namespace ZyphorAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly MongoDbContext _context;
    private readonly CloudinaryService _cloudinaryService;

    public PostsController(MongoDbContext context, CloudinaryService cloudinaryService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
    {
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        string imageUrl = string.Empty;

        if (dto.Image != null)
        {
            imageUrl = await _cloudinaryService.UploadImageAsync(dto.Image);
        }
        if (dto.Image == null)
        {
             Console.WriteLine("❌ Image NOT received");
        }
        else
        {
            
          Console.WriteLine("✅ Image received");
        }
        var post = new Post
        {
            UserId = userId,
            Caption = dto.Caption,
            ImageUrls = string.IsNullOrEmpty(imageUrl) ? new List<string>() : new List<string> { imageUrl },
            LikeCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Posts.InsertOneAsync(post);

        //return Ok(post);
        return Ok(new
        {
            id = post.Id,
            caption = post.Caption,
            imageUrls = post.ImageUrls,
            userId = post.UserId,
            createdAt = post.CreatedAt
        });
    }

    [HttpGet]
public async Task<IActionResult> GetAllPosts()
{
    var posts = await _context.Posts
        .Find(_ => true)
        .SortByDescending(p => p.CreatedAt)
        .ToListAsync();

    var result = new List<object>();

    foreach (var post in posts)
    {
        var user = await _context.Users
            .Find(u => u.Id == post.UserId)
            .FirstOrDefaultAsync();

        result.Add(new
        {
            id = post.Id,
            caption = post.Caption,
            imageUrls = post.ImageUrls,
            likeCount = post.LikeCount,
            createdAt = post.CreatedAt,
            username = user?.Username ?? "Unknown",
            profileImageUrl = user?.ProfileImageUrl ?? ""
        });
    }

    return Ok(result);
}

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost(string id, [FromForm] CreatePostDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var post = await _context.Posts.Find(p => p.Id == id).FirstOrDefaultAsync();

        if (post == null)
            return NotFound(new { message = "Post not found" });

        if (post.UserId != userId)
            return Forbid();

        if (!string.IsNullOrWhiteSpace(dto.Caption))
        {
            post.Caption = dto.Caption;
        }

        if (dto.Image != null)
        {
            var imageUrl = await _cloudinaryService.UploadImageAsync(dto.Image);
            post.ImageUrls = new List<string> { imageUrl };
        }

        var update = Builders<Post>.Update
            .Set(p => p.Caption, post.Caption)
            .Set(p => p.ImageUrls, post.ImageUrls);

        await _context.Posts.UpdateOneAsync(p => p.Id == id, update);

        return Ok(new { message = "Post updated successfully", post });
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var post = await _context.Posts.Find(p => p.Id == id).FirstOrDefaultAsync();

        if (post == null)
            return NotFound(new { message = "Post not found" });

        

        await _context.Posts.DeleteOneAsync(p => p.Id == id);

        return Ok(new { message = "Post deleted successfully" });
    }
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserPosts(string userId)
    {
    var posts = await _context.Posts
        .Find(p => p.UserId == userId)
        .SortByDescending(p => p.CreatedAt)
        .ToListAsync();

    return Ok(posts);
    }

}