using LibrarySystem.Data.Users;
using Npgsql;
using Xunit;

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
        // Create and open connection
        _connection = new NpgsqlConnection("Host=localhost;Port=5432;Database=library_test;Username=Titan;Password=Titan");
        await _connection.OpenAsync();

        // Begin transaction for test isolation
        _transaction = await _connection.BeginTransactionAsync();
        _repository = new UserRepository(_connection);

        // Create users table if not exists
        await CreateTestSchema();

        // Insert test data
        await InsertTestData();
    }

    public async Task DisposeAsync()
    {
        // Rollback transaction to undo all test changes
        if (_transaction != null)
            await _transaction.RollbackAsync();

        // Close and dispose connection
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }

    private async Task CreateTestSchema()
    {
        await using var cmd = new NpgsqlCommand(@"
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

        await cmd.ExecuteNonQueryAsync();
    }

    private async Task InsertTestData()
    {
        // Insert test users
        _testUserId1 = Guid.NewGuid();
        _testUserId2 = Guid.NewGuid();

        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO users (id, first_name, last_name, email, phone_number, password_hash)
            VALUES 
                (@id1, 'John', 'Doe', 'john.doe@example.com', '1234567890', 'hashedpassword1'),
                (@id2, 'Jane', 'Smith', 'jane.smith@example.com', '0987654321', 'hashedpassword2')", _connection);

        cmd.Parameters.AddWithValue("@id1", _testUserId1);
        cmd.Parameters.AddWithValue("@id2", _testUserId2);

        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(_testUserId1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenExists()
    {
        // Act
        var result = await _repository.GetByEmailAsync("john.doe@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserId1, result.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetListAsync_ReturnsAllActiveUsers()
    {
        // Act
        var result = await _repository.GetListAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task InsertAsync_AddsNewUser()
    {
        // Arrange
        var newUser = new User(
            Guid.NewGuid(),
            "New",
            "User",
            "new.user@example.com",
            "5555555555"
        );

        newUser.SetPasswordHash("newhashedpassword");
        newUser.SetCreatedAt(DateTime.UtcNow);

        // Act
        await _repository.InsertAsync(newUser);
        var result = await _repository.GetByIdAsync(newUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New", result.FirstName);
        Assert.Equal("new.user@example.com", result.Email);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingUser()
    {
        // Arrange
        var user = await _repository.GetByIdAsync(_testUserId1);

        var updateUser = new User(user!.Id, user.FirstName, user.LastName, user.Email, "9999999999");
        updateUser.SetPasswordHash(user.PasswordHash);
        updateUser.SetCreatedAt(user.CreatedAt);

        // Act
        await _repository.UpdateAsync(updateUser);
        var updatedUser = await _repository.GetByIdAsync(_testUserId1);

        // Assert
        Assert.Equal("9999999999", updatedUser!.PhoneNumber);
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Act
        var count = await _repository.CountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetPaginateAsync_ReturnsPaginatedResults()
    {
        // Act
        var result = await _repository.GetPaginateAsync(skip: 1, take: 1);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task Email_ShouldBeCaseSensitive()
    {
        // Act - search with different case
        var result = await _repository.GetByEmailAsync("JOHN.DOE@EXAMPLE.COM");

        // Assert
        Assert.Null(result);
    }


    [Fact]
    public async Task DeleteAsync_MarksUserAsDeleted()
    {
        // Act
        await _repository.DeleteAsync(_testUserId1);
        var result = await _repository.GetByIdAsync(_testUserId1);

        // Assert
        Assert.Null(result);
    }

}