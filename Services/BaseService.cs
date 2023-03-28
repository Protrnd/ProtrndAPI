using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProtrndWebAPI.Models.Payments;
using ProtrndWebAPI.Models.User;
using ProtrndWebAPI.Settings;
using Tag = ProtrndWebAPI.Models.Posts.Tag;

namespace ProtrndWebAPI.Services
{
    public class BaseService
    {
        public readonly IMongoDatabase Database;
        public readonly IMongoCollection<Post> _postsCollection;
        public readonly IMongoCollection<Profile> _profileCollection;
        public readonly IMongoCollection<Register> _registrationCollection;
        public readonly IMongoCollection<Like> _likeCollection;
        public readonly IMongoCollection<Comment> _commentCollection;
        public readonly IMongoCollection<Tag> _tagsCollection;
        public readonly IMongoCollection<Followings> _followingsCollection;
        public readonly IMongoCollection<Notification> _notificationsCollection;
        public readonly IMongoCollection<Promotion> _promotionCollection;
        public readonly IMongoCollection<Transaction> _transactionCollection;
        public readonly IMongoCollection<Favorite> _favoriteCollection;
        public readonly IMongoCollection<AccountDetails> _accountDetailsCollection;
        public readonly IMongoCollection<Support> _supportCollection;
        public readonly IMongoCollection<Chat> _chatCollection;
        public readonly IMongoCollection<Conversations> _conversationsCollection;

        public BaseService(IOptions<DBSettings> settings)
        {
            MongoClient client = new(settings.Value.TestConnectionURI); //TODO: Change back to ConnectionURI
            Database = client.GetDatabase(settings.Value.DatabaseName);
            _postsCollection = Database.GetCollection<Post>(settings.Value.PostsCollection);
            _likeCollection = Database.GetCollection<Like>(settings.Value.LikesCollection);
            _commentCollection = Database.GetCollection<Comment>(settings.Value.CommentsCollection);
            _registrationCollection = Database.GetCollection<Register>(settings.Value.UserCollection);
            _profileCollection = Database.GetCollection<Profile>(settings.Value.ProfilesCollection);
            _tagsCollection = Database.GetCollection<Tag>(settings.Value.TagsCollection);
            _followingsCollection = Database.GetCollection<Followings>(settings.Value.FollowingsCollection);
            _notificationsCollection = Database.GetCollection<Notification>(settings.Value.NotificationsCollection);
            _promotionCollection = Database.GetCollection<Promotion>(settings.Value.PromotionsCollection);
            _transactionCollection = Database.GetCollection<Transaction>(settings.Value.TransactionsCollection);
            _favoriteCollection = Database.GetCollection<Favorite>(settings.Value.FavoritesCollection);
            _accountDetailsCollection = Database.GetCollection<AccountDetails>(settings.Value.AccountDetailsCollection);
            _supportCollection = Database.GetCollection<Support>(settings.Value.SupportCollection);
            _chatCollection = Database.GetCollection<Chat>(settings.Value.ChatCollection);
            _conversationsCollection = Database.GetCollection<Conversations>(settings.Value.ConversationsCollection);
        }

        public static string FormatNumber(int number)
        {
            var numberInString = number.ToString();
            if (numberInString.Length < 4)
                return numberInString;
            return Result(numberInString);
        }

        public static string Result(string numberString)
        {
            string? returnResult;
            if (numberString.Length % 3 == 0)
                returnResult = $"{numberString[..3]}.{numberString[3]}";
            else
                returnResult = $"{numberString[..2]}.{numberString[2]}";

            if (numberString.Length == 4 || numberString.Length == 7 || numberString.Length == 10)
                returnResult = $"{numberString[0]}.{numberString[1]}";

            if (numberString.Length >= 4 && numberString.Length < 7)
                return returnResult + "K";
            else if (numberString.Length >= 7 && numberString.Length < 10)
                return returnResult + "M";
            else
                return returnResult + "B";
        }

        public static int Generate()
        {
            Random r = new((int)DateTime.Now.Ticks);
            return r.Next(100000000, 999999999);
        }
    }
}
