using YallaJo.SharedKernel.Domain.Entities;

namespace Security.Domain.Entities;

public sealed class Phone : AuditableEntity
{
    private Phone() { } // EF Core

    public Guid UserId { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    public User User { get; private set; } = default!;

    public static Phone Create(Guid userId, string phoneNumber, bool isPrimary)
    {
        return new Phone
        {
            UserId = userId,
            PhoneNumber = phoneNumber.Trim(),
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
}
