using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProtrndWebAPI.Settings;

namespace ProtrndWebAPI.Services
{
    public class NotificationService : BaseService
    {
        public NotificationService(IOptions<DBSettings> options) : base(options) { }

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

        public async Task PromotionNotification(TokenClaims sender, Guid promotionId)
        {
            var message = Constants.Promoted;
            await _notificationsCollection.InsertOneAsync(Notification(Guid.Empty, sender.ID, message, "Transaction", promotionId));
            return;
        }

        public async Task SupportNotification(TokenClaims sender, Guid receiverId, Guid postId, int amount)
        {
            var message = sender.UserName + $" supported your post with ₦{amount}";
            await _notificationsCollection.InsertOneAsync(Notification(sender.ID, receiverId, message, "Transaction", postId));
            return;
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
