using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using ZyphorAPI.Data;
using ZyphorAPI.DTOs;
using ZyphorAPI.Models;

using ZyphorAPI.Services;

namespace ZyphorAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly MongoDbContext _context;
    private readonly CloudinaryService _cloudinaryService;

    public UsersController(MongoDbContext context, CloudinaryService cloudinaryService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
    }

    // =========================
    // ✅ GET CURRENT USER
    // =========================
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var user = await _context.Users
            .Find(u => u.Id == userId)
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound("User not found");

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.Roles,
            user.Bio,
            user.ProfileImageUrl
        });
    }

    // =========================
    // ✅ UPDATE PROFILE
    // =========================
    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var existingUser = await _context.Users
            .Find(u => u.Id == userId)
            .FirstOrDefaultAsync();

        if (existingUser == null)
            return NotFound("User not found");

        if (string.IsNullOrWhiteSpace(dto.Username))
            return BadRequest("Username is required");

        var normalizedUsername = dto.Username.Trim();

        var usernameTaken = await _context.Users
            .Find(u => u.Username == normalizedUsername && u.Id != userId)
            .FirstOrDefaultAsync();

        if (usernameTaken != null)
            return BadRequest("Username already taken");

        string profileImageUrl = existingUser.ProfileImageUrl;

        if (dto.Image != null)
        {
            profileImageUrl = await _cloudinaryService.UploadImageAsync(dto.Image);
        }
        else if (!string.IsNullOrEmpty(dto.ProfileImageUrl))
        {
            profileImageUrl = dto.ProfileImageUrl;
        }

        var update = Builders<User>.Update
            .Set(u => u.Username, normalizedUsername)
            .Set(u => u.Bio, dto.Bio ?? string.Empty)
            .Set(u => u.ProfileImageUrl, profileImageUrl);

        await _context.Users.UpdateOneAsync(u => u.Id == userId, update);

        var updatedUser = await _context.Users
            .Find(u => u.Id == userId)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            updatedUser!.Id,
            updatedUser.Username,
            updatedUser.Email,
            updatedUser.Roles,
            updatedUser.Bio,
            updatedUser.ProfileImageUrl
        });
    }

    // =========================
    // ✅ SUGGESTED USERS (FIXED)
    // =========================
[Authorize]
[HttpGet("suggestions")]
public async Task<IActionResult> GetSuggestions()
{
    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(currentUserId))
        return Unauthorized();

    var users = await _context.Users
        .Find(u => u.Id != currentUserId)
        .Limit(10)
        .ToListAsync();

    // 🔥 get all users you are following
    var followingList = await _context.Follows
        .Find(f => f.FollowerId == currentUserId)
        .ToListAsync();

    var followingIds = followingList
        .Select(f => f.FollowingId)
        .ToHashSet();

    var result = users.Select(u => new
    {
        id = u.Id,
        username = u.Username,
        profileImageUrl = u.ProfileImageUrl,

        avatar = (u.Username ?? "U")
                    .Split(" ")
                    .Select(w => w[0])
                    .Take(2)
                    .Aggregate("", (acc, c) => acc + c)
                    .ToUpper(),

        bio = u.Bio ?? "",
        mutual = 0,

        // ✅ CORRECT CHECK
        following = followingIds.Contains(u.Id)
    });

    return Ok(result);
}

// =========================
// 👤 GET BY USERNAME
// =========================
[Authorize]
[HttpGet("{username}")]
public async Task<IActionResult> GetByUsername(string username)
{
    var user = await _context.Users
        .Find(u => u.Username.ToLower() == username.ToLower())
        .FirstOrDefaultAsync();

    if (user == null)
        return NotFound("User not found");

    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var following = await _context.Follows
        .Find(f => f.FollowerId == currentUserId && f.FollowingId == user.Id)
        .AnyAsync();

    return Ok(new
    {
        id = user.Id,
        username = user.Username,
        bio = user.Bio,
        profileImageUrl = user.ProfileImageUrl,
        following = following,
        avatar = (user.Username ?? "U").Substring(0, Math.Min(2, (user.Username ?? "").Length)).ToUpper()
    });
}
        // =========================
// 🔍 SEARCH USERS (FOR CHAT)
// =========================
[Authorize]
[HttpGet("search")]
public async Task<IActionResult> SearchUsers(string q)
{
    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(q))
        return Ok(new List<object>());

    var users = await _context.Users
        .Find(u =>
            u.Username.ToLower().Contains(q.ToLower()) &&
            u.Id != currentUserId
        )
        .Limit(10)
        .ToListAsync();

    var result = users.Select(u => new
    {
        id = u.Id,
        username = u.Username,
        profileImageUrl = u.ProfileImageUrl
    });

    return Ok(result);
}
    // =========================
    // 🔧 DEBUG CLAIMS
    // =========================
    [Authorize]
    [HttpGet("debug")]
    public IActionResult DebugClaims()
    {
        var claims = User.Claims.Select(c => new
        {
            c.Type,
            c.Value
        });

        return Ok(claims);
    }

}