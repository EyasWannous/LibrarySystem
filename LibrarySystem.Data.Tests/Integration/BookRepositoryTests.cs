//using LibrarySystem.Data.Books;
//using Npgsql;

//namespace LibrarySystem.Data.Tests.Integration;

//public class BookRepositoryTests : IAsyncLifetime
//{
//    private NpgsqlConnection _connection;
//    private BookRepository _repository;
//    private NpgsqlTransaction _transaction;
//    private Guid _testUserId;
//    private Guid _testBookId1;
//    private Guid _testBookId2;

//    private static async Task EnsureTestDatabaseExists()
//    {
//        var masterConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=Titan;Password=Titan";

//        await using var connection = new NpgsqlConnection(masterConnectionString);
//        await connection.OpenAsync();

//        await using var cmd = new NpgsqlCommand(
//            "SELECT 1 FROM pg_database WHERE datname = 'library_test'",
//            connection);

//        var exists = (await cmd.ExecuteScalarAsync()) != null;

//        if (!exists)
//        {
//            await using var createCmd = new NpgsqlCommand(
//                "CREATE DATABASE library_test",
//                connection);
//            await createCmd.ExecuteNonQueryAsync();
//        }
//    }
//    public async Task InitializeAsync()
//    {
//        _connection = new NpgsqlConnection("Host=localhost;Port=5432;Database=library_test;Username=Titan;Password=Titan");
//        await _connection.OpenAsync();
//        _repository = new BookRepository(_connection);

//        await CreateTestSchema();
//        await InsertTestData();
//    }

//    public async Task DisposeAsync()
//    {
//        await using var cmd = new NpgsqlCommand(@"
//            TRUNCATE TABLE borrowings, books, users RESTART IDENTITY CASCADE;
//        ", _connection);
//        await cmd.ExecuteNonQueryAsync();

//        await _connection.CloseAsync();
//        await _connection.DisposeAsync();
//    }

//    private async Task CreateTestSchema()
//    {
//        await using var cmd = new NpgsqlCommand(@"
//            CREATE TABLE IF NOT EXISTS books (
//                id UUID PRIMARY KEY,
//                title TEXT NOT NULL,
//                description TEXT,
//                author TEXT NOT NULL,
//                isbn TEXT NOT NULL,
//                is_borrowed BOOLEAN NOT NULL DEFAULT false,
//                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
//                is_deleted BOOLEAN NOT NULL DEFAULT false
//            );
            
//            CREATE TABLE IF NOT EXISTS users (
//                id UUID PRIMARY KEY,
//                first_name TEXT NOT NULL,
//                last_name TEXT NOT NULL,
//                email TEXT NOT NULL,
//                phone_number TEXT,
//                password_hash TEXT NOT NULL,
//                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
//                is_deleted BOOLEAN NOT NULL DEFAULT false
//            );
            
//            CREATE TABLE IF NOT EXISTS borrowings (
//                id UUID PRIMARY KEY,
//                user_id UUID NOT NULL REFERENCES users(id),
//                book_id UUID NOT NULL REFERENCES books(id),
//                borrowed_at TIMESTAMP NOT NULL DEFAULT NOW(),
//                returned_at TIMESTAMP NULL
//            );", _connection);
//        await cmd.ExecuteNonQueryAsync();
//    }

//    private async Task InsertTestData()
//    {
//        _testUserId = Guid.NewGuid();
//        await using (var cmd = new NpgsqlCommand(@"
//            INSERT INTO users (id, first_name, last_name, email, phone_number, password_hash)
//            VALUES (@id, 'Test', 'User', 'test@example.com', '1234567890', 'hashedpassword')", _connection))
//        {
//            cmd.Parameters.AddWithValue("@id", _testUserId);
//            await cmd.ExecuteNonQueryAsync();
//        }

//        _testBookId1 = Guid.NewGuid();
//        _testBookId2 = Guid.NewGuid();
//        await using (var cmd = new NpgsqlCommand(@"
//            INSERT INTO books (id, title, description, author, isbn)
//            VALUES (@id1, 'Test Book 1', 'Description 1', 'Author 1', '1111111111111'),
//                   (@id2, 'Test Book 2', 'Description 2', 'Author 2', '2222222222222')", _connection))
//        {
//            cmd.Parameters.AddWithValue("@id1", _testBookId1);
//            cmd.Parameters.AddWithValue("@id2", _testBookId2);
//            await cmd.ExecuteNonQueryAsync();
//        }
//    }

//    [Fact]
//    public async Task GetByIdAsync_ReturnsBook_WhenExists()
//    {
//        var result = await _repository.GetByIdAsync(_testBookId1);

//        Assert.NotNull(result);
//        Assert.Equal("Test Book 1", result.Title);
//        Assert.Equal("Author 1", result.Author);
//    }

//    [Fact]
//    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
//    {
//        var result = await _repository.GetByIdAsync(Guid.NewGuid());

