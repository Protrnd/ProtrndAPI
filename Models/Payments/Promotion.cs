using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Payments
{
    public class Promotion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("identifier")]
        public Guid Identifier { get; set; }
        [JsonPropertyName("profileid")]
        public Guid ProfileId { get; set; }
        [JsonPropertyName("postid")]
        public Guid PostId { get; set; }
        [JsonPropertyName("bannerurl")]
        public string BannerUrl { get; set; } = string.Empty;
        [JsonPropertyName("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [JsonPropertyName("expireat")]
        public DateTime ExpireAt { get; set; } = DateTime.Now.AddDays(7);
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
        [JsonPropertyName("audience")]
        public List<Location> Audience { get; set; } = null!;
        [JsonPropertyName("disabled")]
        public bool Disabled { get; set; } = false;
    }
}
