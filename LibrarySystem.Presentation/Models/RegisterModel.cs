using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Presentation.Models;

public class RegisterModel
{
    [Required]
    [StringLength(maximumLength: 30, MinimumLength = 4)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(maximumLength: 30, MinimumLength = 4)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(maximumLength: 14, MinimumLength = 10)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(maximumLength: 50, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}
