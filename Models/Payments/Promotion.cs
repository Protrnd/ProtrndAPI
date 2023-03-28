using MongoDB.Driver.Linq;
using System.ComponentModel.DataAnnotations;
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
        [JsonPropertyName("expirydate")]
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddWeeks(1);
        [JsonPropertyName("email")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [JsonPropertyName("chargeintervals")]
        public string ChargeIntervals { get; set; } = "week";
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
        [JsonPropertyName("audience")]
        public Location Audience { get; set; } = null!;
        [JsonPropertyName("disabled")]
        public bool Disabled { get; set; } = false;
    }
}
