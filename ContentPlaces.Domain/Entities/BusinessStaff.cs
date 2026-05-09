using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Events;
using ContentPlaces.Domain.Events.BusinessStaffEvents;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Domain.Entities;

public sealed class BusinessStaff : AuditableEntity
{
    private BusinessStaff() { } // EF Core

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
        var staff = new BusinessStaff
        {
            Id = Guid.CreateVersion7(),
            BusinessId = businessId,
            UserId = userId,
            Role = role,
            IsActive = true,
            DeactivatedAt = null
        };

        staff.AddDomainEvent(
            new BusinessStaffAddedDomainEvent(
                staff.Id,
                staff.BusinessId,
                staff.UserId,
                staff.Role.ToString()));

        return staff;
    }

    public void Deactivate()
    {
        IsActive = false;
        DeactivatedAt = DateTime.UtcNow;
        MarkUpdated();

        AddDomainEvent(
            new BusinessStaffRemovedDomainEvent(
                Id,
                BusinessId,
                UserId));
    }
}
