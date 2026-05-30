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

    public void ChangeAddress(string newAddress)
    {
        if (string.IsNullOrWhiteSpace(newAddress))
            throw new ArgumentException("Email address is required.", nameof(newAddress));

        Address = newAddress.Trim().ToLowerInvariant();
        IsVerified = false;
        VerifiedAt = null;
        MarkUpdated();
    }

    public void ResetVerification()
    {
        IsVerified = false;
        VerifiedAt = null;
        MarkUpdated();
    }
}
