using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Payments
{
    public class Support
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("identifier")]
        public Guid Identifier { get; set; } = Guid.Empty;
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
        [JsonPropertyName("postid")]
        public Guid PostId { get; set; } = Guid.Empty;
        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;
        [JsonPropertyName("senderid")]
        public Guid SenderId { get; set; } = Guid.Empty;
        [JsonPropertyName("receiverid")]
        public Guid ReceiverId { get; set; } = Guid.Empty;
        [JsonPropertyName("time")]
        public DateTime Time { get; set; } = DateTime.Now;
    }
}
