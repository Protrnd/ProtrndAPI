namespace ProtrndWebAPI.Models.Payments
{
    public class Revenue
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProfileId { get; set; }
        public double Amount { get; set; } = 0.0;
        public DateTime Time { get; set; } = DateTime.Now;
    }
}
