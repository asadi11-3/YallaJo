using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Domain.Entities;

public sealed class BusinessStaff : AuditableEntity
{
    private BusinessStaff()
    {
    }

    public Guid BusinessId { get; private set; }
    public Guid UserId { get; private set; }
    public BusinessStaffRole Role { get; private set; }
    public Guid? JoinRequestId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }

    public Business Business { get; private set; } = default!;

    public static BusinessStaff Create(
        Guid businessId,
        Guid userId,
        BusinessStaffRole role)
    {
        return new BusinessStaff
        {
            Id = Guid.CreateVersion7(),
            BusinessId = businessId,
            UserId = userId,
            Role = role,
            IsActive = true,
            DeactivatedAt = null
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        DeactivatedAt = DateTime.UtcNow;
        MarkUpdated();
    }
}
