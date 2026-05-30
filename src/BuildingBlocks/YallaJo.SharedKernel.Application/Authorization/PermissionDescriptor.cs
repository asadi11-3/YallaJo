namespace YallaJo.SharedKernel.Application.Authorization;

/// <summary>
/// Immutable description of a single permission.
/// Used by <see cref="IPermissionCatalog"/> implementations to publish the
/// module's permission surface. The seeder in Security.Infrastructure aggregates
/// all descriptors from all catalogs and seeds the database.
/// </summary>
public sealed record PermissionDescriptor(
    string Feature,
    string Action,
    string Group,
    string Description,
    bool IsGuestAccessible = false)
{
    /// <summary>
    /// Deterministic claim value — must exactly match the policy name built by
    /// <c>MustHavePermissionAttribute</c> via <c>PermissionPolicyNames.Build()</c>.
    /// </summary>
    public string Name => $"Permission.{Feature}.{Action}";
}
