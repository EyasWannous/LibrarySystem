using LibrarySystem.BusinessLogic.Users.DTOs;

namespace LibrarySystem.BusinessLogic.Users;

public interface IUserService
{
    Task<UserManagerResponse> LoginByEmailAsync(string email, string password);
    Task<UserManagerResponse> RegisterAsync(string firstName, string lastName, string email, string phoneNumber, string password);
}
