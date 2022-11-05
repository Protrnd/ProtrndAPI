using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.User
{
    public class Followings
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("senderid")]
        public Guid SenderId { get; set; }
        [JsonPropertyName("receiverid")]
        public Guid ReceiverId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
