using Microsoft.AspNetCore.SignalR;
using ZyphorAPI.Data;
using ZyphorAPI.Models;
using MongoDB.Driver;

namespace ZyphorAPI.Hubs
{
    public class ChatHub : Hub
    {
        // ✅ Support multiple connections per user (mobile + web etc.)
        private static Dictionary<string, List<string>> OnlineUsers = new();

        private readonly MongoDbContext _context;

        public ChatHub(MongoDbContext context)
        {
            _context = context;
        }

        // =========================
        // 🔵 USER CONNECTED
        // =========================
        public override async Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

            if (!string.IsNullOrEmpty(userId))
            {
                lock (OnlineUsers)
                {
                    if (!OnlineUsers.ContainsKey(userId))
                        OnlineUsers[userId] = new List<string>();

                    OnlineUsers[userId].Add(Context.ConnectionId);
                }

                await Clients.All.SendAsync("UserOnline", userId);
            }

            await base.OnConnectedAsync();
        }

        // =========================
        // 🔴 USER DISCONNECTED
        // =========================
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string disconnectedUserId = null;

            lock (OnlineUsers)
            {
                var user = OnlineUsers.FirstOrDefault(x => x.Value.Contains(Context.ConnectionId));

                if (!string.IsNullOrEmpty(user.Key))
                {
                    user.Value.Remove(Context.ConnectionId);

                    if (user.Value.Count == 0)
                    {
                        OnlineUsers.Remove(user.Key);
                        disconnectedUserId = user.Key;
                    }
                }
            }

            if (!string.IsNullOrEmpty(disconnectedUserId))
            {
                await Clients.All.SendAsync("UserOffline", disconnectedUserId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // =========================
        // 💬 SEND MESSAGE (SECURE)
        // =========================
public async Task SendMessage(string conversationId, string senderId, string text)
{
    if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(text))
        throw new Exception("Invalid message data");

    var message = new Message
    {
        ConversationId = conversationId,
        SenderId = senderId,
        Text = text,
        CreatedAt = DateTime.UtcNow
    };

    await _context.Messages.InsertOneAsync(message);

    // 🔥 IMPORTANT: ensure sender joins group
    await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);

    await Clients.Group(conversationId)
        .SendAsync("ReceiveMessage", message);
}

        // =========================
        // 👀 MARK MESSAGE AS READ
        // =========================
        public async Task MarkAsRead(string messageId, string conversationId)
        {
            var update = Builders<Message>.Update
                .Set(x => x.IsRead, true);

            await _context.Messages.UpdateOneAsync(
                x => x.Id == messageId,
                update
            );

            // ✅ notify only chat users
            await Clients.Group(conversationId)
                .SendAsync("MessageRead", messageId);
        }

        // =========================
        // ✍️ TYPING INDICATOR
        // =========================
        public async Task Typing(string conversationId)
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

            await Clients.Group(conversationId)
                .SendAsync("UserTyping", userId);
        }

        public async Task StopTyping(string conversationId)
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

            await Clients.Group(conversationId)
                .SendAsync("UserStoppedTyping", userId);
        }

        // =========================
        // 👥 JOIN CHAT ROOM
        // =========================
        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        // =========================
        // 🚪 LEAVE CHAT ROOM (optional)
        // =========================
        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }

        // =========================
        // 🟢 GET ONLINE USERS
        // =========================
        public List<string> GetOnlineUsers()
        {
            lock (OnlineUsers)
            {
                return OnlineUsers.Keys.ToList();
            }
        }
    }
}