using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZyphorAPI.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public bool IsEmailVerified { get; set; } = false;
    public string EmailOtp { get; set; }
    public DateTime? EmailOtpExpiry { get; set; }
    public string PhoneNumber { get; set; }
    public string PhoneOtp { get; set; }
    public DateTime? PhoneOtpExpiry { get; set; }
    public string Bio { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new() { "User" };

    public List<RefreshToken> RefreshTokens { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<string> Following { get; set; } = new();
    public List<string> Followers { get; set; } = new();
}