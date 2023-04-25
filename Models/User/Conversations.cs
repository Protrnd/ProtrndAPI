namespace ProtrndWebAPI.Models.User
{
    public class Conversations
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid Senderid { get; set; }
        public Guid ReceiverId { get; set; }
        public string RecentMessage { get; set; }
        public DateTime Time { get; set; } = DateTime.Now;
    }
}
