using Microsoft.AspNetCore.Http;

namespace ZyphorAPI.DTOs;

public class UpdateProfileDto
{
    public string Username { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
}