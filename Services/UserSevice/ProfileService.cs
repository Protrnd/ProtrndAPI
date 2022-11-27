using MongoDB.Driver;
using Microsoft.Extensions.Options;
using ProtrndWebAPI.Settings;

namespace ProtrndWebAPI.Services.UserSevice
{
    public class ProfileService : BaseService
    {
        public ProfileService(IOptions<DBSettings> settings) : base(settings) { }

        public async Task<Profile?> GetProfileByIdAsync(Guid id)
        {
            return await _profileCollection.Find(Builders<Profile>.Filter.Where(profile => profile.Identifier == id && !profile.Disabled)).FirstOrDefaultAsync();
        }

        public async Task<Profile?> GetProfileByNameAsync(string name)
        {
            return await _profileCollection.Find(Builders<Profile>.Filter.Where(profile => profile.UserName == name && !profile.Disabled)).FirstOrDefaultAsync();
        }

        public async Task<List<Profile>> GetProfilesByNameAsync(string name)
        {
            return await _profileCollection.Find(Builders<Profile>.Filter.Where(profile => profile.UserName == name  || profile.UserName.StartsWith(name) || profile.UserName.Contains(name) && !profile.Disabled)).ToListAsync();
        }

        public async Task<Profile?> UpdateProfile(Profile user, Profile profile)
        {
            var profileName = await GetProfileByNameAsync(profile.UserName);
            if (profile.UserName != user.UserName && profileName != null)
                return null;
            user.UserName = profile.UserName;
            user.FullName = profile.FullName;
            user.Location = profile.Location;
            user.Phone = profile.Phone;
            user.BackgroundImageUrl = profile.BackgroundImageUrl;
            user.ProfileImage = profile.ProfileImage;

            var filter = Builders<Profile>.Filter.Eq(p => p.Identifier, user.Identifier);
            var updateQueryResult = await _profileCollection.ReplaceOneAsync(filter, user);
            if (updateQueryResult == null)
                return null;
            return user;
        }

        public async Task<bool> Follow(TokenClaims profile, Guid receiver)
        {
            if (profile != null)
            {
                var follow = await _followingsCollection.Find(follow => follow.SenderId == profile.ID && follow.ReceiverId == receiver && !profile.Disabled).FirstOrDefaultAsync();
                if (follow != null)
                    return false;
                try
                {
                    await _followingsCollection.InsertOneAsync(new Followings { SenderId = profile.ID, ReceiverId = receiver });
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }

        public async Task<bool> UnFollow(TokenClaims profile, Guid receiver)
        {
            if (profile != null)
            {
                var result = await _followingsCollection.DeleteOneAsync(Builders<Followings>.Filter.Where(f => f.SenderId == profile.ID && f.ReceiverId == receiver && !profile.Disabled));
                return result.DeletedCount > 0;
            }
            return false;
        }

        public async Task<List<Profile>> GetFollowersAsync(Guid id)
        {
            var followers = await _followingsCollection.Find(f => f.ReceiverId == id).ToListAsync();
            var followerProfiles = new List<Profile>();
            foreach (var follower in followers)
            {
                var profile = await GetProfileByIdAsync(follower.SenderId);
                if (profile != null)
                    followerProfiles.Add(profile);
            }
            return followerProfiles;
        }

        public async Task<List<Profile>> GetFollowings(Guid id)
        {
            var followings = await _followingsCollection.Find(Builders<Followings>.Filter.Where(f => f.SenderId == id)).ToListAsync();
            var followingProfiles = new List<Profile>();
            foreach (var following in followings)
            {
                var profile = await GetProfileByIdAsync(following.ReceiverId);
                if (profile != null)
                {
                    followingProfiles.Add(profile);
                }
            }
            return followingProfiles;
        }

        public async Task<bool> IsFollowing(Guid currentUserId, Guid id)
        {
            var isFollowing = await _followingsCollection.Find(Builders<Followings>.Filter.Where(f => f.SenderId == currentUserId && f.ReceiverId == id)).SingleOrDefaultAsync();
            return isFollowing != null;
        }

        public async Task<string> GetFollowerCount(Guid id)
        {
            var followers = await GetFollowersAsync(id);
            if (followers != null)
                return followers.Count.ToString();
            return "0";
        }

        public async Task<string> GetFollowingCount(Guid id)
        {
            var followings = await GetFollowings(id);
            if (followings != null)
                return followings.Count.ToString();
            return "0";
        }
    }
}
