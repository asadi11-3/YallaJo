namespace YallaJo.SharedKernel.Application.Authorization;

/// <summary>
/// Implemented by each module's Contracts project to publish its permission surface.
/// <para>
/// Security.Infrastructure discovers all registered implementations via DI
/// (<see cref="IEnumerable{IPermissionCatalog}"/>) and seeds the database.
/// No module references another module's catalog — Security only aggregates.
/// </para>
/// <para>
/// Registration: in <c>{Module}.Infrastructure/DependencyInjection.cs</c>:
/// <code>services.AddSingleton&lt;IPermissionCatalog, {Module}PermissionCatalog&gt;();</code>
/// </para>
/// </summary>
public interface IPermissionCatalog
{
    /// <summary>Stable module identifier — used for grouping in admin UI + logs.</summary>
    string ModuleName { get; }

    /// <summary>All permissions this module contributes to the system.</summary>
    IReadOnlyList<PermissionDescriptor> Permissions { get; }
}
