using YallaJo.SharedKernel.Domain.Entities;

namespace Security.Domain.Entities;

public sealed class UserRole : AuditableEntity
{
    private UserRole() { }

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    public User User { get; private set; } = default!;
    public Role Role { get; private set; } = default!;

    public static UserRole Create(Guid userId, Guid roleId)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };
    }
}
