using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.BusinessLogic.Books.DTOs;

public class UpdateBookDto
{
    [Required]
    public Guid Id { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string? ISBN { get; set; }
}
