using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZyphorAPI.Models
{
    public class Conversation
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
       public string Id { get; set; }

      public List<string> Members { get; set; } = new();

      public string? LastMessage { get; set; }   // ✅ NEW

      public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

      public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // ✅ NEW
    }
}