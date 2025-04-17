using LibrarySystem.BusinessLogic.Books.DTOs;
using LibrarySystem.Data.Books;

namespace LibrarySystem.BusinessLogic;

public static class Mapper
{
    public static BookDto Map(this Book book)
    {
        return new BookDto
        {
            Id = book.Id,
            ISBN = book.ISBN,
            Author = book.Author,
            Title = book.Title,
            Description = book.Description,
            IsBorrowed = book.IsBorrowed,
            CreatedAt = book.CreatedAt,
        };
    }
}
