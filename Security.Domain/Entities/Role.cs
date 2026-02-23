using YallaJo.SharedKernel.Domain.Entities;

namespace Security.Domain.Entities;

public sealed class Role : AuditableEntity
{
    private readonly List<UserRole> _userRoles = [];
    private readonly List<RoleClaim> _roleClaims = [];

    private Role() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<RoleClaim> RoleClaims => _roleClaims.AsReadOnly();

    public static Role Create(string name, string? description = null)
    {
        return new Role
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            IsActive = true
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        MarkUpdated();
    }
}
