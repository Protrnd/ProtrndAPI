using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Payments
{
    public class VerifyTransaction
    {
        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public object Type { get; set; } = null!;
    }
}
