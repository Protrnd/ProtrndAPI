using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Payments
{
    public class PaymentPin
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("pinhash")]
        public byte[] PaymentPinHash { get; set; }
        [JsonPropertyName("pinsalt")]
        public byte[] PaymentPinSalt { get; set; }
        [JsonPropertyName("updated")]
        public DateTime Updated { get; set; } = DateTime.Now;
        [JsonPropertyName("profileid")]
        public Guid ProfileId { get; set; }
    }
}
