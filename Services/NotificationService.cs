using Microsoft.Extensions.Options;
using ProtrndWebAPI.Settings;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProtrndWebAPI.Services
{
    public class NotificationService : BaseService
    {
        public NotificationService(IOptions<DBSettings> options) : base(options) { }

        public async Task ChatNotification(Profile sender, Guid receiverId)
        {
            var message = sender.UserName + Constants.SentMessage;
            //await _notificationsCollection.InsertOneAsync(Notification(sender.Identifier, receiverId, message));
            return;
        }

        public async Task<bool> FollowNotification(TokenClaims sender, Guid receiverId)
        {
            try
            {
                var message = sender.UserName + Constants.StartedFollowing;
                await _notificationsCollection.InsertOneAsync(Notification(sender.ID, receiverId, message, "Profile", receiverId));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> LikeNotification(TokenClaims sender, Guid receiverId, Guid postId)
        {
            try
            {
                var message = sender.UserName + Constants.Liked;
                await _notificationsCollection.InsertOneAsync(Notification(sender.ID, receiverId, message, "Post", postId));
                return true;
            }
            catch (Exception)
            {
                return false;

            }
        }

        public async Task CommentNotification(TokenClaims sender, Guid receiverId, Guid postId)
        {
            var message = sender.UserName + Constants.Commented;
            await _notificationsCollection.InsertOneAsync(Notification(sender.ID, receiverId, message, "Post", postId));
            return;
        }

        public async Task SupportNotification(Profile sender, Guid receiverId)
        {
            var message = sender.UserName + Constants.Commented;
            //await _notificationsCollection.InsertOneAsync(Notification(sender.Identifier, receiverId, message));
            return;
        }

        public async Task<bool> SendGiftNotification(TokenClaims sender, Post post, long count)
        {
            try
            {
                var message = sender.UserName + $" sent {count} gift to your post: " + post.Identifier;
                //await _notificationsCollection.InsertOneAsync(Notification(sender.ID, post.ProfileId, message));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Notification>> GetNotificationsAsync(Guid id, int page)
        {
            return await _notificationsCollection.Find(Builders<Notification>.Filter.Where(n => n.ReceiverId == id)).SortBy(n => n.Time).Skip((page - 1) * 20)
                .SortByDescending(n => n.Time)
                .Limit(20)
                .ToListAsync();
        }

        public async Task<Notification> GetNotificationByIdAsync(Guid id)
        {
            return await _notificationsCollection.Find(Builders<Notification>.Filter.Where(n => n.Identifier == id)).SingleOrDefaultAsync();
        }

        public async Task<List<Notification>> GetGiftNotificationsByIdAsync(string id)
        {
            return await _notificationsCollection.Find(Builders<Notification>.Filter.Where(n => n.Message.Contains(id))).ToListAsync();
        }

        public async Task<bool> SetNotificationViewedAsync(Guid id)
        {
            var filter = Builders<Notification>.Filter.Eq(n => n.Identifier, id);
            var notification = await GetNotificationByIdAsync(id);
            notification.Viewed = true;
            var result = await _notificationsCollection.ReplaceOneAsync(filter, notification);
            return result.ModifiedCount > 0;
        }

        private static Notification Notification(Guid senderId, Guid receiverId, string message, string type, Guid itemId)
        {
            var notification = new Notification { SenderId = senderId, ReceiverId = receiverId, Message = message, Type = type, ItemId = itemId };
            notification.Identifier = notification.Id;
            return notification;
        }
    }
}
