using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.User
{
    public class Register
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("fullname")]
        public string FullName { get; set; } = string.Empty;
        [JsonPropertyName("username")]
        public string UserName { get; set; } = string.Empty;
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        [JsonPropertyName("phash")]
        public byte[] PasswordHash { get; set; } = null!;
        [JsonPropertyName("psalt")]
        public byte[] PasswordSalt { get; set; } = null!;
        [JsonPropertyName("accounttype")]
        public string AccountType { get; set; } = string.Empty;
        [JsonPropertyName("registrationdate")]
        public DateTime RegistrationDate { get; set; }
    }
}
