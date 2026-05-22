using ContentCore.Application.Commands.EntityCategory.AssignCategoriesToEntity;
using ContentCore.Application.Interfaces;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Tests.Unit;

/// <summary>
/// Authorization regression tests for <see cref="AssignCategoriesToEntityCommandHandler"/>.
/// Verifies admin-tier-or-owner rule added in CONTENTCORE-STD-P1-003 Phase C4.
/// </summary>
public sealed class AssignCategoriesToEntityCommandHandlerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AssignCategoriesToEntityCommandHandler BuildHandler(
        IEntityCategoryRepository? entityCategoryRepository = null,
        ICategoryRepository? categoryRepository = null,
        IContentCoreUnitOfWork? unitOfWork = null,
        Microsoft.Extensions.Caching.Hybrid.HybridCache? cache = null,
        YallaJo.SharedKernel.Application.Abstractions.Context.ICurrentUser? currentUser = null,
        ContentCore.Application.Authorization.IEntityOwnershipResolver? ownershipResolver = null)
    {
        return new AssignCategoriesToEntityCommandHandler(
            entityCategoryRepository ?? Substitute.For<IEntityCategoryRepository>(),
            categoryRepository ?? Substitute.For<ICategoryRepository>(),
            unitOfWork ?? OwnershipAuthFixture.NoOpUnitOfWork(),
            Substitute.For<IContentCoreOutboxWriter>(),
            cache ?? OwnershipAuthFixture.NoOpCache(),
            currentUser ?? OwnershipAuthFixture.NonAdminUser(Guid.NewGuid()),
            ownershipResolver ?? OwnershipAuthFixture.ResolverReturning(
                OwnershipAuthFixture.ValidOwner(Guid.NewGuid())),
            Substitute.For<ILogger<AssignCategoriesToEntityCommandHandler>>());
    }

    private static AssignCategoriesToEntityCommand ValidCommand(Guid entityId) =>
        new(OwnershipAuthFixture.ValidEntityTypeString, entityId, [Guid.NewGuid()]);

    // ── Authentication guard ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIdIsNull()
    {
        var handler = BuildHandler(currentUser: OwnershipAuthFixture.UnauthenticatedUser());

        var result = await handler.Handle(
            ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
    }

    // ── EntityType parse guard ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenEntityTypeIsUnrecognized()
    {
        var userId = Guid.NewGuid();
        var handler = BuildHandler(currentUser: OwnershipAuthFixture.NonAdminUser(userId));
        var command = new AssignCategoriesToEntityCommand(
            OwnershipAuthFixture.InvalidEntityTypeString, Guid.NewGuid(), [Guid.NewGuid()]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e =>
            e.Code == "EntityCategory.InvalidEntityType" &&
            e.Message == "Invalid entity type.");
    }

    // ── Ownership probe — target not found ────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTargetEntityDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var resolver = OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.NotFound());
        var entityCategoryRepo = Substitute.For<IEntityCategoryRepository>();
        var cache = OwnershipAuthFixture.NoOpCache();

        var handler = BuildHandler(
            entityCategoryRepository: entityCategoryRepo,
            cache: cache,
            currentUser: OwnershipAuthFixture.NonAdminUser(userId),
            ownershipResolver: resolver);

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        entityCategoryRepo.DidNotReceiveWithAnyArgs().Add(default!);
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync((string)default!, cancellationToken: default);
    }

    // ── Ownership probe — target deleted ──────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenTargetEntityIsDeleted()
    {
        var userId = Guid.NewGuid();
        var resolver = OwnershipAuthFixture.ResolverReturning(
            OwnershipAuthFixture.Deleted(ownerUserId: userId));
        var entityCategoryRepo = Substitute.For<IEntityCategoryRepository>();
        var cache = OwnershipAuthFixture.NoOpCache();

        var handler = BuildHandler(
            entityCategoryRepository: entityCategoryRepo,
            cache: cache,
            currentUser: OwnershipAuthFixture.NonAdminUser(userId),
            ownershipResolver: resolver);

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "EntityCategory.TargetDeleted");
        entityCategoryRepo.DidNotReceiveWithAnyArgs().Add(default!);
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync((string)default!, cancellationToken: default);
    }

    // ── Ownership probe — unsupported EntityType ──────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenEntityTypeIsUnsupported()
    {
        var userId = Guid.NewGuid();
        var resolver = OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.Unsupported());
        var entityCategoryRepo = Substitute.For<IEntityCategoryRepository>();

        var handler = BuildHandler(
            entityCategoryRepository: entityCategoryRepo,
            currentUser: OwnershipAuthFixture.NonAdminUser(userId),
            ownershipResolver: resolver);

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "EntityCategory.UnsupportedEntityType");
        entityCategoryRepo.DidNotReceiveWithAnyArgs().Add(default!);
    }

    // ── Non-admin, non-owner ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenCallerIsNotOwnerAndNotAdminTier()
    {
        var callerId = Guid.NewGuid();
        var differentOwnerId = Guid.NewGuid();
        var entityCategoryRepo = Substitute.For<IEntityCategoryRepository>();
        var cache = OwnershipAuthFixture.NoOpCache();
        var resolver = OwnershipAuthFixture.ResolverReturning(
            OwnershipAuthFixture.ValidOwner(differentOwnerId)); // caller ≠ owner

        var handler = BuildHandler(
            entityCategoryRepository: entityCategoryRepo,
            cache: cache,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: resolver);

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        // Guard must fire before any mutation
        entityCategoryRepo.DidNotReceiveWithAnyArgs().Add(default!);
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync((string)default!, cancellationToken: default);
    }

    // ── Non-admin, owner ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldCallResolver_WhenCallerIsNonAdminTier()
    {
        var userId = Guid.NewGuid();
        var resolver = OwnershipAuthFixture.ResolverReturning(
            OwnershipAuthFixture.ValidOwner(userId)); // caller IS owner
        var entityCategoryRepo = Substitute.For<IEntityCategoryRepository>();
        var categoryRepo = Substitute.For<ICategoryRepository>();
        // No categories found → NotFound short-circuit; but resolver was called first
        categoryRepo.GetAllAsync(filter: null!, ct: default)
            .ReturnsForAnyArgs([]);

        var handler = BuildHandler(
            entityCategoryRepository: entityCategoryRepo,
            categoryRepository: categoryRepo,
            currentUser: OwnershipAuthFixture.NonAdminUser(userId),
            ownershipResolver: resolver);

        await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        // Resolver must have been called exactly once
        await resolver.Received(1)
            .ResolveAsync(
                OwnershipAuthFixture.ValidEntityType,
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    // ── Admin-tier — resolver must NOT be called ──────────────────────────────

    [Fact]
    public async Task Handle_ShouldNotCallResolver_WhenCallerIsAdminTier()
    {
        var userId = Guid.NewGuid();
        var resolver = Substitute.For<ContentCore.Application.Authorization.IEntityOwnershipResolver>();
        var categoryRepo = Substitute.For<ICategoryRepository>();
        // Return empty category list → NotFound, but guard was already bypassed
        categoryRepo.GetAllAsync(filter: null!, ct: default).ReturnsForAnyArgs([]);

        var handler = BuildHandler(
            categoryRepository: categoryRepo,
            currentUser: OwnershipAuthFixture.AdminUser(userId),
            ownershipResolver: resolver);

        await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        await resolver.DidNotReceiveWithAnyArgs()
            .ResolveAsync(default, default, default);
    }

    // ── Admin-tier skips resolver and proceeds to operation ──────────────────

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenAdminTier_IsNot_BlockedByResolver()
    {
        // Sanity: resolver returning NotFound should NOT block admin-tier.
        // We prove this by supplying a resolver that would return NotFound for
        // non-admins, then show that admin-tier handler still calls the category repo.
        var userId = Guid.NewGuid();
        var resolver = OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.NotFound());
        var categoryRepo = Substitute.For<ICategoryRepository>();
        categoryRepo.GetAllAsync(filter: null!, ct: default).ReturnsForAnyArgs([]);

        var handler = BuildHandler(
            categoryRepository: categoryRepo,
            currentUser: OwnershipAuthFixture.AdminUser(userId),
            ownershipResolver: resolver);

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        // Handler got past the auth guard (it reached the Category.NotFound check)
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Category.NotFound");
        // Resolver was NOT called
        await resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default);
    }
}
