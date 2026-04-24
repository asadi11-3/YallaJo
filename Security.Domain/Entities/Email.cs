using YallaJo.SharedKernel.Domain.Entities;

namespace Security.Domain.Entities;

public sealed class Email : AuditableEntity
{
    private Email() { } // EF Core

    public Guid UserId { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    public User User { get; private set; } = default!;

    public static Email Create(Guid userId, string address, bool isPrimary)
    {
        return new Email
        {
            UserId = userId,
            Address = address.Trim().ToLowerInvariant(),
            IsPrimary = isPrimary,
            IsVerified = false
        };
    }

    public void MarkVerified()
    {
        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
        MarkUpdated();
    }

    /// <summary>
    /// Phase 3C — retarget this email entity to a new address. Used by the
    /// admin reassignment flow to move a user's primary email to a new
    /// address while preserving the row (FK continuity). Normalizes the
    /// supplied address the same way <see cref="Create"/> does so the
    /// unique index on <c>Address</c> stays deterministic. Clears
    /// verification state — the new address must be verified via the
    /// activation flow before it can be used for login.
    /// </summary>
    public void ChangeAddress(string newAddress)
    {
        if (string.IsNullOrWhiteSpace(newAddress))
            throw new ArgumentException("Email address is required.", nameof(newAddress));

        Address = newAddress.Trim().ToLowerInvariant();
        IsVerified = false;
        VerifiedAt = null;
        MarkUpdated();
    }

    /// <summary>
    /// Phase 3C — reset verification state without changing the address.
    /// Kept as a separate, explicit verb so callers that only need to
    /// revoke verification (without retargeting) do not accidentally
    /// touch <see cref="Address"/>.
    /// </summary>
    public void ResetVerification()
    {
        IsVerified = false;
        VerifiedAt = null;
        MarkUpdated();
    }
}
