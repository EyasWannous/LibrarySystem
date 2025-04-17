namespace LibrarySystem.Data.Books;

public class Book
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Author { get; private set; }
    public string ISBN { get; private set; }
    public bool IsBorrowed { get; private set; } = false;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public bool IsDeleted { get; private set; } = false;
    
    public Book(Guid id, string description, string title, string author, string isbn)
    {
        Id = id;
        Description = description;
        Title = title;
        Author = author;
        ISBN = isbn;
    }

    public void SetTitle(string title) => Title = title;
    public void SetDescription(string description) => Description = description;
    public void SetAuthor(string author) => Author = author;
    public void SetISBN(string isbn) => ISBN = isbn;
    public void SetIsBorrowed(bool isBorrowed) => IsBorrowed = isBorrowed;
    public void SetCreatedAt(DateTime createdAt) => CreatedAt = createdAt;

}
