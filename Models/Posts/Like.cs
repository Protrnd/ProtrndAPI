using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Posts
{
    public class Like
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("senderid")]
        public Guid SenderId { get; set; }
        [JsonPropertyName("uploadid")]
        public Guid UploadId { get; set; }
        [JsonPropertyName("time")]
        public DateTime Time { get; set; } = DateTime.Now;
    }
}
