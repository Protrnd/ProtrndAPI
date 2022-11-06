namespace ProtrndWebAPI.Models.User
{
    public class Location
    {
        public Guid Id { get; set; }
        public string State { get; set; } = string.Empty;
        public List<string> Cities { get; set; } = null!;
    }
}
