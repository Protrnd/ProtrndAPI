using Microsoft.Extensions.Options;
using ProtrndWebAPI.Settings;
using MongoDB.Driver;

namespace ProtrndWebAPI.Services
{
    public class CategoriesService : BaseService
    {
        public CategoriesService(IOptions<DBSettings> settings) : base(settings) { }

        public async Task<Category> AddCategoryAsync(string name)
        {
            var category = await GetSingleCategory(name);
            if (category != null)
            {
                return category;
            }
            category = new Category { Name = name.ToLower() };
            await _categoriesCollection.InsertOneAsync(category);
            return category;
        }

        public async Task<Category?> GetSingleCategory(string name)
        {
            var category = await _categoriesCollection.Find(Builders<Category>.Filter.Where(category => category.Name == name.ToLower())).FirstOrDefaultAsync();
            if (category == null)
                return null;
            return category;
        }

        public async Task<List<Category>> GetCategoriesAsync(string name)
        {
            return await _categoriesCollection.Find(Builders<Category>.Filter.Where(category => category.Name.Contains(name.ToLower()))).ToListAsync();
        }
    }
}
