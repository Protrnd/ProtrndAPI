namespace ProtrndWebAPI.Models.Payments
{
    public class PromotionDTO
    {
        public Guid ProfileId { get; set; }
        public Guid PostId { get; set; }
        public string BannerUrl { get; set; } = string.Empty;
        public int Amount { get; set; }
        public LocationDTO Audience { get; set; }
        public string Email { get; set; } = string.Empty;
        public string ChargeIntervals { get; set; } = "week";
    }
}
