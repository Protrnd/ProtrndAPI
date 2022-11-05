namespace ProtrndWebAPI.Models.User
{
    public class Location
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Cities { get; set; } = null!;
    }
}
