using ContentCore.Application.Authorization;
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
/// Verifies admin-tier-or-owner rule via IOwnershipGuard (CONTENTCORE-STD-P1-003 Phase C4).
/// </summary>
public sealed class AssignCategoriesToEntityCommandHandlerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AssignCategoriesToEntityCommandHandler BuildHandler(
        IEntityCategoryRepository? entityCategoryRepository = null,
        ICategoryRepository? categoryRepository = null,
        IContentCoreUnitOfWork? unitOfWork = null,
        Microsoft.Extensions.Caching.Hybrid.HybridCache? cache = null,
        IOwnershipGuard? ownershipGuard = null)
    {
        return new AssignCategoriesToEntityCommandHandler(
            entityCategoryRepository ?? Substitute.For<IEntityCategoryRepository>(),
            categoryRepository ?? Substitute.For<ICategoryRepository>(),
            unitOfWork ?? OwnershipAuthFixture.NoOpUnitOfWork(),
            Substitute.For<IContentCoreOutboxWriter>(),
            cache ?? OwnershipAuthFixture.NoOpCache(),
            ownershipGuard ?? OwnershipAuthFixture.GuardAllowing(),
            Substitute.For<ILogger<AssignCategoriesToEntityCommandHandler>>());
    }

    private static AssignCategoriesToEntityCommand ValidCommand(Guid entityId) =>
        new(OwnershipAuthFixture.ValidEntityTypeString, entityId, [Guid.NewGuid()]);

    // ── EntityType parse guard ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenEntityTypeIsUnrecognized()
    {
        var handler = BuildHandler();
        var command = new AssignCategoriesToEntityCommand(
            OwnershipAuthFixture.InvalidEntityTypeString, Guid.NewGuid(), [Guid.NewGuid()]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e =>
            e.Code == "EntityCategory.InvalidEntityType" &&
            e.Message == "Invalid entity type.");
    }

    // ── Guard denials propagate correctly ─────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenGuardDeniesWithNotFound()
    {
        var entityCategoryRepo = Substitute.For<IEntityCategoryRepository>();
        var cache = OwnershipAuthFixture.NoOpCache();

        var handler = BuildHandler(
            entityCategoryRepository: entityCategoryRepo,
            cache: cache,
            ownershipGuard: OwnershipAuthFixture.GuardNotFound());

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        entityCategoryRepo.DidNotReceiveWithAnyArgs().Add(default!);
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync((string)default!, cancellationToken: default);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenGuardDeniesWithDeleted()
    {
        var entityCategoryRepo = Substitute.For<IEntityCategoryRepository>();
        var cache = OwnershipAuthFixture.NoOpCache();

        var handler = BuildHandler(
            entityCategoryRepository: entityCategoryRepo,
            cache: cache,
            ownershipGuard: OwnershipAuthFixture.GuardInvalid("EntityCategory.TargetDeleted", "Entity is deleted."));

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "EntityCategory.TargetDeleted");
        entityCategoryRepo.DidNotReceiveWithAnyArgs().Add(default!);
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync((string)default!, cancellationToken: default);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenGuardDeniesWithUnsupported()
    {
        var entityCategoryRepo = Substitute.For<IEntityCategoryRepository>();

        var handler = BuildHandler(
            entityCategoryRepository: entityCategoryRepo,
            ownershipGuard: OwnershipAuthFixture.GuardInvalid("EntityCategory.UnsupportedEntityType", "Unsupported entity type."));

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "EntityCategory.UnsupportedEntityType");
        entityCategoryRepo.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenGuardDenies()
    {
        var entityCategoryRepo = Substitute.For<IEntityCategoryRepository>();
        var cache = OwnershipAuthFixture.NoOpCache();

        var handler = BuildHandler(
            entityCategoryRepository: entityCategoryRepo,
            cache: cache,
            ownershipGuard: OwnershipAuthFixture.GuardForbidden());

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        entityCategoryRepo.DidNotReceiveWithAnyArgs().Add(default!);
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync((string)default!, cancellationToken: default);
    }

    // ── Guard is invoked with correct parameters ─────────────────────────────

    [Fact]
    public async Task Handle_ShouldCallGuard_WithCorrectEntityTypeAndId()
    {
        var guard = OwnershipAuthFixture.GuardAllowing();
        var categoryRepo = Substitute.For<ICategoryRepository>();
        categoryRepo.GetAllAsync(filter: null!, ct: default).ReturnsForAnyArgs([]);

        var handler = BuildHandler(categoryRepository: categoryRepo, ownershipGuard: guard);
        var entityId = Guid.NewGuid();

        await handler.Handle(ValidCommand(entityId), CancellationToken.None);

        await guard.Received(1)
            .AuthorizeAsync(
                OwnershipAuthFixture.ValidEntityType,
                entityId,
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>());
    }

    // ── Guard success proceeds to operation ───────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReachCategoryLookup_WhenGuardAllows()
    {
        var categoryRepo = Substitute.For<ICategoryRepository>();
        categoryRepo.GetAllAsync(filter: null!, ct: default).ReturnsForAnyArgs([]);

        var handler = BuildHandler(
            categoryRepository: categoryRepo,
            ownershipGuard: OwnershipAuthFixture.GuardAllowing(isAdminTier: true));

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        // Handler got past the guard (it reached the Category.NotFound check)
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Category.NotFound");
    }
}
