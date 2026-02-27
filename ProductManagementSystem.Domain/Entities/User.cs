using ProductManagementSystem.Domain.Exceptions;

namespace ProductManagementSystem.Domain.Entities;

public sealed class User
{
    public int Id { get; init; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public string Email { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; init; }

    private User(int id, string username, string passwordHash, string email, bool isActive, DateTime createdAt)
    {
        Id = id;
        Username = username;
        PasswordHash = passwordHash;
        Email = email;
        IsActive = isActive;
        CreatedAt = createdAt;
    }

    public static User Create(string username, string passwordHash, string email)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Username não pode ser vazio");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password não pode ser vazio");

        if (!email.Contains("@"))
            throw new DomainException("Email inválido");

        return new User(0, username, passwordHash, email, true, DateTime.UtcNow);
    }

    public static User Reconstruct(int id, string username, string passwordHash, string email, bool isActive, DateTime createdAt)
    {
        return new User(id, username, passwordHash, email, isActive, createdAt);
    }
}
