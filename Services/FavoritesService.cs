using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProtrndWebAPI.Settings;

namespace ProtrndWebAPI.Services
{
    public class FavoritesService : BaseService
    {
        public FavoritesService(IOptions<DBSettings> options) : base(options) { }

        public async Task<object> AddToFavoritesAsync(Profile profile, Favorite favorite)
        {
            favorite.UserId = profile.Identifier;
            await _favoriteCollection.InsertOneAsync(favorite);
            return new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok };
        }

        public async Task<List<Favorite>> GetFavoritesAsync(Profile profile)
        {
            return await _favoriteCollection.Find(Builders<Favorite>.Filter.Where(f => f.UserId == profile.Identifier)).ToListAsync();
        }

        public async Task<Favorite> GetFavoriteByIdAsync(Guid id)
        {
            return await _favoriteCollection.Find(Builders<Favorite>.Filter.Where(f => f.Identifier == id)).SingleOrDefaultAsync();
        }

        public async Task<Favorite> GetFavoriteByPostIdAsync(Post post)
        {
            return await _favoriteCollection.Find(Builders<Favorite>.Filter.Where(f => f.PostId == post.Identifier)).SingleOrDefaultAsync();
        }

        public async Task<object> RemoveFromFavoritesAsync(Profile profile, Post post)
        {
            var filter = Builders<Favorite>.Filter.Where(f => f.PostId == post.Identifier && f.UserId == profile.Identifier);
            await _favoriteCollection.DeleteOneAsync(filter);
            return new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok };
        }
    }
}
