using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.User
{
    public class Withdraw
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("amount")]
        public int Amount { get; set; } = 0;
        [JsonPropertyName("account")]
        public AccountDetailsDTO Account { get; set; } = null!;
        [JsonPropertyName("ref")]
        public string Ref { get; set; } = string.Empty;
        [JsonPropertyName("status")]
        public string Status { get; set; } = Constants.Pending;
        [JsonPropertyName("by")]
        public Guid By { get; set; }
        [JsonPropertyName("owner")]
        public Guid Owner { get; set; }
        [JsonPropertyName("created")]
        public DateTime Date { get; set; } = DateTime.Now;
        [JsonPropertyName("completed")]
        public DateTime Completed { get; set; } = DateTime.Now;
    }
}
