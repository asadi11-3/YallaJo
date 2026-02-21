using YallaJo.SharedKernel.Domain.Entities;

namespace Auth.Domain.Entities;

/// <summary>
/// Authentication credentials for a registered user.
/// Created when a UserRegisteredIntegrationEvent is received from the Accounts module.
/// The Id mirrors the Accounts module User.Id so lookups are O(1) by identity.
/// </summary>
public sealed class UserCredentials : BaseEntity<Guid>, IAggregateRoot
{
    private UserCredentials() { } // EF Core

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    /// <summary>
    /// Creates a stub credential record when a user registers.
    /// Id is set to the Accounts User.Id so cross-module lookups require no join.
    /// Password is intentionally empty — the user will set it via a separate flow.
    /// </summary>
    public static UserCredentials CreateStub(Guid userId, string email)
    {
        return new UserCredentials
        {
            Id = userId,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = string.Empty,
            PasswordSalt = string.Empty,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Activates the credentials once the user's email is verified.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Sets a hashed password once the user completes onboarding.</summary>
    public void SetPassword(string passwordHash, string passwordSalt)
    {
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        UpdatedAt = DateTime.UtcNow;
    }
}
