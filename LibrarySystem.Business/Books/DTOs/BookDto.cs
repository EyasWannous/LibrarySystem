namespace LibrarySystem.BusinessLogic.Books.DTOs;

public class BookDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public bool IsBorrowed { get; set; }
    public DateTime CreatedAt { get; set; }
}
