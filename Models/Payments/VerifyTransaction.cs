using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Payments
{
    public class VerifyPromotionTransaction
    {
        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;
        [JsonPropertyName("promotion")]
        public PromotionDTO Promotion { get; set; } = null!;
    }
}
