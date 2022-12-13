namespace ProtrndWebAPI.Models.Posts
{
    public class PostQuery
    {
        public int Page { get; set; } = 1;
        public string Word { get; set; } = string.Empty;
    }
}
