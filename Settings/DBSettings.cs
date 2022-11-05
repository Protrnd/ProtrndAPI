namespace ProtrndWebAPI.Settings
{
    public class DBSettings
    {
        public string ConnectionURI { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string UserCollection { get; set; } = null!;
        public string PostsCollection { get; set; } = null!;
        public string CommentsCollection { get; set; } = null!;
        public string LikesCollection { get; set; } = null!;
        public string PromotionsCollection { get; set; } = null!;
        public string ProfilesCollection { get; set; } = null!;
        public string ChatsCollection { get; set; } = null!;
        public string CategoriesCollection { get; set; } = null!;
        public string TagsCollection { get; set; } = null!;
        public string FollowingsCollection { get; set; } = null!;
        public string NotificationsCollection { get; set; } = null!;
        public string TransactionsCollection { get; set; } = null!;
        public string FavoritesCollection { get; set; } = null!;
        public string GiftsCollection { get; set; } = null!;
        public string AccountDetailsCollection { get; set; } = null!;
    }
}
