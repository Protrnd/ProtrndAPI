namespace ProtrndWebAPI.Models.Payments
{
    public class AccountDetailsDTO
    {
        public string CardNumber { get; set; } = string.Empty;
        public string ExpirtyDate { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public Guid ProfileId { get; set; }
    }
}
