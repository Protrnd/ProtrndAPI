using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.User
{
    public class WithdrawDTO
    {
        [JsonPropertyName("amount")]
        public int Amount { get; set; } = 0;
        [JsonPropertyName("account")]
        public AccountDetailsDTO Account { get; set; } = null!;
    }
}
