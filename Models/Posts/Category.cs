using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Posts
{
    public class Category
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
