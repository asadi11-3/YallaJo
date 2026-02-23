using YallaJo.SharedKernel.Domain.Entities;

namespace Accounts.Domain.Entities;


public sealed class Profile : AuditableEntity, IAggregateRoot
{
    private Profile() { } // EF Core

    /// <summary>Logical reference to Security.User.Id — not a FK.</summary>
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }

    public static Profile Create(Guid userId, string firstName, string lastName)
    {
        return new Profile
        {
            UserId = userId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim()
        };
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        MarkUpdated();
    }

    public void SetDisplayName(string? displayName)
    {
        DisplayName = displayName?.Trim();
        MarkUpdated();
    }

    public void SetAvatarUrl(string? avatarUrl)
    {
        AvatarUrl = avatarUrl?.Trim();
        MarkUpdated();
    }
}
