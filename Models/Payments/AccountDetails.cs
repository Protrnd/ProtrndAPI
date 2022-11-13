using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Payments
{
    public class AccountDetails
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("cardnumber")]
        public string CardNumber { get; set; } = string.Empty;
        [JsonPropertyName("expirydate")]
        public string ExpirtyDate { get; set; } = string.Empty;
        [JsonPropertyName("accountnumber")]
        public string AccountNumber { get; set; } = string.Empty;
        [JsonPropertyName("cvv")]
        public string CVV { get; set; } = string.Empty;
        [JsonPropertyName("profileid")]
        public Guid ProfileId { get; set; }
        [JsonPropertyName("authcode")]
        public string AuthCode { get; set; } = string.Empty;
    }
}
