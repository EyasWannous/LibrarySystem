namespace LibrarySystem.Data.Users;

public class User
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public bool IsDeleted { get; private set; } = false;
    public User(
        Guid id,
        string firstName,
        string lastName,
        string email,
        string phoneNumber)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
    }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
    public void SetCreatedAt(DateTime createdAt) => CreatedAt = createdAt;
}
