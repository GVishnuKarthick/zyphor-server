using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZyphorAPI.Models;

public class Post
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string Caption { get; set; } = string.Empty;

    public List<string> ImageUrls { get; set; } = new();

    public int LikeCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}