//        Assert.Null(result);
//    }

//    [Fact]
//    public async Task GetListAsync_ReturnsAllActiveBooks()
//    {
//        var result = await _repository.GetListAsync();

//        Assert.Equal(2, result.Count());
//    }

//    [Fact]
//    public async Task InsertAsync_AddsNewBook()
//    {
//        var newBook = new Book(
//            Guid.NewGuid(),
//            "New Book Description",
//            "New Book Title",
//            "New Author",
//            "3333333333333"
//        );

//        await _repository.InsertAsync(newBook);
//        var result = await _repository.GetByIdAsync(newBook.Id);

//        Assert.NotNull(result);
//        Assert.Equal("New Book Title", result.Title);
//    }

//    [Fact]
//    public async Task UpdateAsync_ModifiesExistingBook()
//    {
//        var book = await _repository.GetByIdAsync(_testBookId1);
//        var updateBook = new Book(
//            book!.Id,
//            book.Description,
//            "Updated Title",
//            book.Author,
//            book.ISBN
//        );

//        await _repository.UpdateAsync(updateBook);
//        var updatedBook = await _repository.GetByIdAsync(_testBookId1);

//        Assert.Equal("Updated Title", updatedBook!.Title);
//    }

//    [Fact]
//    public async Task DeleteAsync_MarksBookAsDeleted()
//    {
//        await _repository.DeleteAsync(_testBookId1);
//        var result = await _repository.GetByIdAsync(_testBookId1);

//        Assert.Null(result);
//    }

//    [Fact]
//    public async Task CountAsync_ReturnsCorrectCount()
//    {
//        var count = await _repository.CountAsync();

//        Assert.Equal(2, count);
//    }

//    [Fact]
//    public async Task GetPaginateAsync_ReturnsPaginatedResults()
//    {
//        var result = await _repository.GetPaginateAsync(skip: 1, take: 1);

//        Assert.Single(result.Items);
//        Assert.Equal(2, result.TotalCount);
//    }

//    [Fact]
//    public async Task SearchAsync_FiltersByTitle()
//    {
//        var result = await _repository.SearchAsync(
//            title: "Book 1",
//            author: null,
//            isbn: null,
//            skip: 0,
//            take: 10
//        );

//        Assert.Single(result.Items);
//        Assert.Equal("Test Book 1", result.Items.First().Title);
//    }

//    [Fact]
//    public async Task BorrowBookAsync_CreatesBorrowingAndUpdatesBookStatus()
//    {
//        using var separateConnection = new NpgsqlConnection("Host=localhost;Port=5432;Database=library_test;Username=Titan;Password=Titan");
//        await separateConnection.OpenAsync();
//        var separateRepo = new BookRepository(separateConnection);

//        var borrowing = await separateRepo.BorrowBookAsync(_testUserId, _testBookId1);

//        var book = await separateRepo.GetByIdAsync(_testBookId1);
//        Assert.True(book!.IsBorrowed);

//        var borrowings = await separateRepo.GetUserBorrowingsAsync(_testUserId);
//        Assert.Contains(borrowings, b => b.BookId == _testBookId1);
//    }

//    [Fact]
//    public async Task ReturnBookAsync_UpdatesBorrowingAndBookStatus()
//    {
//        using var separateConnection = new NpgsqlConnection("Host=localhost;Port=5432;Database=library_test;Username=Titan;Password=Titan");
//        await separateConnection.OpenAsync();
//        var separateRepo = new BookRepository(separateConnection);

//        var borrowing = await separateRepo.BorrowBookAsync(_testUserId, _testBookId1);

//        await separateRepo.ReturnBookAsync(_testUserId, borrowing.Id);

//        var book = await separateRepo.GetByIdAsync(_testBookId1);
//        Assert.False(book!.IsBorrowed);

//        var borrowings = await separateRepo.GetUserBorrowingsAsync(_testUserId);
//        var returnedBorrowing = borrowings.First(b => b.Id == borrowing.Id);
//        Assert.NotNull(returnedBorrowing.ReturnedAt);
//    }

//    [Fact]
//    public async Task GetUserBorrowingsAsync_ReturnsAllUserBorrowings()
//    {
//        using var separateConnection = new NpgsqlConnection("Host=localhost;Port=5432;Database=library_test;Username=Titan;Password=Titan");
//        await separateConnection.OpenAsync();
//        var separateRepo = new BookRepository(separateConnection);

//        var borrowing1 = await separateRepo.BorrowBookAsync(_testUserId, _testBookId1);
//        var borrowing2 = await separateRepo.BorrowBookAsync(_testUserId, _testBookId2);

//        var borrowings = await separateRepo.GetUserBorrowingsAsync(_testUserId);

//        Assert.Equal(2, borrowings.Count());
//        Assert.Contains(borrowings, b => b.BookId == _testBookId1);
//        Assert.Contains(borrowings, b => b.BookId == _testBookId2);
//    }
//}