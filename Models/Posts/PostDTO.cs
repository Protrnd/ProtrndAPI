using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Posts
{
    public class PostDTO
    {
        [JsonPropertyName("caption")]
        public string Caption { get; set; } = string.Empty;

        [JsonPropertyName("uploadurls")]
        public List<string> UploadUrls { get; set; } = new List<string>();

        [JsonPropertyName("location")]
        public Location Location { get; set; } = null!;
    }
}
