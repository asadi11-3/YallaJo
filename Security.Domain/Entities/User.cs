using Security.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Security.Domain.Entities;

public sealed class User : AuditableEntity, IAggregateRoot
{
    private readonly List<Email> _emails = [];
    private readonly List<Phone> _phones = [];
    private readonly List<UserRole> _userRoles = [];
    private readonly List<UserClaim> _userClaims = [];

    private User() { } // EF Core

    public AccountLifecycleState LifecycleState { get; private set; } = AccountLifecycleState.Provisioned;

    public bool IsActive { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;
    public IReadOnlyCollection<Email> Emails => _emails.AsReadOnly();
    public IReadOnlyCollection<Phone> Phones => _phones.AsReadOnly();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<UserClaim> UserClaims => _userClaims.AsReadOnly();

    public static User Create()
    {
        return new User
        {
            LifecycleState = AccountLifecycleState.Provisioned,
            IsActive = false,
        };
    }

    public static User Register(string email, string firstName, string lastName)
    {
        var user = new User
        {
            LifecycleState = AccountLifecycleState.Provisioned,
            IsActive = false,
        };
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

        if (LifecycleState != AccountLifecycleState.Active)
        {
            TransitionTo(AccountLifecycleState.Active);
        }

        AddDomainEvent(new EmailVerifiedEvent(Id, email.Id, email.Address));

        return email;
    }

    public Email? GetPrimaryEmail() => _emails.FirstOrDefault(e => e.IsPrimary);

    public void MarkPendingActivation() => TransitionTo(AccountLifecycleState.PendingActivation);

    public void Activate() => TransitionTo(AccountLifecycleState.Active);
    public void Suspend() => TransitionTo(AccountLifecycleState.Suspended);

    public void Reactivate() => TransitionTo(AccountLifecycleState.Active);
    public void MarkPendingPasswordReset() => TransitionTo(AccountLifecycleState.PendingPasswordReset);

    public void Archive() => TransitionTo(AccountLifecycleState.Archived);
    public void Deactivate() => Suspend();

    public void ReassignToPendingActivation(string newEmail, string replacementPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
            throw new ArgumentException("New email is required.", nameof(newEmail));
        if (string.IsNullOrWhiteSpace(replacementPasswordHash))
            throw new ArgumentException("Replacement password hash is required.", nameof(replacementPasswordHash));

        if (LifecycleState is not (AccountLifecycleState.Active
                                 or AccountLifecycleState.Suspended
                                 or AccountLifecycleState.PendingPasswordReset))
        {
            throw new InvalidLifecycleTransitionException(LifecycleState, AccountLifecycleState.PendingActivation);
        }

        var primary = _emails.FirstOrDefault(e => e.IsPrimary);
        if (primary is null)
            throw new InvalidOperationException("User has no primary email to reassign.");

        primary.ChangeAddress(newEmail);

        ResetPassword(replacementPasswordHash);

        TransitionTo(AccountLifecycleState.PendingActivation);
    }

    private void TransitionTo(AccountLifecycleState target)
    {
        if (LifecycleState == target)
            return; // idempotent self-transition — no event, no mutation

        if (!IsTransitionAllowed(LifecycleState, target))
            throw new InvalidLifecycleTransitionException(LifecycleState, target);

        var from = LifecycleState;
        LifecycleState = target;
        IsActive = target == AccountLifecycleState.Active;
        MarkUpdated();

        AddDomainEvent(new AccountLifecycleTransitionedEvent(Id, from, target));
    }

    private static bool IsTransitionAllowed(AccountLifecycleState from, AccountLifecycleState to)
    {
        // Archived is terminal — no path out.
        if (from == AccountLifecycleState.Archived)
            return false;

        // Anything non-Archived can be archived.
        if (to == AccountLifecycleState.Archived)
            return true;

        return (from, to) switch
        {
            // Provisioning path
            (AccountLifecycleState.Provisioned,        AccountLifecycleState.PendingActivation)    => true,
            (AccountLifecycleState.PendingActivation,  AccountLifecycleState.Active)               => true,
            (AccountLifecycleState.PendingActivation,  AccountLifecycleState.Provisioned)          => true, // admin revoke (Phase 2C)

            // Active operations
            (AccountLifecycleState.Active,             AccountLifecycleState.Suspended)            => true,
            (AccountLifecycleState.Suspended,          AccountLifecycleState.Active)               => true,
            (AccountLifecycleState.Active,             AccountLifecycleState.PendingPasswordReset) => true,
            (AccountLifecycleState.PendingPasswordReset, AccountLifecycleState.Active)             => true,

            (AccountLifecycleState.Provisioned,        AccountLifecycleState.Active)               => true,

            (AccountLifecycleState.Active,               AccountLifecycleState.PendingActivation) => true,
            (AccountLifecycleState.Suspended,            AccountLifecycleState.PendingActivation) => true,
            (AccountLifecycleState.PendingPasswordReset, AccountLifecycleState.PendingActivation) => true,

            _ => false,
        };
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
        MarkUpdated();
        AddDomainEvent(new PasswordChangedEvent(Id));
    }

    public void SetInitialPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
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

    public void AssignRole(Role role)
    {
        var userRole = UserRole.Create(Id, role.Id);
        _userRoles.Add(userRole);
        MarkUpdated();
    }
}
