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
        [JsonPropertyName("nextcharge")]
        public DateTime NextCharge { get; set; } = DateTime.Now.AddMinutes(10);
        [JsonPropertyName("useremail")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [JsonPropertyName("chargeintervals")]
        public string ChargeIntervals { get; set; } = "day";
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
        [JsonPropertyName("audience")]
        public List<Location> Audience { get; set; } = null!;
        [JsonPropertyName("disabled")]
        public bool Disabled { get; set; } = false;
        [JsonPropertyName("authcode")]
        public string AuthCode { get; set; } = string.Empty;
    }
}
