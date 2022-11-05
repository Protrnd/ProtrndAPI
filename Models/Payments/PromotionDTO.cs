using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Payments
{
    public class PromotionDTO
    {
        [JsonPropertyName("ref")]
        public string Ref { get; set; } = string.Empty;
        public Guid ProfileId { get; set; }
        [JsonPropertyName("postid")]
        public Guid PostId { get; set; }
        [JsonPropertyName("bannerurl")]
        public string BannerUrl { get; set; } = string.Empty;
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
        [JsonPropertyName("audience")]
        public List<Location> Audience { get; set; } = null!;
    }
}
