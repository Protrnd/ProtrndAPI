namespace ProtrndWebAPI.Models.User
{
    public class AccountDetailsDTO
    {
        public string AccountNumber { set; get; } = string.Empty;
        public Guid ProfileId { get; set; } = Guid.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
    }
}
