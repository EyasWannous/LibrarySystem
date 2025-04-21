using LibrarySystem.Data.Cache;
using LibrarySystem.Data.Results;
using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace LibrarySystem.Data.Books;

public class BookRepository : IBookRepository
{
    private readonly NpgsqlConnection _db;

    private readonly string EntityName = nameof(Book);
    private string AllKey => $"allOf_{EntityName}";
    private string PaginateKeyTag => $"paginate_{EntityName}_";
    private string KeyOfOneTag => $"oneOf_{EntityName}_Id: ";
    private string CountKey => $"count_{EntityName}";
    private string SearchTag => $"search_{EntityName}";
    private string UserBorrowingsKeyTag => $"userBorrowings_";
    private string UserBorrowingBooksKeyTag => $"userBorrowingBooks_";

    private string MakeKeyOne(Guid id) => KeyOfOneTag + id.ToString();
    private string MakePaginateKey(int skip, int take) => PaginateKeyTag + "skip: " + skip + "_take: " + take;

    private string MakeSearchKey(string? title = null, string? author = null, string? isbn = null, int skip = 0, int take = 10) 
        => SearchTag + "_title: " + title + "_author: " + author + "_isbn: " + isbn + "_skip: " + skip + "_take: " + take;

    private string MakeUserBorrowingsKey(Guid userId) => UserBorrowingsKeyTag + userId;
    private string MakeUserBorrowingBooksKey(Guid userId, int skip, int take) 
        => UserBorrowingBooksKeyTag + userId + "_skip:" + skip + "_take:" + take;


    private readonly IHybridCacheService _hybridCache;

    public BookRepository(NpgsqlConnection db, IHybridCacheService hybridCache)
    {
        _db = db;
        _hybridCache = hybridCache;
    }

    public async Task<int> CountAsync()
    {
        return await _hybridCache.GetOrCreateAsync(
            CountKey,
            async _ => await CountDbAsync(),
            tags: [CountKey]
        );
    }

    private async Task<int> CountDbAsync()
    {
        const string sql = "SELECT COUNT(*) FROM books WHERE is_deleted = false";

        using var command = new NpgsqlCommand(sql, _db);
        await EnsureConnectionOpen();

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task DeleteAsync(Guid id)
    {
        await Task.WhenAll(
            _hybridCache.RemoveAsync(MakeKeyOne(id)).AsTask(),
            _hybridCache.RemoveByTagsAsync([
                AllKey,
                PaginateKeyTag,
                SearchTag,
                CountKey,
                UserBorrowingsKeyTag,
                UserBorrowingBooksKeyTag
            ]).AsTask()
        );

        await DeleteDbAsync(id);
    }

    private async Task DeleteDbAsync(Guid id)
    {

        const string sql = "UPDATE books SET is_deleted = true WHERE id = @id";

        using var command = new NpgsqlCommand(sql, _db);
        command.Parameters.AddWithValue("@id", id);

        await EnsureConnectionOpen();
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Book?> GetByIdAsync(Guid id)
    {
        var OneKey = MakeKeyOne(id);

        return await _hybridCache.GetOrCreateAsync(
            OneKey,
            async _ => await GetByIdDbAsync(id),
            tags: [KeyOfOneTag]
        );
    }

    private async Task<Book?> GetByIdDbAsync(Guid id)
    {
        const string sql = @"SELECT id, title, description, author, isbn, is_borrowed, created_at 
                           FROM books 
                           WHERE id = @id AND is_deleted = false";

        using var command = new NpgsqlCommand(sql, _db);
        command.Parameters.AddWithValue("@id", id);

        await EnsureConnectionOpen();

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var book = new Book(
                reader.GetGuid(0),
                reader.GetString(2),
                reader.GetString(1),
                reader.GetString(3),
                reader.GetString(4)
            );

            book.SetIsBorrowed(reader.GetBoolean(5));
            book.SetCreatedAt(reader.GetDateTime(6));

            return book;
        }

        return null;
    }

    public async Task<IEnumerable<Book>> GetListAsync()
    {
        return await _hybridCache.GetOrCreateAsync(
            AllKey,
            async _ => await GetListDbAsync(),
            tags: [AllKey]
        );
    }

    private async Task<IEnumerable<Book>> GetListDbAsync()
    {
        var books = new List<Book>();
        const string sql = @"SELECT id, title, description, author, isbn, is_borrowed, created_at 
                           FROM books 
                           WHERE is_deleted = false";

        using var command = new NpgsqlCommand(sql, _db);

        await EnsureConnectionOpen();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var book = new Book(
                reader.GetGuid(0),
                reader.GetString(2),
                reader.GetString(1),
                reader.GetString(3),
                reader.GetString(4)
            );

            book.SetIsBorrowed(reader.GetBoolean(5));
            book.SetCreatedAt(reader.GetDateTime(6));

            books.Add(book);
        }

        return books;
    }

