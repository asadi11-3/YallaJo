using ContentCore.Application.Authorization;
using ContentCore.Application.Commands.EntityTag.RemoveTagFromEntity;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Tests.Unit;

/// <summary>
/// Authorization regression tests for <see cref="RemoveTagFromEntityCommandHandler"/>.
/// Verifies admin-tier-or-owner rule via IOwnershipGuard (CONTENTCORE-STD-P1-003 Phase C4).
/// </summary>
public sealed class RemoveTagFromEntityCommandHandlerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RemoveTagFromEntityCommandHandler BuildHandler(
        IEntityTagRepository? entityTagRepository = null,
        IContentCoreUnitOfWork? unitOfWork = null,
        Microsoft.Extensions.Caching.Hybrid.HybridCache? cache = null,
        IOwnershipGuard? ownershipGuard = null)
    {
        return new RemoveTagFromEntityCommandHandler(
            entityTagRepository ?? Substitute.For<IEntityTagRepository>(),
            unitOfWork ?? OwnershipAuthFixture.NoOpUnitOfWork(),
            cache ?? OwnershipAuthFixture.NoOpCache(),
            ownershipGuard ?? OwnershipAuthFixture.GuardAllowing(),
            Substitute.For<ILogger<RemoveTagFromEntityCommandHandler>>());
    }

    private static RemoveTagFromEntityCommand ValidCommand(Guid entityId) =>
        new(OwnershipAuthFixture.ValidEntityTypeString, entityId, Guid.NewGuid());

    // ── EntityType parse guard ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenEntityTypeIsUnrecognized()
    {
        var handler = BuildHandler();
        var command = new RemoveTagFromEntityCommand(
            OwnershipAuthFixture.InvalidEntityTypeString, Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e =>
            e.Code == "EntityTag.InvalidEntityType" &&
            e.Message == "Invalid entity type.");
    }

    // ── Guard denials propagate correctly ─────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenGuardDenies()
    {
        var entityTagRepo = Substitute.For<IEntityTagRepository>();
        var cache = OwnershipAuthFixture.NoOpCache();

        var handler = BuildHandler(
            entityTagRepository: entityTagRepo,
            cache: cache,
            ownershipGuard: OwnershipAuthFixture.GuardForbidden());

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        entityTagRepo.DidNotReceiveWithAnyArgs().Remove(default!);
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync((string)default!, cancellationToken: default);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenGuardDeniesWithNotFound()
    {
        var entityTagRepo = Substitute.For<IEntityTagRepository>();

        var handler = BuildHandler(
            entityTagRepository: entityTagRepo,
            ownershipGuard: OwnershipAuthFixture.GuardNotFound());

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        entityTagRepo.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenGuardDeniesWithDeleted()
    {
        var entityTagRepo = Substitute.For<IEntityTagRepository>();

        var handler = BuildHandler(
            entityTagRepository: entityTagRepo,
            ownershipGuard: OwnershipAuthFixture.GuardInvalid("EntityTag.TargetDeleted", "Entity is deleted."));

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "EntityTag.TargetDeleted");
        entityTagRepo.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    // ── Guard is invoked with correct parameters ─────────────────────────────

    [Fact]
    public async Task Handle_ShouldCallGuard_WithCorrectEntityTypeAndId()
    {
        var guard = OwnershipAuthFixture.GuardAllowing();
        var entityTagRepo = Substitute.For<IEntityTagRepository>();
        entityTagRepo
            .GetByEntityAsync(Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = BuildHandler(entityTagRepository: entityTagRepo, ownershipGuard: guard);
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
    public async Task Handle_ShouldReachTagLookup_WhenGuardAllows()
    {
        var entityTagRepo = Substitute.For<IEntityTagRepository>();
        entityTagRepo
            .GetByEntityAsync(Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = BuildHandler(
            entityTagRepository: entityTagRepo,
            ownershipGuard: OwnershipAuthFixture.GuardAllowing(isAdminTier: true));

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        // Handler reached the tag-lookup stage (EntityTag.NotFound)
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "EntityTag.NotFound");
    }
}
