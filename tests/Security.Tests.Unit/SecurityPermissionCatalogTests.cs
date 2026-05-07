using FluentAssertions;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Authorization;

namespace Security.Tests.Unit;

/// <summary>
/// Guards the contract that every permission used by Security endpoints
/// (in particular AuditLog.Read used by AuditLogEndpoints) is declared in
/// <see cref="SecurityPermissionCatalog"/>. Without this entry, the
/// permission seeder cannot grant the permission and the endpoint becomes
/// effectively ungrantable.
/// </summary>
public sealed class SecurityPermissionCatalogTests
{
    private readonly SecurityPermissionCatalog _catalog = new();

    [Fact]
    public void Catalog_ShouldDeclare_AuditLogRead_Permission()
    {
        var match = _catalog.Permissions
            .SingleOrDefault(p => p.Feature == SecurityFeatures.AuditLog
                               && p.Action  == AppAction.Read);

        match.Should().NotBeNull(
            "AuditLogEndpoints requires Permission.AuditLog.Read; the catalog must declare it.");
    }

    [Fact]
    public void Catalog_PermissionNames_ShouldBeUnique()
    {
        var duplicates = _catalog.Permissions
            .GroupBy(p => p.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.Should().BeEmpty();
    }
}
