using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZyphorAPI.Models;

public class SavedPost
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string PostId { get; set; }
    public string UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}