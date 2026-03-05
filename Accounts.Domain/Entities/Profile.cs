using Accounts.Domain.Enums;
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
    public DateOnly? DateOfBirth { get; private set; }
    public Gender? Gender { get; private set; }
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? AddressLine { get; private set; }

    public static Profile Create(Guid userId, string firstName, string lastName)
    {
        return new Profile
        {
            UserId = userId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim()
        };
    }

    public void UpdateProfile(
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        Gender? gender,
        string? country,
        string? city,
        string? addressLine)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Country = country?.Trim();
        City = city?.Trim();
        AddressLine = addressLine?.Trim();
        MarkUpdated();
    }

    public void UpdateAvatar(string avatarUrl)
    {
        AvatarUrl = avatarUrl.Trim();
        MarkUpdated();
    }

    public void DeleteAvatar()
    {
        AvatarUrl = null;
        MarkUpdated();
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
