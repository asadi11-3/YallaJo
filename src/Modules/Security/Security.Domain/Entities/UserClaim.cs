using YallaJo.SharedKernel.Domain.Entities;

namespace Security.Domain.Entities;

public sealed class UserClaim : AuditableEntity
{
    private UserClaim() { }

    public Guid UserId { get; private set; }
    public string ClaimType { get; private set; } = string.Empty;
    public string ClaimValue { get; private set; } = string.Empty;

    public User User { get; private set; } = default!;

    public static UserClaim Create(Guid userId, string claimType, string claimValue)
    {
        return new UserClaim
        {
            UserId = userId,
            ClaimType = claimType.Trim(),
            ClaimValue = claimValue.Trim()
        };
    }
}
