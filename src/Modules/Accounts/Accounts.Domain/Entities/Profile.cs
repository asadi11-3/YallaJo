using Accounts.Domain.Enums;
using Accounts.Domain.ValueObjects;
using YallaJo.SharedKernel.Domain.Entities;

namespace Accounts.Domain.Entities;


public sealed class Profile : AuditableEntity, IAggregateRoot
{
    private Profile() { } // EF Core

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
    public MarketingConsent? MarketingConsent { get; private set; }

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

    public void UpdateMarketingConsent(MarketingConsent consent)
    {
        MarketingConsent = consent;
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

    public void ResetForReassignment(string? newEmailLocalPart)
    {
        FirstName = "Pending";
        LastName  = "Activation";

        DisplayName = string.IsNullOrWhiteSpace(newEmailLocalPart)
            ? null
            : newEmailLocalPart.Trim();

        AvatarUrl   = null;
        DateOfBirth = null;
        Gender      = null;
        Country     = null;
        City        = null;
        AddressLine = null;

        MarkUpdated();
    }
}
