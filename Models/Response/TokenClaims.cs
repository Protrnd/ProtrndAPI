namespace ProtrndWebAPI.Models.Response
{
    public class TokenClaims
    {
        public Guid ID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool Disabled { get; set; } = false;
    }
}
