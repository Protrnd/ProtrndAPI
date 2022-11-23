namespace ProtrndWebAPI.Models.User
{
    public class VerifyOTPSalt
    {
        public string PlainText { get; set; } = string.Empty;
        public byte[]? OTPHash { get; set; } = null;
        public string Type { get; set; } = "cookie";
        public RegisterDTO RegisterDto { get; set; } = null!;
    }
}
