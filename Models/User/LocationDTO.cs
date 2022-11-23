namespace ProtrndWebAPI.Models.User
{
    public class LocationDTO
    {
        public string State { get; set; } = string.Empty;
        public List<string> Cities { get; set; } = null!;
    }
}
