using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Payments
{
    public class Funds
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("amount")]
        public double Amount { get; set; }
        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;
        [JsonPropertyName("profileid")]
        public Guid ProfileId { get; set; } = Guid.Empty;
        [JsonPropertyName("time")]
        public DateTime Time { get; set; } = DateTime.Now;
    }
}
