using Security.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Security.Domain.Entities;

/// <summary>
/// The ONLY identity aggregate root in the system.
/// All other modules reference this entity's Id (UserId) via logical relationships only.
/// </summary>
public sealed class User : AuditableEntity, IAggregateRoot
{
    private readonly List<Email> _emails = [];
    private readonly List<Phone> _phones = [];
    private readonly List<UserRole> _userRoles = [];
    private readonly List<UserClaim> _userClaims = [];

    private User() { } // EF Core

    public bool IsActive { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public IReadOnlyCollection<Email> Emails => _emails.AsReadOnly();
    public IReadOnlyCollection<Phone> Phones => _phones.AsReadOnly();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<UserClaim> UserClaims => _userClaims.AsReadOnly();

    public static User Create()
    {
        return new User { IsActive = false };
    }

    public static User Register(string email, string firstName, string lastName)
    {
        var user = new User { IsActive = false };
        var primaryEmail = Email.Create(user.Id, email, true);
        user._emails.Add(primaryEmail);

        user.AddDomainEvent(new UserCreatedEvent(user.Id, primaryEmail.Address, firstName, lastName));

        return user;
    }

    public Email? VerifyEmail(Guid emailId)
    {
        var email = _emails.FirstOrDefault(e => e.Id == emailId);
        if (email is null)
        {
            return null;
        }

        if (email.IsVerified)
        {
            return email;
        }

        email.MarkVerified();

        if (!IsActive)
        {
            Activate();
        }

        AddDomainEvent(new EmailVerifiedEvent(Id, email.Id, email.Address));

        return email;
    }

    /// <summary>
    /// Returns the primary email entity, or null if none exists.
    /// </summary>
    public Email? GetPrimaryEmail() => _emails.FirstOrDefault(e => e.IsPrimary);

    public void Activate() => IsActive = true;

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
        MarkUpdated();
    }

    public void ResetPassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash is required.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        MarkUpdated();
        AddDomainEvent(new PasswordResetEvent(Id));
    }

    public Phone UpdatePrimaryPhone(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

        var trimmed = phoneNumber.Trim();

        var primary = _phones.FirstOrDefault(p => p.IsPrimary);

        if (primary is null)
        {
            var created = Phone.Create(Id, trimmed, isPrimary: true);
            _phones.Add(created);

            AddDomainEvent(new PhoneNumberUpdatedEvent(Id, created.PhoneNumber, created.IsPrimary));
            MarkUpdated();
            return created;
        }

        // Idempotency: if already primary and same number, do nothing
        if (string.Equals(primary.PhoneNumber, trimmed, StringComparison.Ordinal))
            return primary;

        primary.UpdateNumber(trimmed);
        if (!primary.IsPrimary)
            primary.SetPrimary(true);

        AddDomainEvent(new PhoneNumberUpdatedEvent(Id, primary.PhoneNumber, primary.IsPrimary));
        MarkUpdated();
        return primary;
    }
}
