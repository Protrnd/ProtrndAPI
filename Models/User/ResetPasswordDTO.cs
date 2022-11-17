namespace ProtrndWebAPI.Models.User
{
    public class ResetPasswordDTO
    {
        public string PlainText { get; set; } = string.Empty;
        public byte[] OTPHash { get; set; }
        public Login Reset { get; set; } = null!;
    }
}
