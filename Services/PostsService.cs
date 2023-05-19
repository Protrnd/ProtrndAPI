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
            return await _postsCollection.Find(Builders<Post>.Filter.Where(p => !p.Disabled))
                .SortByDescending(b => b.Time)
                .Skip((page - 1) * 10)
                .Limit(10)
                .ToListAsync();
        }

        public async Task<List<Post>> GetPostProfileTagsAsync(int page, Guid profileId)
        {
            return await _postsCollection.Find(Builders<Post>.Filter.Where(p => p.Tags.Contains(profileId) && !p.Disabled))
                .SortByDescending(t => t.Time)
                .Skip((page - 1) * 10)
                .Limit(10)
                .ToListAsync();
        }

        public async Task<List<Post>> GetPostQuery(PostQuery query)
        {
            return await _postsCollection.Find(Builders<Post>.Filter.Where(p => !p.Disabled && p.Caption.Contains(query.Word)))
                .SortByDescending(b => b.Time)
                .Skip((query.Page - 1) * 10)
                .Limit(10)
                .ToListAsync();
        }

        public async Task<long> GetQueryCount(string word)
        {
            return await _postsCollection.Find(Builders<Post>.Filter.Where(p => !p.Disabled & p.Caption.Contains(word)))
                .CountDocumentsAsync();
        }

        public async Task<List<Promotion>> GetPromotionsPaginatedAsync(int page, TokenClaims profile)
        {
            var profileDetail = await _profileService.GetProfileByIdAsync(profile.ID);
            if (profileDetail.Location == null)
                return new List<Promotion>();
            var location = profileDetail.Location.Split(',');
            //30,000 naira paid means promotion is accessible by every user
            //location[0] = State
            //location[1] = City
            return await _promotionCollection
                .Find(Builders<Promotion>.Filter
                .Where(p => p.ExpiryDate <= DateTime.Now || !p.Disabled || p.Amount == 30000 || p.Audience.State == location[0] || p.Audience.City == location[1]))
                .SortByDescending(p => p.Views)
                .Skip((page - 1) * 15)
                .Limit(15)
                .ToListAsync();
        }

        public async Task<bool> PromoteAsync(Promotion promotion)
        {
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

        public async Task<int> GetViewCountAsync(Guid promoId)
        {
            var views = await _viewClickCollection.Find(v => v.PromoId == promoId && v.Viewed).ToListAsync();
            return views.Count;
        }

        public async Task<int> GetClickCountAsync(Guid promoId)
        {
            var clicks = await _viewClickCollection.Find(v => v.PromoId == promoId && v.Clicked).ToListAsync();
            return clicks.Count;
        }

        public async Task View(ViewClick view)
        {
            var viewed = await _viewClickCollection.Find(v => v.PromoId == view.PromoId && v.ProfileId == view.ProfileId && !v.Viewed).SingleOrDefaultAsync();
            if (viewed == null)
            {
                view.Id = Guid.NewGuid();
                await _viewClickCollection.InsertOneAsync(view);
                var viewCount = await GetViewCountAsync(view.PromoId);
                var update = Builders<Promotion>.Update.Set(p => p.Views, viewCount);
                var filter = Builders<Promotion>.Filter.Where(p => p.Id == view.PromoId);
                await _promotionCollection.UpdateOneAsync(filter, update);
            }
        }

        public async Task Click(ViewClick click)
        {
            var clickFilter = Builders<ViewClick>.Filter.Where(v => v.PromoId == click.PromoId && v.ProfileId == click.ProfileId && !v.Clicked);
            var clicked = await _viewClickCollection.Find(clickFilter).SingleOrDefaultAsync();
            if (clicked == null)
            {
                click.Id = Guid.NewGuid();
                var viewCount = await GetClickCountAsync(click.PromoId);
                var update = Builders<Promotion>.Update.Set(p => p.Clicks, viewCount);
                var filter = Builders<Promotion>.Filter.Where(p => p.Id == click.PromoId);
                await _promotionCollection.UpdateOneAsync(filter, update);
                var clickUpdate = Builders<ViewClick>.Update.Set(v => v.Clicked, true);
                await _viewClickCollection.UpdateOneAsync(clickFilter, clickUpdate);
            }
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
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Like>> GetPostLikesAsync(Guid id)
        {
            return await _likeCollection.Find(Builders<Like>.Filter.Eq(l => l.UploadId, id)).ToListAsync();
        }

        public async Task<List<Promotion>> GetPromotionsAsync(TokenClaims profile)
        {
            var profileDetail = await _profileService.GetProfileByIdAsync(profile.ID);
            if (profileDetail.Location == null)
                return new List<Promotion>();
            var location = profileDetail.Location.Split(',');
            //30,000 naira paid means promotion is accessible by every user for 1 month
            //10,000 naira paid means promotion is accessible by every user for 1 month
            //location[0] = State
            //location[1] = City
            return await _promotionCollection.Find(Builders<Promotion>.Filter.Where(p => p.ExpiryDate <= DateTime.Now || !p.Disabled || p.Amount == 30000 || p.Amount == 10000 || p.Audience.State == location[0] || p.Audience.City == location[1]))
                .SortByDescending(p => p.Views)
                .ToListAsync();
        }

        public async Task<bool> AddLikeAsync(Like likeDto)
        {
            var liked = await _likeCollection.Find(l => l.SenderId == likeDto.SenderId && l.UploadId == likeDto.UploadId).FirstOrDefaultAsync();
            if (liked == null)
            {
                await _likeCollection.InsertOneAsync(new Like { UploadId = likeDto.UploadId, SenderId = likeDto.SenderId });
                return true;
            }
            return false;
        }

        public async Task<bool> IsLikedAsync(LikeDTO likeDto)
        {
            var liked = await _likeCollection.Find(l => l.SenderId == likeDto.SenderId && l.UploadId == likeDto.UploadId).FirstOrDefaultAsync();
            if (liked == null)
            {
                return false;
            }
            return true;
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
            return await _commentCollection.Find(Builders<Comment>.Filter.Eq<Guid>(c => c.PostId, id)).SortBy(c => c.Time).SortByDescending(c => c.Time).ToListAsync();
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
            return await _postsCollection.Find(Builders<Post>.Filter.Where(p => p.ProfileId == userId && !p.Disabled)).SortBy(p => p.Time).SortByDescending(b => b.Time).ToListAsync();
        }

        public async Task<bool> DeletePostAsync(Guid postId, Guid profileId)
        {
            var filter = Builders<Post>.Filter.Where(p => p.Id == postId && p.ProfileId == profileId);
            var update = Builders<Post>.Update.Set(p => p.Disabled, true);
            var result = await _postsCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
    }
}
