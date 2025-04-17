using LibrarySystem.BusinessLogic.PasswordHashers;
using LibrarySystem.BusinessLogic.Tokens;
using LibrarySystem.BusinessLogic.Users.DTOs;
using LibrarySystem.Data.Users;

namespace LibrarySystem.BusinessLogic.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;

    public UserService(IUserRepository userRepository, PasswordHasher passwordHasher, ITokenProvider tokenProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenProvider = tokenProvider;
    }

    public async Task<UserManagerResponse> LoginByEmailAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null)
            return new UserManagerResponse { Message = "No user found with this email.", IsSuccess = false, User = null };

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (!result)
            return new UserManagerResponse { Message = "Incorrect email or password.", IsSuccess = false, User = null };

        var (jwtToken, expireDate) = await _tokenProvider.GenerateJwtTokenAsync(user);

        return new UserManagerResponse 
        { 
            Message = "Logged in successfully!",
            IsSuccess = true,
            User = user,
            JwtToken = jwtToken,
            ExpiryDate = expireDate
        };
    }

    public async Task<UserManagerResponse> RegisterAsync(string firstName, string lastName, string email, string phoneNumber, string password)
    {
        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser is not null)
            return new UserManagerResponse
            {
                Message = "Email is already taken. Email must be unique.",
                IsSuccess = false,
            };

        var user = new User(Guid.NewGuid(), firstName, lastName, email, phoneNumber);

        var hashedPassword = _passwordHasher.HashPassword(user, password);

        user.SetPasswordHash(hashedPassword);

        await _userRepository.InsertAsync(user);

        var (jwtToken, expireDate) = await _tokenProvider.GenerateJwtTokenAsync(user);

        return new UserManagerResponse 
        { 
            Message = "User created successfully!",
            IsSuccess = true,
            User = user,
            JwtToken = jwtToken,
            ExpiryDate = expireDate
        };
    }
}
