using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.User
{
    public class Chat
    {
        public Guid Id { get; set; }
        [JsonPropertyName("senderid")]
        public Guid SenderId { get; set; }
        [JsonPropertyName("receiverid")]
        public Guid ReceiverId { get; set; }
        [JsonPropertyName("time")]
        public DateTime Time { get; set; } = DateTime.Now;
        [JsonPropertyName("seen")]
        public bool Seen { get; set; } = false;
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("itemid")]
        public Guid? ItemId { get; set; } = null;
        [JsonPropertyName("type")]
        public string Type { get; set; } = "chat";
    }
}
