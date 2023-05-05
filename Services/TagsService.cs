using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProtrndWebAPI.Settings;
using Tag = ProtrndWebAPI.Models.Posts.Tag;

namespace ProtrndWebAPI.Services
{
    public class TagsService : BaseService
    {
        public TagsService(IOptions<DBSettings> settings) : base(settings) { }

        public async Task<List<Tag>?> GetTagsWithNameAsync(string name)
        {
            if (!name.StartsWith("#") || name.Contains(' '))
            {
                return null;
            }
            return await _tagsCollection.Find(Builders<Tag>.Filter.Where(t => t.Name.Contains(name.ToLower()))).ToListAsync();
        }

        public async Task<bool> AddTagAsync(string name)
        {
            if (!name.StartsWith("#") || name.Contains(' '))
            {
                return false;
            }
            var tag = await TagExists(name);
            if (tag != null)
                return false;
            await _tagsCollection.InsertOneAsync(new Tag { Name = name.ToLower() });
            return true;
        }

        private async Task<Tag?> TagExists(string name)
        {
            return await _tagsCollection.Find(t => t.Name.Equals(name.ToLower())).FirstOrDefaultAsync();
        }
    }
}
