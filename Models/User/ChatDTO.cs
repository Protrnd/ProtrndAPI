using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.User
{
    public class ChatDTO
    {
        [JsonPropertyName("receiverid")]
        public Guid ReceiverId { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("itemid")]
        public Guid ItemId { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
}
