using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Payments
{
    public class FundsDTO
    {
        [JsonPropertyName("profileid")]
        public Guid ProfileId { get; set; } = Guid.Empty;
        [JsonPropertyName("fromid")]
        public Guid FromId { get; set; } = Guid.Empty;
        [JsonPropertyName("amount")]
        public double Amount { get; set; } = 0d;
        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;
    }
}
