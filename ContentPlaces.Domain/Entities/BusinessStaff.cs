using ContentPlaces.Domain.Enums;
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
}
