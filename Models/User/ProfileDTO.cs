using System.ComponentModel.DataAnnotations;

namespace ProtrndWebAPI.Models.User
{
    public class ProfileDTO
    {
        [Required]
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string BackgroundImageUrl { get; set; } = string.Empty;
        public string? Location { get; set; } = string.Empty;
        public string About { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = string.Empty;
    }
}
