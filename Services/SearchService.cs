using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProtrndWebAPI.Settings;

namespace ProtrndWebAPI.Services
{
    public class SearchService : BaseService
    {
        public SearchService(IOptions<DBSettings> settings) : base(settings) { }

        public async Task<object> GetSearchResultAsync(string search)
        {
            var posts = await SearchPostsByNameAsync(search);
            string? postsCount;
            if (posts == null)
                postsCount = "0";
            else
                postsCount = FormatNumber(posts.Count);
            var people = await SearchProfilesByNameAsync(search);
            string? peopleCount;
            if (people == null)
                peopleCount = "0";
            else
                peopleCount = FormatNumber(people.Count);

            return new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = new List<List<string>> { SearchCountResult(postsCount, "Posts"), SearchCountResult(peopleCount, "People") } };
        }

        public async Task<List<Post>> SearchPostsByNameAsync(string name)
        {
            return await _postsCollection.Find(Builders<Post>.Filter.Where(post => post.Caption.ToLower().Contains(name.ToLower()))).ToListAsync();
        }

        public async Task<List<Profile>> SearchProfilesByNameAsync(string name)
        {
            return await _profileCollection.Find(Builders<Profile>.Filter.Where(profile => profile.UserName.Contains(name.ToLower()) || profile.FullName.Contains(name) && profile.Disabled == false)).ToListAsync();
        }

        public async Task<List<Profile>> SearchProfilesByEmailAsync(string email)
        {
            return await _profileCollection.Find(Builders<Profile>.Filter.Where(profile => profile.Email.Contains(email.ToLower()) && profile.Disabled == false)).ToListAsync();
        }

        private static List<string> SearchCountResult(string count, string category)
        {
            return new List<string> { count, category };
        }
    }
}
