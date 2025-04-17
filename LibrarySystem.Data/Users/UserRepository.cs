using LibrarySystem.Data.Results;
using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace LibrarySystem.Data.Users;

public class UserRepository : IUserRepository
{
    private readonly NpgsqlConnection _db;

    public UserRepository(NpgsqlConnection db)
    {
        _db = db;
    }

    public async Task<int> CountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM users WHERE is_deleted = false";

        await using var cmd = new NpgsqlCommand(sql, _db);
        await EnsureConnectionOpen();

        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "UPDATE users SET is_deleted = true WHERE id = @id";

        await using var cmd = new NpgsqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);

        await EnsureConnectionOpen();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT id, first_name, last_name, email, phone_number, password_hash, created_at 
                              FROM users 
                              WHERE id = @id AND is_deleted = false";

        await using var cmd = new NpgsqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);

        await EnsureConnectionOpen();

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var user = new User(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4)
            );

            user.SetPasswordHash(reader.GetString(5));
            user.SetCreatedAt(reader.GetDateTime(6));

            return user;
        }

        return null;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = @"SELECT id, first_name, last_name, email, phone_number, password_hash, created_at 
                              FROM users 
                              WHERE email = @email AND is_deleted = false";

        await using var cmd = new NpgsqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@email", email);

        await EnsureConnectionOpen();

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var user = new User(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4)
            );

            user.SetPasswordHash(reader.GetString(5));
            user.SetCreatedAt(reader.GetDateTime(6));
            
            return user;
        }

        return null;
    }

    public async Task<IEnumerable<User>> GetListAsync()
    {
        var users = new List<User>();
        const string sql = @"SELECT id, first_name, last_name, email, phone_number, password_hash, created_at 
                              FROM users 
                              WHERE is_deleted = false";

        await using var cmd = new NpgsqlCommand(sql, _db);

        await EnsureConnectionOpen();

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var user = new User(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4)
            );

            user.SetPasswordHash(reader.GetString(5));
            user.SetCreatedAt(reader.GetDateTime(6));
            users.Add(user);
        }

        return users;
    }

    public async Task<PaginatedResponse<User>> GetPaginateAsync(int skip, int take)
    {
        var users = new List<User>();
        const string sql = @"SELECT id, first_name, last_name, email, phone_number, password_hash, created_at 
                              FROM users 
                              WHERE is_deleted = false 
                              ORDER BY created_at DESC 
                              LIMIT @take OFFSET @skip;
                              
                              SELECT COUNT(*) 
                              FROM users 
                              WHERE is_deleted = false";

        await using var cmd = new NpgsqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@skip", skip);
        cmd.Parameters.AddWithValue("@take", take);

        await EnsureConnectionOpen();

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var user = new User(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4)
            );

            user.SetPasswordHash(reader.GetString(5));
            user.SetCreatedAt(reader.GetDateTime(6));
            users.Add(user);
        }

        await reader.NextResultAsync();
        var totalCount = await reader.ReadAsync() ? reader.GetInt32(0) : 0;

        return new PaginatedResponse<User>(totalCount, users);
    }

    public async Task InsertAsync(User entity)
    {
        const string sql = @"INSERT INTO users 
                               (id, first_name, last_name, email, phone_number, password_hash, created_at, is_deleted)
                               VALUES 
                               (@id, @firstName, @lastName, @email, @phoneNumber, @passwordHash, @createdAt, false)";

        await using var cmd = new NpgsqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", entity.Id);
        cmd.Parameters.AddWithValue("@firstName", entity.FirstName);
        cmd.Parameters.AddWithValue("@lastName", entity.LastName);
        cmd.Parameters.AddWithValue("@email", entity.Email);
        cmd.Parameters.AddWithValue("@phoneNumber", entity.PhoneNumber);
        cmd.Parameters.AddWithValue("@passwordHash", entity.PasswordHash);
        cmd.Parameters.AddWithValue("@createdAt", entity.CreatedAt);

        await EnsureConnectionOpen();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(User entity)
    {
        const string sql = @"UPDATE users 
                               SET first_name = @firstName, 
                                   last_name = @lastName, 
                                   email = @email, 
                                   phone_number = @phoneNumber,
                                   password_hash = @passwordHash
                               WHERE id = @id AND is_deleted = false";

        await using var cmd = new NpgsqlCommand(sql, _db);

        cmd.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Uuid) { Value = entity.Id });
        cmd.Parameters.Add(new NpgsqlParameter("@firstName", NpgsqlDbType.Text) { Value = entity.FirstName });
        cmd.Parameters.Add(new NpgsqlParameter("@lastName", NpgsqlDbType.Text) { Value = entity.LastName });
        cmd.Parameters.Add(new NpgsqlParameter("@email", NpgsqlDbType.Text) { Value = entity.Email });
        cmd.Parameters.Add(new NpgsqlParameter("@phoneNumber", NpgsqlDbType.Text) { Value = entity.PhoneNumber });
        cmd.Parameters.Add(new NpgsqlParameter("@passwordHash", NpgsqlDbType.Text) { Value = entity.PasswordHash });

        await EnsureConnectionOpen();
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task EnsureConnectionOpen()
    {
        if (_db.State is not ConnectionState.Open)
            await _db.OpenAsync();
    }
}