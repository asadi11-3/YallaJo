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
/// Verifies admin-tier-or-owner rule added in CONTENTCORE-STD-P1-003 Phase C4.
/// </summary>
public sealed class RemoveTagFromEntityCommandHandlerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RemoveTagFromEntityCommandHandler BuildHandler(
        IEntityTagRepository? entityTagRepository = null,
        IContentCoreUnitOfWork? unitOfWork = null,
        Microsoft.Extensions.Caching.Hybrid.HybridCache? cache = null,
        YallaJo.SharedKernel.Application.Abstractions.Context.ICurrentUser? currentUser = null,
        ContentCore.Application.Authorization.IEntityOwnershipResolver? ownershipResolver = null)
    {
        return new RemoveTagFromEntityCommandHandler(
            entityTagRepository ?? Substitute.For<IEntityTagRepository>(),
            unitOfWork ?? OwnershipAuthFixture.NoOpUnitOfWork(),
            cache ?? OwnershipAuthFixture.NoOpCache(),
            currentUser ?? OwnershipAuthFixture.NonAdminUser(Guid.NewGuid()),
            ownershipResolver ?? OwnershipAuthFixture.ResolverReturning(
                OwnershipAuthFixture.ValidOwner(Guid.NewGuid())),
            Substitute.For<ILogger<RemoveTagFromEntityCommandHandler>>());
    }

    private static RemoveTagFromEntityCommand ValidCommand(Guid entityId) =>
        new(OwnershipAuthFixture.ValidEntityTypeString, entityId, Guid.NewGuid());

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
        var command = new RemoveTagFromEntityCommand(
            OwnershipAuthFixture.InvalidEntityTypeString, Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e =>
            e.Code == "EntityTag.InvalidEntityType" &&
            e.Message == "Invalid entity type.");
    }

    // ── Non-admin, non-owner ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenCallerIsNotOwnerAndNotAdminTier()
    {
        var callerId = Guid.NewGuid();
        var differentOwnerId = Guid.NewGuid();
        var entityTagRepo = Substitute.For<IEntityTagRepository>();
        var cache = OwnershipAuthFixture.NoOpCache();
        var resolver = OwnershipAuthFixture.ResolverReturning(
            OwnershipAuthFixture.ValidOwner(differentOwnerId));

        var handler = BuildHandler(
            entityTagRepository: entityTagRepo,
            cache: cache,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: resolver);

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        entityTagRepo.DidNotReceiveWithAnyArgs().Remove(default!);
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync((string)default!, cancellationToken: default);
    }

    // ── Non-admin, owner — resolver should be called ─────────────────────────

    [Fact]
    public async Task Handle_ShouldCallResolver_WhenCallerIsNonAdminTier()
    {
        var userId = Guid.NewGuid();
        var resolver = OwnershipAuthFixture.ResolverReturning(
            OwnershipAuthFixture.ValidOwner(userId));
        var entityTagRepo = Substitute.For<IEntityTagRepository>();
        // Return empty list → NotFound for the tag, but resolver was called first
        entityTagRepo
            .GetByEntityAsync(Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = BuildHandler(
            entityTagRepository: entityTagRepo,
            currentUser: OwnershipAuthFixture.NonAdminUser(userId),
            ownershipResolver: resolver);

        await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

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
        var entityTagRepo = Substitute.For<IEntityTagRepository>();
        entityTagRepo
            .GetByEntityAsync(Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = BuildHandler(
            entityTagRepository: entityTagRepo,
            currentUser: OwnershipAuthFixture.AdminUser(userId),
            ownershipResolver: resolver);

        await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        await resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default);
    }

    // ── Target not found ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTargetEntityDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var resolver = OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.NotFound());
        var entityTagRepo = Substitute.For<IEntityTagRepository>();

        var handler = BuildHandler(
            entityTagRepository: entityTagRepo,
            currentUser: OwnershipAuthFixture.NonAdminUser(userId),
            ownershipResolver: resolver);

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        entityTagRepo.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    // ── Target deleted ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenTargetEntityIsDeleted()
    {
        var userId = Guid.NewGuid();
        var resolver = OwnershipAuthFixture.ResolverReturning(
            OwnershipAuthFixture.Deleted(ownerUserId: userId));
        var entityTagRepo = Substitute.For<IEntityTagRepository>();

        var handler = BuildHandler(
            entityTagRepository: entityTagRepo,
            currentUser: OwnershipAuthFixture.NonAdminUser(userId),
            ownershipResolver: resolver);

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "EntityTag.TargetDeleted");
        entityTagRepo.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    // ── Admin-tier skips resolver and proceeds to operation ───────────────────

    [Fact]
    public async Task Handle_ShouldReachTagLookup_WhenAdminTier_EvenIfResolverWouldFail()
    {
        var userId = Guid.NewGuid();
        var resolver = OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.NotFound());
        var entityTagRepo = Substitute.For<IEntityTagRepository>();
        entityTagRepo
            .GetByEntityAsync(Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = BuildHandler(
            entityTagRepository: entityTagRepo,
            currentUser: OwnershipAuthFixture.AdminUser(userId),
            ownershipResolver: resolver);

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        // Handler reached the tag-lookup stage (EntityTag.NotFound)
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "EntityTag.NotFound");
        await resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default);
    }
}
