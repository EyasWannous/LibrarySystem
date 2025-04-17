using LibrarySystem.Data.Results;

namespace LibrarySystem.Data.Users;

public interface IUserRepository
{
    Task InsertAsync(User entity);
    Task<int> CountAsync();
    Task DeleteAsync(Guid id);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetListAsync();
    Task<PaginatedResponse<User>> GetPaginateAsync(int skip, int take);
    Task UpdateAsync(User entity);
}
