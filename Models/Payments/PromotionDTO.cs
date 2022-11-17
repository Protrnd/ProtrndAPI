namespace ProtrndWebAPI.Models.Payments
{
    public class PromotionDTO
    {
        public string Ref { get; set; } = string.Empty;
        public Guid ProfileId { get; set; }
        public Guid PostId { get; set; }
        public string BannerUrl { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public int Amount { get; set; }
        public List<Location>? Audience { get; set; } = null;
        public string AuthCode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new();
        public string ChargeIntervals { get; internal set; } = "day";
    }
}
