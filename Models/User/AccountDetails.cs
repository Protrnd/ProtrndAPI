using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.User
{
    public class AccountDetails
    {
        public Guid Id { get; set; }
        [JsonPropertyName("accountnumber")]
        public string AccountNumber { set; get; } = string.Empty;
        [JsonPropertyName("profileid")]
        public Guid ProfileId { get; set; } = Guid.Empty;
        [JsonPropertyName("bankname")]
        public string BankName { get; set; } = string.Empty;
        [JsonPropertyName("accountname")]
        public string AccountName { get; set; } = string.Empty;
    }
}
