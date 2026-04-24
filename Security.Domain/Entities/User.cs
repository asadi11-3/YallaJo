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

    /// <summary>
    /// Persisted lifecycle state — Phase 2A+2B source of truth.
    /// <see cref="IsActive"/> is derived from this for backward compatibility
    /// with existing callers (queries, projections, admin UI). Will become
    /// the sole representation when <c>IsActive</c> is removed in Phase 4.
    /// </summary>
    public AccountLifecycleState LifecycleState { get; private set; } = AccountLifecycleState.Provisioned;

    /// <summary>
    /// Backward-compatibility shim: <c>true</c> iff
    /// <see cref="LifecycleState"/> equals <see cref="AccountLifecycleState.Active"/>.
    /// Persisted as a normal column for the duration of Phase 2A+2B so
    /// existing LINQ queries (<c>Where(u =&gt; u.IsActive)</c>) continue to
    /// translate. The property is read-only; mutations flow through the
    /// lifecycle transitions instead. Removal deferred to Phase 4.
    /// </summary>
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

        // Pre-Phase-2A invariant: verifying the primary email also activates
        // the account. Preserved here so the existing self-registration +
        // external-login + invite-acceptance flows keep their atomic
        // semantics. The lifecycle transition is routed through TransitionTo
        // so the AccountLifecycleTransitionedEvent fires alongside.
        if (LifecycleState != AccountLifecycleState.Active)
        {
            TransitionTo(AccountLifecycleState.Active);
        }

        AddDomainEvent(new EmailVerifiedEvent(Id, email.Id, email.Address));

        return email;
    }

    /// <summary>
    /// Returns the primary email entity, or null if none exists.
    /// </summary>
    public Email? GetPrimaryEmail() => _emails.FirstOrDefault(e => e.IsPrimary);

    // ── Lifecycle transitions ─────────────────────────────────────────────────
    //
    // Each public verb maps a business intent to a state-machine transition.
    // The private TransitionTo(...) method is the SOLE place that mutates
    // LifecycleState + IsActive — keeps invariants in one spot and emits a
    // single AccountLifecycleTransitionedEvent per real change.

    /// <summary>
    /// Admin issued an activation token (and sent the email) for a freshly
    /// provisioned account. <c>Provisioned → PendingActivation</c>.
    /// Idempotent on <c>PendingActivation</c>.
    /// </summary>
    public void MarkPendingActivation() => TransitionTo(AccountLifecycleState.PendingActivation);

    /// <summary>
    /// Activation completed by the user (password set, email verified) OR
    /// admin lifted a suspension. Allowed transitions:
    /// <c>PendingActivation → Active</c>, <c>Suspended → Active</c>,
    /// <c>PendingPasswordReset → Active</c>, plus the legacy implicit path
    /// from <c>Provisioned → Active</c> retained for the external-login
    /// auto-create flow that has no separate activation step.
    /// </summary>
    public void Activate() => TransitionTo(AccountLifecycleState.Active);

    /// <summary>
    /// Admin temporarily disabled the account. <c>Active → Suspended</c>.
    /// Idempotent on <c>Suspended</c>.
    /// <para>
    /// Phase 2A note: existing <see cref="Deactivate"/> verb is preserved as
    /// a façade that delegates here, keeping <c>DeactivateUserCommandHandler</c>
    /// working unchanged.
    /// </para>
    /// </summary>
    public void Suspend() => TransitionTo(AccountLifecycleState.Suspended);

    /// <summary>Lifts a suspension. <c>Suspended → Active</c>.</summary>
    public void Reactivate() => TransitionTo(AccountLifecycleState.Active);

    /// <summary>
    /// Admin forced a password reset (Phase 2C+ wiring). Blocks login until
    /// the user completes the reset. <c>Active → PendingPasswordReset</c>.
    /// </summary>
    public void MarkPendingPasswordReset() => TransitionTo(AccountLifecycleState.PendingPasswordReset);

    /// <summary>
    /// Terminal: archive the account. Permitted from any non-Archived state.
    /// </summary>
    public void Archive() => TransitionTo(AccountLifecycleState.Archived);

    /// <summary>
    /// Legacy verb retained for backward compatibility (existing
    /// DeactivateUserCommandHandler call site). Maps to <see cref="Suspend"/>
    /// in the lifecycle model. Will be removed in Phase 4 along with the
    /// command rename to <c>SuspendUserCommand</c>.
    /// </summary>
    public void Deactivate() => Suspend();

    private void TransitionTo(AccountLifecycleState target)
    {
        if (LifecycleState == target)
            return; // idempotent self-transition — no event, no mutation

        if (!IsTransitionAllowed(LifecycleState, target))
            throw new InvalidLifecycleTransitionException(LifecycleState, target);

        var from = LifecycleState;
        LifecycleState = target;
        IsActive = (target == AccountLifecycleState.Active);
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

            // Legacy implicit path: external-login auto-create + self-registration's
            // VerifyEmail flow currently jumps Provisioned -> Active in one move
            // (no separate activation step exists for those code paths). Phase 2C
            // will route the external/self-registration flows through
            // PendingActivation explicitly; until then, allow this shortcut to
            // preserve current behaviour.
            (AccountLifecycleState.Provisioned,        AccountLifecycleState.Active)               => true,

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

    /// <summary>
    /// Sets the password hash for a brand-new user during initial registration.
    /// Does NOT raise <see cref="PasswordChangedEvent"/> — the user was just created
    /// and <see cref="UserCreatedEvent"/> already covers the initial state.
    /// </summary>
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
