using System.Text.Json.Serialization;
namespace ProtrndWebAPI.Models
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("identifier")]
        public Guid Identifier { get; set; }
        [JsonPropertyName("receiverid")]
        public Guid ReceiverId { get; set; }
        [JsonPropertyName("senderid")]
        public Guid SenderId { get; set; } = Guid.Empty;
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("viewed")]
        public bool Viewed { get; set; } = false;
        [JsonPropertyName("time")]
        public DateTime Time { get; set; } = DateTime.Now;
    }
}
