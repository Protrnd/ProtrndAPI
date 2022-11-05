using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Posts
{
    public class PostDTO
    {
        [JsonPropertyName("caption")]
        [Required(ErrorMessage = "Field cannot be empty")]
        public string Caption { get; set; } = string.Empty;

        [JsonPropertyName("uploadurls")]
        public List<string> UploadUrls { get; set; } = new List<string>();

        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public List<string> Category { get; set; } = new List<string>();
    }
}
