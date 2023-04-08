using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Posts
{
    public class ProfileTagsQuery
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }
        [JsonPropertyName("profileid")]
        public Guid ProfileId { get; set; }
    }
}
