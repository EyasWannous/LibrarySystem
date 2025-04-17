using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.BusinessLogic.Books.DTOs;

public class UpdateBookDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [StringLength(maximumLength: 100, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(maximumLength: 500, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(maximumLength: 50, MinimumLength = 3)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [StringLength(maximumLength: 13, MinimumLength = 10)]
    public string ISBN { get; set; } = string.Empty;
}
