using System.Text.Json.Serialization;

namespace LibrarySystem.Data.Books;

public class Borrowing
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid BookId { get; private set; }
    public DateTime BorrowedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ReturnedAt { get; private set; }

    public Borrowing(Guid id, Guid userId, Guid bookId) 
    {
        Id = id;
        UserId = userId;
        BookId = bookId;
    }


    [JsonConstructor]
    public Borrowing(Guid id, Guid userId, Guid bookId, DateTime borrowedAt, DateTime? returnedAt)
    {
        Id = id;
        UserId = userId;
        BookId = bookId;
        ReturnedAt = borrowedAt;
        ReturnedAt = returnedAt;
    }

    public void SetBorrowedAt(DateTime borrowedAt) => BorrowedAt = borrowedAt;
    public void SetReturnedAt(DateTime returnedAt) => ReturnedAt = returnedAt;
}
