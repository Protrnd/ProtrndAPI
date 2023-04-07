using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Posts
{
    public class ViewClick
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("profileid")]
        public Guid ProfileId { get; set; }
        [JsonPropertyName("promoid")]
        public Guid PromoId { get; set; }
        [JsonPropertyName("viewed")]
        public bool Viewed { get; set; } = false;
        [JsonPropertyName("clicked")]
        public bool Clicked { get; set; } = false;
    }
}
