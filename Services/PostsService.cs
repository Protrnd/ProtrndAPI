using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProtrndWebAPI.Models.Payments;
using ProtrndWebAPI.Settings;

namespace ProtrndWebAPI.Services
{
    public class PostsService : BaseService
    {
        private readonly ProfileService _profileService;
        private readonly NotificationService _notificationService;

        public PostsService(IOptions<DBSettings> settings) : base(settings)
        {
            _profileService = new ProfileService(settings);
            _notificationService = new NotificationService(settings);
        }

        public async Task<List<Post>> GetAllPostsAsync()
        {
            return await _postsCollection.Find(Builders<Post>.Filter.Where(p => !p.Disabled)).ToListAsync();
        }

        public async Task<List<Post>> GetPagePostsAsync(int page)
        {
            return await _postsCollection.Find(Builders<Post>.Filter.Where(p => !p.Disabled)).Skip((page - 1) * 10)
                .Limit(10)
                .ToListAsync();
        }

        public async Task<bool> PromoteAsync(Profile profile, Promotion promotion)
        {
            promotion.Identifier = promotion.Id;
            promotion.ProfileId = profile.Id;
            try
            {
                await _promotionCollection.InsertOneAsync(promotion);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<long> SendGiftToPostAsync(Post post, int count, Guid userId)
        {
            if (post != null && post.AcceptGift)
            {
                var filter = Builders<Gift>.Filter.Where(g => g.ProfileId == userId && !g.Disabled);
                var gifts = await _giftsCollection.Find(filter).ToListAsync();
                long updateResult = 0;
                for (int i = 0; i < count; i++)
                {
                    var updateBuilder = Builders<Gift>.Update;
                    var update = updateBuilder.Set(g => g.ProfileId, post.ProfileId).Set(g => g.PostId, post.Identifier);
                    var updateOne = await _giftsCollection.UpdateOneAsync(filter, update);
                    updateResult += updateOne.ModifiedCount;
                }
                return updateResult;
            }
            return 0;
        }

        public async Task<List<Gift>> GetAllGiftOnPostAsync(Guid postId)
        {
            return await _giftsCollection.Find(Builders<Gift>.Filter.Where(s => s.PostId == postId)).ToListAsync();
        }

        public async Task<List<Profile>> GetGiftersAsync(Guid id)
        {
            var profiles = new List<Profile>();
            var giftNotifications = await _notificationService.GetGiftNotificationsByIdAsync(id.ToString());
            foreach (var notification in giftNotifications)
            {
                var sender = await _profileService.GetProfileByIdAsync(notification.SenderId);
                if (profiles.Find(p => p.Identifier == sender.Identifier) == null)
                    profiles.Add(sender);
            }
            return profiles;
        }

        public async Task<bool> AcceptGift(Guid id)
        {
            var filter = Builders<Post>.Filter.Eq(p => p.Identifier, id);
            var update = Builders<Post>.Update.Set(p => p.AcceptGift, true);
            var result = await _postsCollection.FindOneAndUpdateAsync(filter, update);
            if (result != null)
                return true;
            return false;
        }

        public async Task<Post?> AddPostAsync(Post upload)
        {
            try
            {
                upload.Identifier = upload.Id;
                await _postsCollection.InsertOneAsync(upload);
                return upload;
            }
            catch(Exception)
            {
                return null;
            }
        }

        public async Task<List<Like>> GetPostLikesAsync(Guid id)
        {
            return await _likeCollection.Find(Builders<Like>.Filter.Eq(l => l.UploadId, id)).ToListAsync();
        }

        public async Task<List<Promotion>> GetPromotionsAsync(Profile profile)
        {
            var location = profile.Location.Split(',');
            //30,000 naira paid means promotion is accessible by every user
            //location[0] = State
            //location[1] = City
            return await _promotionCollection.Find(Builders<Promotion>.Filter.Where(p => p.NextCharge <= DateTime.Now || !p.Disabled || p.Amount == 30000 || p.Audience.Where(a => a.State == location[0]).FirstOrDefault() != null || p.Audience.Where(a => a.Cities.Contains(location[1])).FirstOrDefault() != null)).ToListAsync();
        }

        public async Task<bool> AddLikeAsync(Like like)
        {
            var liked = await _likeCollection.Find(l => l.SenderId == like.SenderId && l.UploadId == like.UploadId).FirstOrDefaultAsync();
            if (liked == null)
            {
                await _likeCollection.InsertOneAsync(like);
                return true;
            }
            return false;
        }

        public async Task<bool> RemoveLike(Guid postId, Guid profileId)
        {
            var filter = Builders<Like>.Filter.Where(l => l.SenderId == profileId && l.UploadId == postId);
            var liked = await _likeCollection.Find(filter).FirstOrDefaultAsync();
            if (liked != null)
            {
                var result = await _likeCollection.DeleteOneAsync(filter);
                return result.DeletedCount > 0;
            }
            return false;
        }

        public async Task<int> GetLikesCountAsync(Guid id)
        {
            var likes = await GetPostLikesAsync(id);
            return likes.Count;
        }

        public async Task<Comment> InsertCommentAsync(Comment comment)
        {
            await _commentCollection.InsertOneAsync(comment);
            return comment;
        }

        public async Task<List<Comment>> GetCommentsAsync(Guid id)
        {
            return await _commentCollection.Find(Builders<Comment>.Filter.Eq<Guid>(c => c.PostId, id)).ToListAsync();
        }

        public async Task<Post?> GetSinglePostAsync(Guid id)
        {
            var post = await _postsCollection.Find(Builders<Post>.Filter.Where(p => p.Id == id && !p.Disabled)).FirstOrDefaultAsync();
            if (post == null)
                return null;
            return post;
        }

        public async Task<List<Post>> GetUserPostsAsync(Guid userId)
        {
            return await _postsCollection.Find(Builders<Post>.Filter.Where(p => p.ProfileId == userId && !p.Disabled)).SortBy(p => p.Time).ToListAsync();
        }

        public async Task<bool> DeletePostAsync(Guid postId, Guid profileId)
        {
            var filter = Builders<Post>.Filter.Where(p => p.Id == postId && p.ProfileId == profileId);
            var post = await _postsCollection.Find(filter).FirstOrDefaultAsync();
            if (post != null)
            {
                post.Disabled = true;
                var result = await _postsCollection.ReplaceOneAsync(filter, post);
                return result.ModifiedCount > 0;
            }
            return false;
        }

        public async Task<List<Post>> GetPostsInCategoryAsync(string category)
        {
            return await _postsCollection.Find(Builders<Post>.Filter.Where(p => p.Category.Contains(category))).ToListAsync();
        }    
    }
}
