using MongoDB.Driver;
using ZyphorAPI.Models;

namespace ZyphorAPI.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration config)
    {
        var client = new MongoClient(config["MongoDb:ConnectionString"]);
        _database = client.GetDatabase(config["MongoDb:DatabaseName"]);
        var storyCollection = _database.GetCollection<Story>("Stories");
        var indexKeys = Builders<Story>.IndexKeys.Ascending(s => s.ExpiresAt);
        var indexOptions = new CreateIndexOptions
        {
            ExpireAfter = TimeSpan.Zero
        };
        var indexModel = new CreateIndexModel<Story>(indexKeys, indexOptions);
        storyCollection.Indexes.CreateOne(indexModel);
    }

    public IMongoCollection<User> Users =>
        _database.GetCollection<User>("Users");
    public IMongoCollection<Follow> Follows =>
        _database.GetCollection<Follow>("Follows");
    public IMongoCollection<Post> Posts =>
        _database.GetCollection<Post>("Posts");
    public IMongoCollection<Like> Likes =>
         _database.GetCollection<Like>("Likes");
    public IMongoCollection<Comment> Comments =>
        _database.GetCollection<Comment>("Comments");
    public IMongoCollection<SavedPost> SavedPosts =>
        _database.GetCollection<SavedPost>("SavedPosts");
    public IMongoCollection<Story> Stories =>
        _database.GetCollection<Story>("Stories");
    public IMongoCollection<Message> Messages =>
        _database.GetCollection<Message>("Messages");
    public IMongoCollection<Conversation> Conversations =>
        _database.GetCollection<Conversation>("Conversations");
    public IMongoCollection<Notification> Notifications =>
        _database.GetCollection<Notification>("Notifications");
}