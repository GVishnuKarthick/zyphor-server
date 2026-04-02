using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZyphorAPI.Models
{
    public class Message
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string ConversationId { get; set; }
        public string SenderId { get; set; }
        public string Text { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}