using ContentCore.Application.Authorization;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;

namespace ContentCore.Tests.Unit;

/// <summary>
/// Shared fixture for EntityCategory / EntityTag authorization tests.
/// Provides pre-configured substitutes and factory helpers to avoid setup repetition.
/// </summary>
internal static class OwnershipAuthFixture
{
    // ── Ownership resolution results ────────────────────────────────────────

    internal static EntityOwnershipResolution ValidOwner(Guid ownerUserId) =>
        new(IsSupported: true, Exists: true, IsDeleted: false, OwnerUserId: ownerUserId);

    internal static EntityOwnershipResolution NotFound() =>
        new(IsSupported: true, Exists: false, IsDeleted: false, OwnerUserId: null);

    internal static EntityOwnershipResolution Deleted(Guid ownerUserId) =>
        new(IsSupported: true, Exists: true, IsDeleted: true, OwnerUserId: ownerUserId);

    internal static EntityOwnershipResolution Unsupported() =>
        new(IsSupported: false, Exists: false, IsDeleted: false, OwnerUserId: null);

    // ── Current-user helpers ────────────────────────────────────────────────

    /// <summary>Authenticated, non-admin user.</summary>
    internal static ICurrentUser NonAdminUser(Guid userId)
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(userId);
        user.IsAuthenticated.Returns(true);
        // Roles returns empty → HighestPrivilegeLevel = None < Admin
        user.Roles.Returns(Enumerable.Empty<string>());
        return user;
    }

    /// <summary>Authenticated admin-tier user.</summary>
    internal static ICurrentUser AdminUser(Guid userId)
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(userId);
        user.IsAuthenticated.Returns(true);
        user.Roles.Returns([AppRoles.Admin]);
        return user;
    }

    /// <summary>Unauthenticated (no UserId).</summary>
    internal static ICurrentUser UnauthenticatedUser()
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns((Guid?)null);
        user.IsAuthenticated.Returns(false);
        user.Roles.Returns(Enumerable.Empty<string>());
        return user;
    }

    // ── Unit of work / cache subs ────────────────────────────────────────────

    internal static IContentCoreUnitOfWork NoOpUnitOfWork()
    {
        var uow = Substitute.For<IContentCoreUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        return uow;
    }

    internal static HybridCache NoOpCache() => Substitute.For<HybridCache>();

    // ── Constants ────────────────────────────────────────────────────────────

    /// <summary>Valid EntityType string used by all positive tests.</summary>
    internal const string ValidEntityTypeString = "Tour";

    /// <summary>Corresponds to <see cref="ValidEntityTypeString"/>.</summary>
    internal static readonly EntityType ValidEntityType = EntityType.Tour;

    internal const string InvalidEntityTypeString = "NotAnEntityType";

    // ── Resolver wiring ───────────────────────────────────────────────────────

    internal static IEntityOwnershipResolver ResolverReturning(
        EntityOwnershipResolution resolution)
    {
        var resolver = Substitute.For<IEntityOwnershipResolver>();
        resolver
            .ResolveAsync(Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(resolution);
        return resolver;
    }
}
