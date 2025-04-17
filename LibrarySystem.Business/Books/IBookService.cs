using LibrarySystem.BusinessLogic.Books.DTOs;
using LibrarySystem.BusinessLogic.DTOs;
using LibrarySystem.Data.Books;
using LibrarySystem.Data.Results;

namespace LibrarySystem.BusinessLogic.Books;

public interface IBookService
{
    Task<bool> BorrowBookAsync(Guid userId, Guid bookId);
    Task CreateAsync(CreateBookDto input);
    Task<bool> DeleteAsync(Guid bookId);
    Task<BookDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<BookDto>> GetListAsync(GetPaginateListDto input);
    Task<PaginatedResponse<BookDto>?> GetUserBorrowingBooksAsync(Guid userId, GetPaginateListDto input);
    Task<IEnumerable<Borrowing>?> GetUserBorrowingsAsync(Guid userId);
    Task<bool> ReturnBookAsync(Guid userId, Guid borrowingId);
    Task<PaginatedResponse<BookDto>> SearchAsync(SearchBookDto input);
    Task<bool> UpdateAsync(UpdateBookDto input);
}
