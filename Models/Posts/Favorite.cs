using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Posts
{
    public class Favorite
    {
        public Guid Id { get; set; }
        [JsonPropertyName("identifier")]
        public Guid Identifier { get; set; }
        [JsonPropertyName("userid")]
        public Guid UserId { get; set; }
        [JsonPropertyName("postid")]
        public Guid PostId { get; set; }
        [JsonPropertyName("time")]
        public DateTime Time { get; set; } = DateTime.Now;
    }
}
