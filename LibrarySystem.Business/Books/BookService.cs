using LibrarySystem.BusinessLogic.Books.DTOs;
using LibrarySystem.BusinessLogic.DTOs;
using LibrarySystem.Data.Books;
using LibrarySystem.Data.Results;
using LibrarySystem.Data.Users;

namespace LibrarySystem.BusinessLogic.Books;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IUserRepository _userRepository;

    public BookService(IBookRepository bookRepository, IUserRepository userRepository)
    {
        _bookRepository = bookRepository;
        _userRepository = userRepository;
    }

    public async Task<BookDto?> GetByIdAsync(Guid id)
    {
        var result = await _bookRepository.GetByIdAsync(id);
        if (result is null)
            return null;

        return result.Map();
    }

    public async Task<PaginatedResponse<BookDto>> GetListAsync(GetPaginateListDto input)
    {
        var result = await _bookRepository.GetPaginateAsync(input.Skip, input.Take);

        return new PaginatedResponse<BookDto>(result.TotalCount, result.Items.Select(x => x.Map()));
    }

    public async Task<bool> BorrowBookAsync(Guid userId, Guid bookId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return false;

        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book is null)
            return false;

        try
        {
            await _bookRepository.BorrowBookAsync(userId, bookId);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ReturnBookAsync(Guid userId, Guid borrowingId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return false;

        var borrowings = await _bookRepository.GetUserBorrowingsAsync(userId);
        if (borrowings.Select(x => x.UserId).ToList().Contains(userId) is false)
            return false;

        try
        {
            await _bookRepository.ReturnBookAsync(userId, borrowingId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Borrowing>?> GetUserBorrowingsAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return null;

        return await _bookRepository.GetUserBorrowingsAsync(userId);
    }

    public async Task<PaginatedResponse<BookDto>> SearchAsync(SearchBookDto input)
    {
        var result = await _bookRepository.SearchAsync(input.Title, input.Author, input.ISBN, input.Skip, input.Take);

        return new PaginatedResponse<BookDto>(result.TotalCount, result.Items.Select(x => x.Map()));
    }

    public async Task<bool> DeleteAsync(Guid bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book is null)
            return false;

        await _bookRepository.DeleteAsync(bookId);

        return true;
    }

    public async Task<bool> UpdateAsync(UpdateBookDto input)
    {
        var book = await _bookRepository.GetByIdAsync(input.Id);
        if (book is null)
            return false;

        if (input.Title is not null)
            book.SetTitle(input.Title);

        if (input.Description is not null)
            book.SetDescription(input.Description);

        if (input.Author is not null)
            book.SetAuthor(input.Author);

        if (input.ISBN is not null)
            book.SetISBN(input.ISBN);

        await _bookRepository.UpdateAsync(book);

        return true;
    }

    public async Task CreateAsync(CreateBookDto input)
    {
        var newBook = new Book(Guid.NewGuid(), input.Description, input.Title, input.Author, input.ISBN);

        await _bookRepository.InsertAsync(newBook);
    }

    public async Task<PaginatedResponse<BookDto>?> GetUserBorrowingBooksAsync(Guid userId, GetPaginateListDto input)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return null;

        var result = await _bookRepository.GetUserBorrowingBooksAsync(userId, input.Skip, input.Take);

        return new PaginatedResponse<BookDto>(result.TotalCount, result.Items.Select(x => x.Map()));
    }

    public async Task<bool> GetIsBorrowedBookSpecificUserIdAsync(Guid userId, Guid bookId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return false;

        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book is null)
            return false;

        var borrowings = await _bookRepository.GetUserBorrowingsAsync(userId);

        return borrowings.Where(x => x.ReturnedAt is null).Select(x => x.BookId).ToList().Contains(bookId);
    }
}
