using YallaJo.SharedKernel.Domain.Entities;

namespace Security.Domain.Entities;

public sealed class RoleClaim : AuditableEntity
{
    private RoleClaim() { } // EF Core

    public Guid RoleId { get; private set; }
    public string ClaimType { get; private set; } = string.Empty;
    public string ClaimValue { get; private set; } = string.Empty;

    public Role Role { get; private set; } = default!;

    public static RoleClaim Create(Guid roleId, string claimType, string claimValue)
    {
        return new RoleClaim
        {
            RoleId = roleId,
            ClaimType = claimType.Trim(),
            ClaimValue = claimValue.Trim()
        };
    }
}
