using LibrarySystem.Data.Users;
using Npgsql;

namespace LibrarySystem.Data.Tests.Integration;

public class UserRepositoryTests : IAsyncLifetime
{
    private NpgsqlConnection _connection;
    private NpgsqlTransaction _transaction;
    private UserRepository _repository;
    private Guid _testUserId1;
    private Guid _testUserId2;

    public async Task InitializeAsync()
    {
        _connection = new NpgsqlConnection("Host=localhost;Port=5432;Database=library_test;Username=Titan;Password=Titan");
        await _connection.OpenAsync();

        _transaction = await _connection.BeginTransactionAsync();
        _repository = new UserRepository(_connection);

        await CreateTestSchema();

        await InsertTestData();
    }

    public async Task DisposeAsync()
    {
        if (_transaction != null)
            await _transaction.RollbackAsync();

        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }

    private async Task CreateTestSchema()
    {
        await using var command = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY,
                first_name TEXT NOT NULL,
                last_name TEXT NOT NULL,
                email TEXT NOT NULL UNIQUE,
                phone_number TEXT,
                password_hash TEXT NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                is_deleted BOOLEAN NOT NULL DEFAULT false
            );", _connection);

        await command.ExecuteNonQueryAsync();
    }

    private async Task InsertTestData()
    {
        _testUserId1 = Guid.NewGuid();
        _testUserId2 = Guid.NewGuid();

        await using var command = new NpgsqlCommand(@"
            INSERT INTO users (id, first_name, last_name, email, phone_number, password_hash)
            VALUES 
                (@id1, 'John', 'Doe', 'john.doe@example.com', '1234567890', 'hashedpassword1'),
                (@id2, 'Jane', 'Smith', 'jane.smith@example.com', '0987654321', 'hashedpassword2')", _connection);

        command.Parameters.AddWithValue("@id1", _testUserId1);
        command.Parameters.AddWithValue("@id2", _testUserId2);

        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenExists()
    {
        var result = await _repository.GetByIdAsync(_testUserId1);

        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenExists()
    {
        var result = await _repository.GetByEmailAsync("john.doe@example.com");

        Assert.NotNull(result);
        Assert.Equal(_testUserId1, result.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _repository.GetByEmailAsync("nonexistent@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetListAsync_ReturnsAllActiveUsers()
    {
        var result = await _repository.GetListAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task InsertAsync_AddsNewUser()
    {
        var newUser = new User(
            Guid.NewGuid(),
            "New",
            "User",
            "new.user@example.com",
            "5555555555"
        );

        newUser.SetPasswordHash("newhashedpassword");
        newUser.SetCreatedAt(DateTime.UtcNow);

        await _repository.InsertAsync(newUser);
        var result = await _repository.GetByIdAsync(newUser.Id);

        Assert.NotNull(result);
        Assert.Equal("New", result.FirstName);
        Assert.Equal("new.user@example.com", result.Email);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingUser()
    {
        var user = await _repository.GetByIdAsync(_testUserId1);

        var updateUser = new User(user!.Id, user.FirstName, user.LastName, user.Email, "9999999999");
        updateUser.SetPasswordHash(user.PasswordHash);
        updateUser.SetCreatedAt(user.CreatedAt);

        await _repository.UpdateAsync(updateUser);
        var updatedUser = await _repository.GetByIdAsync(_testUserId1);

        Assert.Equal("9999999999", updatedUser!.PhoneNumber);
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        var count = await _repository.CountAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetPaginateAsync_ReturnsPaginatedResults()
    {
        var result = await _repository.GetPaginateAsync(skip: 1, take: 1);

        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task Email_ShouldBeCaseSensitive()
    {
        var result = await _repository.GetByEmailAsync("JOHN.DOE@EXAMPLE.COM");

        Assert.Null(result);
    }


    [Fact]
    public async Task DeleteAsync_MarksUserAsDeleted()
    {
        await _repository.DeleteAsync(_testUserId1);
        var result = await _repository.GetByIdAsync(_testUserId1);

        Assert.Null(result);
    }

}