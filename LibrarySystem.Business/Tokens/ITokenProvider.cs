using LibrarySystem.Data.Users;

namespace LibrarySystem.BusinessLogic.Tokens;

public interface ITokenProvider
{
    Task<(string JwtToken, DateTime ExpireDate)> GenerateJwtTokenAsync(User user);
    Task<(string Message, bool IsSuccess)> VerfiyTokenAsync(string jwtToken);
}