    public async Task<PaginatedResponse<Book>> GetPaginateAsync(int skip = 0, int take = 10)
    {
        var paginateKey = MakePaginateKey(skip, take);

        return await _hybridCache.GetOrCreateAsync(
            paginateKey,
            async _ => await GetPaginateDbAsync(skip, take),
            tags: [PaginateKeyTag]
        );
    }

    private async Task<PaginatedResponse<Book>> GetPaginateDbAsync(int skip = 0, int take = 10)
    {
        var books = new List<Book>();
        const string sql = @"SELECT id, title, description, author, isbn, is_borrowed, created_at 
                           FROM books 
                           WHERE is_deleted = false 
                           ORDER BY created_at DESC 
                           LIMIT @take OFFSET @skip;
                           
                           SELECT COUNT(*) 
                           FROM books 
                           WHERE is_deleted = false";

        using var command = new NpgsqlCommand(sql, _db);
        command.Parameters.AddWithValue("@skip", skip);
        command.Parameters.AddWithValue("@take", take);

        await EnsureConnectionOpen();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var book = new Book(
                reader.GetGuid(0),
                reader.GetString(2),
                reader.GetString(1),
                reader.GetString(3),
                reader.GetString(4)
            );

            book.SetIsBorrowed(reader.GetBoolean(5));
            book.SetCreatedAt(reader.GetDateTime(6));

            books.Add(book);
        }

        await reader.NextResultAsync();
        var totalCount = await reader.ReadAsync() ? reader.GetInt32(0) : 0;

