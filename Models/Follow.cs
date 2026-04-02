using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZyphorAPI.Models;

public class Follow
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string FollowerId { get; set; }
    public string FollowingId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}