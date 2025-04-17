using LibrarySystem.Data.Results;

namespace LibrarySystem.Data.Books;

public interface IBookRepository
{
    Task InsertAsync(Book entity);
    Task<int> CountAsync();
    Task DeleteAsync(Guid id);
    Task<Book?> GetByIdAsync(Guid id);
    Task<IEnumerable<Book>> GetListAsync();
    Task<PaginatedResponse<Book>> GetPaginateAsync(int skip = 0, int take = 10);
    Task<PaginatedResponse<Book>> SearchAsync(
        string? title = null,
        string? author = null,
        string? isbn = null,
        int skip = 0,
        int take = 10
    );
    Task UpdateAsync(Book entity);

    Task<Borrowing> BorrowBookAsync(Guid userId, Guid bookId);
    Task ReturnBookAsync(Guid userId, Guid borrowingId);
    Task<IEnumerable<Borrowing>> GetUserBorrowingsAsync(Guid userId);
    Task<PaginatedResponse<Book>> GetUserBorrowingBooksAsync(Guid userId, int skip = 0, int take = 10);
}
