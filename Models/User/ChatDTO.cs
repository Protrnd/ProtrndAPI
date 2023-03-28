namespace ProtrndWebAPI.Models.User
{
    public class ChatDTO
    {
        public Guid ReceiverId { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid PostId { get; set; } = Guid.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