        return new PaginatedResponse<Book>(totalCount, books);
    }

    public async Task InsertAsync(Book entity)
    {
        await _hybridCache.RemoveByTagsAsync([
            AllKey,
            PaginateKeyTag,
            SearchTag,
            CountKey
        ]);

        await InsertDbAsync(entity);
    }

    private async Task InsertDbAsync(Book entity)
    {
        const string sql = @"INSERT INTO books 
                           (id, title, description, author, isbn, is_borrowed, created_at, is_deleted)
                           VALUES 
                           (@id, @title, @description, @author, @isbn, @isBorrowed, @createdAt, false)";

        using var command = new NpgsqlCommand(sql, _db);
        command.Parameters.AddWithValue("@id", entity.Id);
        command.Parameters.AddWithValue("@title", entity.Title);
        command.Parameters.AddWithValue("@description", entity.Description);
        command.Parameters.AddWithValue("@author", entity.Author);
        command.Parameters.AddWithValue("@isbn", entity.ISBN);
        command.Parameters.AddWithValue("@isBorrowed", entity.IsBorrowed);
        command.Parameters.AddWithValue("@createdAt", entity.CreatedAt);

        await EnsureConnectionOpen();
        await command.ExecuteNonQueryAsync();
    }

    public async Task<PaginatedResponse<Book>> SearchAsync(
        string? title = null,
        string? author = null,
        string? isbn = null,
        int skip = 0,
        int take = 10)
    {
        var seachKey = MakeSearchKey(title, author, isbn, skip, take);

        return await _hybridCache.GetOrCreateAsync(
            seachKey,
            async _ => await SearchDbAsync(title, author, isbn, skip, take),
            tags: [SearchTag]
        );
    }

    private async Task<PaginatedResponse<Book>> SearchDbAsync(
        string? title = null,
        string? author = null,
        string? isbn = null,
        int skip = 0,
        int take = 10)
    {
        var books = new List<Book>();

        const string sql = @"SELECT id, title, description, author, isbn, is_borrowed, created_at 
                       FROM books 
                       WHERE is_deleted = false 
                       AND (@title IS NULL OR title ILIKE @title)
                       AND (@author IS NULL OR author ILIKE @author)
                       AND (@isbn IS NULL OR isbn ILIKE @isbn)
                       ORDER BY created_at DESC 
                       LIMIT @take OFFSET @skip;
                       
                       SELECT COUNT(*) 
                       FROM books 
                       WHERE is_deleted = false 
                       AND (@title IS NULL OR title ILIKE @title)
                       AND (@author IS NULL OR author ILIKE @author)
                       AND (@isbn IS NULL OR isbn ILIKE @isbn)";

        await using var command = new NpgsqlCommand(sql, _db);

        command.Parameters.Add(new NpgsqlParameter("@title", NpgsqlDbType.Text)
        {
            Value = string.IsNullOrWhiteSpace(title) ? DBNull.Value : $"%{title}%"
        });

        command.Parameters.Add(new NpgsqlParameter("@author", NpgsqlDbType.Text)
        {
            Value = string.IsNullOrWhiteSpace(author) ? DBNull.Value : $"%{author}%"
        });

        command.Parameters.Add(new NpgsqlParameter("@isbn", NpgsqlDbType.Text)
        {
            Value = string.IsNullOrWhiteSpace(isbn) ? DBNull.Value : $"%{isbn}%"
        });

        command.Parameters.AddWithValue("@skip", skip);
        command.Parameters.AddWithValue("@take", take);

        await EnsureConnectionOpen();

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var book = new Book(
                reader.GetGuid(0),
                reader.GetString(2),
                reader.GetString(1),
                reader.GetString(3),
                reader.GetString(4)
            );

            book.SetIsBorrowed(reader.GetBoolean(5));
            book.SetCreatedAt(reader.GetDateTime(6));

            books.Add(book);
        }

        await reader.NextResultAsync();
        var totalCount = await reader.ReadAsync() ? reader.GetInt32(0) : 0;

        return new PaginatedResponse<Book>(totalCount, books);
    }

    public async Task UpdateAsync(Book entity)
    {
        await Task.WhenAll(
            _hybridCache.RemoveAsync(MakeKeyOne(entity.Id)).AsTask(),
            _hybridCache.RemoveByTagsAsync([
                AllKey,
                PaginateKeyTag,
                SearchTag,
                CountKey,
                UserBorrowingsKeyTag,
                UserBorrowingBooksKeyTag
            ]).AsTask()
        );

        await UpdateDbAsync(entity);
    }

    private async Task UpdateDbAsync(Book entity)
    {
        const string sql = @"UPDATE books 
                               SET title = @title, 
                                   description = @description, 
                                   author = @author, 
                                   isbn = @isbn, 
                                   is_borrowed = @isBorrowed
                               WHERE id = @id AND is_deleted = false";

        using var command = new NpgsqlCommand(sql, _db);
        command.Parameters.AddWithValue("@id", entity.Id);
        command.Parameters.AddWithValue("@title", entity.Title);
        command.Parameters.AddWithValue("@description", entity.Description);
        command.Parameters.AddWithValue("@author", entity.Author);
        command.Parameters.AddWithValue("@isbn", entity.ISBN);
        command.Parameters.AddWithValue("@isBorrowed", entity.IsBorrowed);

        await EnsureConnectionOpen();
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Borrowing> BorrowBookAsync(Guid userId, Guid bookId)
    {
        var result = await BorrowBookDbAsync(userId, bookId);

        await Task.WhenAll(
            _hybridCache.RemoveAsync(MakeKeyOne(bookId)).AsTask(),
            _hybridCache.RemoveByTagsAsync([
                AllKey,
                PaginateKeyTag,
                SearchTag,
                CountKey,
                UserBorrowingsKeyTag,
                UserBorrowingBooksKeyTag
            ]).AsTask()
        );

        return result;
    }

    private async Task<Borrowing> BorrowBookDbAsync(Guid userId, Guid bookId)
    {
        await using var transaction = await _db.BeginTransactionAsync();

        try
        {
            const string checkBookSql = @"SELECT is_borrowed FROM books 
                                    WHERE id = @bookId AND is_deleted = false FOR UPDATE";

            await using var checkCommand = new NpgsqlCommand(checkBookSql, _db, transaction);
            checkCommand.Parameters.AddWithValue("@bookId", bookId);

            await EnsureConnectionOpen();

            var isBorrowed = (bool?)await checkCommand.ExecuteScalarAsync();

            if (isBorrowed is null)
                throw new Exception("Book not found");

            if (isBorrowed is true)
                throw new Exception("Book is already borrowed");

            const string updateBookSql = "UPDATE books SET is_borrowed = true WHERE id = @bookId";

            await using var updateBookCommand = new NpgsqlCommand(updateBookSql, _db, transaction);
            updateBookCommand.Parameters.AddWithValue("@bookId", bookId);

            await updateBookCommand.ExecuteNonQueryAsync();

            var newBorrowing = new Borrowing(Guid.NewGuid(), userId, bookId);

            const string insertBorrowingSql = @"INSERT INTO borrowings 
                                          (id, user_id, book_id, borrowed_at, returned_at)
                                          VALUES 
                                          (@id, @userId, @bookId, @borrowedAt, NULL)";

            await using var insertCommand = new NpgsqlCommand(insertBorrowingSql, _db, transaction);
            insertCommand.Parameters.AddWithValue("@id", newBorrowing.Id);
            insertCommand.Parameters.AddWithValue("@userId", userId);
            insertCommand.Parameters.AddWithValue("@bookId", bookId);
            insertCommand.Parameters.AddWithValue("@borrowedAt", newBorrowing.BorrowedAt);

            await insertCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return newBorrowing;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task ReturnBookAsync(Guid userId, Guid borrowingId)
    {
        await _hybridCache.RemoveByTagsAsync([
            KeyOfOneTag,
            AllKey,
            PaginateKeyTag,
            SearchTag,
            CountKey,
            UserBorrowingsKeyTag,
            UserBorrowingBooksKeyTag
        ]);


        await ReturnBookDbAsync(userId, borrowingId);
    }
    
    private async Task ReturnBookDbAsync(Guid userId, Guid borrowingId)
    {
        await using var transaction = await _db.BeginTransactionAsync();

        try
        {
            const string getBorrowingSql = @"
                SELECT book_id
                FROM borrowings 
                WHERE id = @borrowingId 
                AND user_id = @userId
                AND returned_at IS NULL 
                FOR UPDATE";

            await using var getCommand = new NpgsqlCommand(getBorrowingSql, _db, transaction);
            getCommand.Parameters.AddWithValue("@borrowingId", borrowingId);
            getCommand.Parameters.AddWithValue("@userId", userId);

            await EnsureConnectionOpen();

            var bookId = (Guid?)await getCommand.ExecuteScalarAsync();

            if (bookId is null)
                throw new Exception("Borrowing record not found, already returned, or doesn't belong to user");

            const string updateBookSql = "UPDATE books SET is_borrowed = false WHERE id = @bookId";

            await using var updateBookCommand = new NpgsqlCommand(updateBookSql, _db, transaction);
            updateBookCommand.Parameters.AddWithValue("@bookId", bookId);

            await updateBookCommand.ExecuteNonQueryAsync();

            const string updateBorrowingSql = @"UPDATE borrowings 
                                                    SET returned_at = @returnedAt 
                                                    WHERE id = @borrowingId";

            await using var updateCommand = new NpgsqlCommand(updateBorrowingSql, _db, transaction);
            updateCommand.Parameters.AddWithValue("@borrowingId", borrowingId);
            updateCommand.Parameters.AddWithValue("@returnedAt", DateTime.UtcNow);

            await updateCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task<IEnumerable<Borrowing>> GetUserBorrowingsAsync(Guid userId)
    {
        var userBorrowingsKey = MakeUserBorrowingsKey(userId);

        return await _hybridCache.GetOrCreateAsync(
            userBorrowingsKey,
            async _ => await GetUserBorrowingsDbAsync(userId),
            tags: [UserBorrowingsKeyTag]
        );
    }

    private async Task<IEnumerable<Borrowing>> GetUserBorrowingsDbAsync(Guid userId)
    {
        var borrowings = new List<Borrowing>();
        const string sql = @"SELECT id, user_id, book_id, borrowed_at, returned_at 
                        FROM borrowings 
                        WHERE user_id = @userId
                        ORDER BY borrowed_at DESC";

        await using var command = new NpgsqlCommand(sql, _db);
        command.Parameters.AddWithValue("@userId", userId);

        await EnsureConnectionOpen();

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var borrowing = new Borrowing(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2)
            );

            borrowing.SetBorrowedAt(reader.GetDateTime(3));

            if (!reader.IsDBNull(4))
            {
                borrowing.SetReturnedAt(reader.GetDateTime(4));
            }

            borrowings.Add(borrowing);
        }

        return borrowings;
    }

    public async Task<PaginatedResponse<Book>> GetUserBorrowingBooksAsync(Guid userId, int skip = 0, int take = 10)
    {
        var userBorrowingBooksKey = MakeUserBorrowingBooksKey(userId, skip, take);

        return await _hybridCache.GetOrCreateAsync(
            userBorrowingBooksKey,
            async _ => await GetUserBorrowingBooksDbAsync(userId, skip, take),
            tags: [UserBorrowingBooksKeyTag]
        );
    }
    
    private async Task<PaginatedResponse<Book>> GetUserBorrowingBooksDbAsync(Guid userId, int skip = 0, int take = 0)
    {
        var books = new List<Book>();

        const string sql = @"SELECT
                                b.id AS book_id,
                                b.title,
                                b.description,
                                b.author,
                                b.isbn,
                                b.is_borrowed,
                                br.borrowed_at
                            FROM borrowings br
                            JOIN books b ON br.book_id = b.id
                            WHERE br.user_id = @userId AND b.is_borrowed
                            ORDER BY br.borrowed_at DESC
                            LIMIT @take OFFSET @skip;

                            SELECT COUNT(*) 
                            FROM borrowings br
                            JOIN books b ON br.book_id = b.id
                            WHERE user_id = @userId AND b.is_borrowed;
                            ";

        await using var command = new NpgsqlCommand(sql, _db);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@take", take);
        command.Parameters.AddWithValue("@skip", skip);

        await EnsureConnectionOpen();

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var borrowingBook = new Book(
                id: reader.GetGuid(0),
                title: reader.GetString(1),
                description: reader.GetString(2),
                author: reader.GetString(3),
                isbn: reader.GetString(4)
            );

            borrowingBook.SetIsBorrowed(reader.GetBoolean(5));

            books.Add(borrowingBook);
        }

        await reader.NextResultAsync();
        var totalCount = await reader.ReadAsync() ? reader.GetInt32(0) : 0;

        return new PaginatedResponse<Book>(totalCount, books);
    }

    private async Task EnsureConnectionOpen()
    {
        if (_db.State is not ConnectionState.Open)
            await _db.OpenAsync();
    }
}