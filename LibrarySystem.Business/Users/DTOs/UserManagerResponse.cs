using LibrarySystem.Data.Users;
using System.Security.Claims;

namespace LibrarySystem.BusinessLogic.Users.DTOs;

public record UserManagerResponse
{
    public User? User { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public IEnumerable<string> Errors { get; set; } = [];
    public string JwtToken { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
}
