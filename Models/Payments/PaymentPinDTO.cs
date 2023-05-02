using System.Text.Json.Serialization;

namespace ProtrndWebAPI.Models.Payments
{
    public class PaymentPinDTO
    {
        [JsonPropertyName("pin")]
        public string Pin { get; set; }
    }
}
