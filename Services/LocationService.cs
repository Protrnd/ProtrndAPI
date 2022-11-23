using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProtrndWebAPI.Settings;

namespace ProtrndWebAPI.Services
{
    public class LocationService : BaseService
    {
        public LocationService(IOptions<DBSettings> settings) : base(settings) { }

        public async Task<Location?> AddLocationAsync(LocationDTO locationDto)
        {
            var location = new Location { State = locationDto.State, Cities = locationDto.Cities };
            var filter = Builders<Location>.Filter.Eq(l => l.State, location.State);
            if (filter == null)
                return null;
            var locationExists = await _locationCollection.Find(filter).SingleOrDefaultAsync();
            if (locationExists == null)
            {
                await _locationCollection.InsertOneAsync(location);
                return location;
            }
            else if (locationExists.Cities.Count > 0)
            {
                foreach (var city in location.Cities)
                {
                    var exists = locationExists.Cities.Contains(city);
                    if (!exists)
                        locationExists.Cities.Add(city);
                }

            }
            
            return await _locationCollection.FindOneAndReplaceAsync(filter, locationExists);
        }

        public async Task<List<Location>> GetLocations()
        {
            return await _locationCollection.Find(_ => true).ToListAsync();
        }
    }
}
