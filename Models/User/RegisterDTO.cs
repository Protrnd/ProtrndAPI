using System.ComponentModel.DataAnnotations;

namespace ProtrndWebAPI.Models.User
{
    public class RegisterDTO

    {
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password field can't be empty")]
        public string Password { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
    }
}
