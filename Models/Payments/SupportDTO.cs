namespace ProtrndWebAPI.Models.Payments
{
    public class SupportDTO
    {
        public int Amount { get; set; }
        public Guid ReceiverId { get; set; } = Guid.Empty;
        public string Reference { get; set; } = string.Empty;
        public Guid PostId { get; set; } = Guid.Empty;
    }
}
