using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ZyphorAPI.DTOs;

public class CreatePostDto
{
    [Required]
    [MaxLength(500)]
    public string Caption { get; set; } = string.Empty;

    public IFormFile? Image { get; set; }
}