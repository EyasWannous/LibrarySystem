using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Presentation.Models;

public class LoginModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(maximumLength: 50, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}
