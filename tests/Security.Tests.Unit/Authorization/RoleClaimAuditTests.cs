using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Security.Contracts.Authorization;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Seeding;
using YallaJo.SharedKernel.Application.Authorization;

namespace Security.Tests.Unit.Authorization;

/// <summary>
/// Verifies the P1 (2026-05-30) AUDIT-ONLY RoleClaim reconciliation: it must
/// REPORT stale/missing claims but never delete (or persist) anything.
/// </summary>
public sealed class RoleClaimAuditTests
{
    private static SecurityDataSeeder BuildSeeder(
        IRoleRepository roleRepo,
        IRoleClaimRepository claimRepo,
        ISecurityUnitOfWork uow,
        RolePermissionMapping mapping)
        => new(roleRepo, claimRepo, uow, mapping, NullLogger<SecurityDataSeeder>.Instance);

    private static RolePermissionMapping BuildMapping(params PermissionDescriptor[] perms)
    {
        var catalog = Substitute.For<IPermissionCatalog>();
        catalog.Permissions.Returns(perms);
        return new RolePermissionMapping(new[] { catalog });
    }

    [Fact]
    public async Task Audit_does_not_delete_or_persist_anything_even_with_stale_claims()
    {
        var role = Role.Create("Admin");

        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetAllAsync(
                Arg.Any<Expression<Func<Role, bool>>>(),
                Arg.Any<Func<IQueryable<Role>, IQueryable<Role>>?>(),
                Arg.Any<Func<IQueryable<Role>, IOrderedQueryable<Role>>?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<Role> { role });

        // Persisted claims include a STALE one (Permission.Tour.Delete) that the
        // authoritative mapping no longer grants.
        var staleClaim = RoleClaim.Create(role.Id, "Permission", "Permission.Tour.Delete");

        var claimRepo = Substitute.For<IRoleClaimRepository>();
        claimRepo.GetAllAsync(
                Arg.Any<Expression<Func<RoleClaim, bool>>>(),
                Arg.Any<Func<IQueryable<RoleClaim>, IQueryable<RoleClaim>>?>(),
                Arg.Any<Func<IQueryable<RoleClaim>, IOrderedQueryable<RoleClaim>>?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<RoleClaim> { staleClaim });

        var uow = Substitute.For<ISecurityUnitOfWork>();
        var mapping = BuildMapping(); // empty mapping → Admin authoritative set is empty

        var seeder = BuildSeeder(roleRepo, claimRepo, uow, mapping);

        await seeder.AuditRoleClaimsAsync(CancellationToken.None);

        // No deletions of any kind.
        claimRepo.DidNotReceive().Remove(Arg.Any<RoleClaim>());
        claimRepo.DidNotReceive().RemoveRange(Arg.Any<IEnumerable<RoleClaim>>());
        await claimRepo.DidNotReceive().ExecuteDeleteAsync(
            Arg.Any<Expression<Func<RoleClaim, bool>>>(), Arg.Any<CancellationToken>());
        await claimRepo.DidNotReceive().DeleteByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        // No inserts and no SaveChanges either — audit is purely diagnostic.
        claimRepo.DidNotReceive().Add(Arg.Any<RoleClaim>());
        await claimRepo.DidNotReceive().AddAsync(Arg.Any<RoleClaim>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
