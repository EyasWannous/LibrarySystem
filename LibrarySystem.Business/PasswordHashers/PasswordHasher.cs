using LibrarySystem.Data.Users;

namespace LibrarySystem.BusinessLogic.PasswordHashers;

public class PasswordHasher
{
    const char Delimiter = ';';
    public string HashPassword(User user, string password)
    {
        var mergedPassword = MakeMergedPassword(user, password);
        var hasedPassword = PasswordHashingSalting.HashPasword(mergedPassword, out byte[] salt);

        return string.Join(Delimiter, Convert.ToHexString(salt), hasedPassword);
    }

    public bool VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
    {
        var elements = hashedPassword.Split(Delimiter);
        var salt = Convert.FromHexString(elements[0]);
        var hash = elements[1];

        var mergedPassword = MakeMergedPassword(user, providedPassword);

        return PasswordHashingSalting.VerifyPassword(mergedPassword, hash, salt);
    }

    private string MakeMergedPassword(User user, string password) => $"{user.FirstName}_{user.LastName}_{password}_{user.Email}";
}
