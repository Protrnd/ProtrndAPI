using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Payments
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("identifier")]
        public Guid Identifier { get; set; }

        [JsonPropertyName("profileid")]
        public Guid ProfileId { get; set; }

        [JsonPropertyName("receiverid")]
        public Guid ReceiverId { get; set; }

        [JsonPropertyName("itemid")]
        public Guid ItemId { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("purpose")]
        public string Purpose { get; set; }

        [JsonPropertyName("trxref")]
        public string TrxRef { get; set; } = string.Empty;

        [JsonPropertyName("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
