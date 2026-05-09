using ContentCore.Application.Authorization;
using ContentCore.Application.Caching;
using ContentCore.Application.Commands.Attachment.DeleteAttachment;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Events;
using ContentCore.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Tests.Unit;

file static class DeleteAttachmentHandlerBuilder
{
    internal static DeleteAttachmentCommandHandler Build(
        IAttachmentRepository attachmentRepository,
        ICurrentUser? currentUser = null,
        IEntityOwnershipResolver? ownershipResolver = null,
        IContentCoreUnitOfWork? unitOfWork = null,
        IFileStorageService? fileStorageService = null,
        HybridCache? cache = null)
    {
        return new DeleteAttachmentCommandHandler(
            attachmentRepository,
            fileStorageService ?? Substitute.For<IFileStorageService>(),
            unitOfWork ?? OwnershipAuthFixture.NoOpUnitOfWork(),
            currentUser ?? OwnershipAuthFixture.NonAdminUser(Guid.NewGuid()),
            ownershipResolver ?? OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(Guid.NewGuid())),
            cache ?? OwnershipAuthFixture.NoOpCache(),
            Substitute.For<ILogger<DeleteAttachmentCommandHandler>>());
    }

    internal static Attachment BuildAttachment(Guid entityId, Guid uploadedByUserId) =>
        Attachment.Create(EntityType.Tour, entityId, AttachmentType.Image, "https://cdn/app/photo.jpg", uploadedByUserId);
}

public sealed class DeleteAttachmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIdIsNull()
    {
        var repository = Substitute.For<IAttachmentRepository>();
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var storage = Substitute.For<IFileStorageService>();
        var cache = Substitute.For<HybridCache>();
        var resolver = Substitute.For<IEntityOwnershipResolver>();
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.UnauthenticatedUser(),
            ownershipResolver: resolver,
            unitOfWork: unitOfWork,
            fileStorageService: storage,
            cache: cache);

        var result = await handler.Handle(new DeleteAttachmentCommand(Guid.NewGuid()), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Unauthorized);
        await repository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default, default);
        await resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await storage.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
        await cache.DidNotReceiveWithAnyArgs().RemoveByTagAsync((string)default!, default);
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenUploaderIsNotOwner()
    {
        var callerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachment = DeleteAttachmentHandlerBuilder.BuildAttachment(entityId, callerId);

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), false).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var storage = Substitute.For<IFileStorageService>();
        var cache = Substitute.For<HybridCache>();
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(ownerId)),
            unitOfWork: unitOfWork,
            fileStorageService: storage,
            cache: cache);

        var result = await handler.Handle(new DeleteAttachmentCommand(attachment.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        repository.DidNotReceiveWithAnyArgs().Remove(default!);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await storage.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
        await cache.DidNotReceiveWithAnyArgs().RemoveByTagAsync((string)default!, default);
    }

    [Fact]
    public async Task Handle_ShouldAllowOwner_WhenCallerOwnsTargetEntity()
    {
        var callerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachment = DeleteAttachmentHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid());

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), false).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var storage = Substitute.For<IFileStorageService>();
        storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var cache = Substitute.For<HybridCache>();
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(callerId)),
            unitOfWork: unitOfWork,
            fileStorageService: storage,
            cache: cache);

        var result = await handler.Handle(new DeleteAttachmentCommand(attachment.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Received(1).Remove(attachment);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await storage.Received(1).DeleteAsync(attachment.Url, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSkipResolver_WhenCallerIsAdminTier()
    {
        var callerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachment = DeleteAttachmentHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid());

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), false).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var storage = Substitute.For<IFileStorageService>();
        storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var cache = Substitute.For<HybridCache>();
        var resolver = Substitute.For<IEntityOwnershipResolver>();
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.AdminUser(callerId),
            ownershipResolver: resolver,
            unitOfWork: unitOfWork,
            fileStorageService: storage,
            cache: cache);

        var result = await handler.Handle(new DeleteAttachmentCommand(attachment.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default);
    }

    [Fact]
    public async Task Handle_ShouldMapOwnershipNotFound_AndNotMutate()
    {
        var callerId = Guid.NewGuid();
        var attachment = DeleteAttachmentHandlerBuilder.BuildAttachment(Guid.NewGuid(), Guid.NewGuid());

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), false).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var storage = Substitute.For<IFileStorageService>();
        var cache = Substitute.For<HybridCache>();
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.NotFound()),
            unitOfWork: unitOfWork,
            fileStorageService: storage,
            cache: cache);

        var result = await handler.Handle(new DeleteAttachmentCommand(attachment.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().Contain(x => x.Code == "Attachment.TargetNotFound");
        repository.DidNotReceiveWithAnyArgs().Remove(default!);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}

/// <summary>
/// T3 regression tests — CONTENTCORE-STD-P0-001.
/// Proves that <c>attachment.MarkForDeletion()</c> is called on the success path
/// (so <see cref="AttachmentDeletedDomainEvent"/> is raised) and is NOT called
/// on any authorization-failure or lookup-failure path.
/// Also verifies both required cache tags are evicted on success.
/// </summary>
public sealed class DeleteAttachmentEventRegressionTests
{
    // ── success path: domain event raised ────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldRaiseAttachmentDeletedDomainEvent_WhenOwnerDeletes()
    {
        var callerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachment = DeleteAttachmentHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid());

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), false).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var storage = Substitute.For<IFileStorageService>();
        storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(callerId)),
            unitOfWork: unitOfWork,
            fileStorageService: storage);

        var result = await handler.Handle(new DeleteAttachmentCommand(attachment.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        attachment.DomainEvents
            .OfType<AttachmentDeletedDomainEvent>()
            .Should().ContainSingle("MarkForDeletion must be called before Remove on the success path");
    }

    [Fact]
    public async Task Handle_ShouldRaiseAttachmentDeletedDomainEvent_WhenAdminDeletes()
    {
        var adminId = Guid.NewGuid();
        var attachment = DeleteAttachmentHandlerBuilder.BuildAttachment(Guid.NewGuid(), Guid.NewGuid());

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), false).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var storage = Substitute.For<IFileStorageService>();
        storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.AdminUser(adminId),
            unitOfWork: unitOfWork,
            fileStorageService: storage);

        var result = await handler.Handle(new DeleteAttachmentCommand(attachment.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        attachment.DomainEvents
            .OfType<AttachmentDeletedDomainEvent>()
            .Should().ContainSingle("admin-tier bypasses resolver but must still mark for deletion");
    }

    // ── success path: domain event payload ───────────────────────────────────

    [Fact]
    public async Task Handle_ShouldRaiseDomainEventWithCorrectFields_WhenDeleteSucceeds()
    {
        var callerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachment = DeleteAttachmentHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid());

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), false).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var storage = Substitute.For<IFileStorageService>();
        storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(callerId)),
            unitOfWork: unitOfWork,
            fileStorageService: storage);

        await handler.Handle(new DeleteAttachmentCommand(attachment.Id), CancellationToken.None);

        var evt = attachment.DomainEvents.OfType<AttachmentDeletedDomainEvent>().Single();
        evt.AttachmentId.Should().Be(attachment.Id);
        evt.EntityType.Should().Be(EntityType.Tour);
        evt.EntityId.Should().Be(entityId);
        evt.AttachmentType.Should().Be(AttachmentType.Image);
        evt.Url.Should().Be(attachment.Url);
    }

    // ── success path: cache eviction ─────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldEvictEntityAttachmentsTag_WhenDeleteSucceeds()
    {
        var callerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachment = DeleteAttachmentHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid());

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), false).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var storage = Substitute.For<IFileStorageService>();
        storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var cache = Substitute.For<HybridCache>();
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(callerId)),
            unitOfWork: unitOfWork,
            fileStorageService: storage,
            cache: cache);

        await handler.Handle(new DeleteAttachmentCommand(attachment.Id), CancellationToken.None);

        // Fine-grained entity list tag: only this entity's attachments are evicted.
        var expectedEntityTag = ContentCoreCacheKeys.EntityAttachmentsTag(
            EntityType.Tour.ToString(), entityId);
        await cache.Received(1).RemoveByTagAsync(expectedEntityTag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldEvictSingleAttachmentTag_WhenDeleteSucceeds()
    {
        var callerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachment = DeleteAttachmentHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid());

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), false).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var storage = Substitute.For<IFileStorageService>();
        storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var cache = Substitute.For<HybridCache>();
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(callerId)),
            unitOfWork: unitOfWork,
            fileStorageService: storage,
            cache: cache);

        await handler.Handle(new DeleteAttachmentCommand(attachment.Id), CancellationToken.None);

        // Per-attachment tag: only this attachment's cache entry is evicted.
        var expectedAttachmentTag = ContentCoreCacheKeys.AttachmentTag(attachment.Id);
        await cache.Received(1).RemoveByTagAsync(expectedAttachmentTag, Arg.Any<CancellationToken>());
    }

    // ── failure paths: no domain event ───────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldNotRaiseDomainEvent_WhenForbidden()
    {
        var callerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachment = DeleteAttachmentHandlerBuilder.BuildAttachment(entityId, callerId);

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), false).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var storage = Substitute.For<IFileStorageService>();
        var cache = Substitute.For<HybridCache>();
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(ownerId)),
            unitOfWork: unitOfWork,
            fileStorageService: storage,
            cache: cache);

        var result = await handler.Handle(new DeleteAttachmentCommand(attachment.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        attachment.DomainEvents
            .OfType<AttachmentDeletedDomainEvent>()
            .Should().BeEmpty("MarkForDeletion must NOT be called on forbidden paths");
        repository.DidNotReceiveWithAnyArgs().Remove(default!);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await storage.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
        await cache.DidNotReceiveWithAnyArgs().RemoveByTagAsync((string)default!, default);
    }

    [Fact]
    public async Task Handle_ShouldNotRaiseDomainEvent_WhenAttachmentNotFound()
    {
        var repository = Substitute.For<IAttachmentRepository>();
        repository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), false)
            .Returns((Attachment?)null);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var storage = Substitute.For<IFileStorageService>();
        var cache = Substitute.For<HybridCache>();
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(Guid.NewGuid()),
            unitOfWork: unitOfWork,
            fileStorageService: storage,
            cache: cache);

        var result = await handler.Handle(new DeleteAttachmentCommand(Guid.NewGuid()), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().Contain(e => e.Code == "Attachment.NotFound");
        repository.DidNotReceiveWithAnyArgs().Remove(default!);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await storage.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
        await cache.DidNotReceiveWithAnyArgs().RemoveByTagAsync((string)default!, default);
    }

    [Fact]
    public async Task Handle_ShouldNotRaiseDomainEvent_WhenTargetEntityNotFound()
    {
        var callerId = Guid.NewGuid();
        var attachment = DeleteAttachmentHandlerBuilder.BuildAttachment(Guid.NewGuid(), Guid.NewGuid());

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), false).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var storage = Substitute.For<IFileStorageService>();
        var cache = Substitute.For<HybridCache>();
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.NotFound()),
            unitOfWork: unitOfWork,
            fileStorageService: storage,
            cache: cache);

        var result = await handler.Handle(new DeleteAttachmentCommand(attachment.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().Contain(e => e.Code == "Attachment.TargetNotFound");
        attachment.DomainEvents
            .OfType<AttachmentDeletedDomainEvent>()
            .Should().BeEmpty("MarkForDeletion must NOT be called when the target entity is missing");
        repository.DidNotReceiveWithAnyArgs().Remove(default!);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await storage.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
        await cache.DidNotReceiveWithAnyArgs().RemoveByTagAsync((string)default!, default);
    }

    [Fact]
    public async Task Handle_ShouldNotRaiseDomainEvent_WhenTargetEntityIsDeleted()
    {
        var callerId = Guid.NewGuid();
        var attachment = DeleteAttachmentHandlerBuilder.BuildAttachment(Guid.NewGuid(), Guid.NewGuid());

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), false).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var storage = Substitute.For<IFileStorageService>();
        var cache = Substitute.For<HybridCache>();
        var handler = DeleteAttachmentHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.Deleted(callerId)),
            unitOfWork: unitOfWork,
            fileStorageService: storage,
            cache: cache);

        var result = await handler.Handle(new DeleteAttachmentCommand(attachment.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().Contain(e => e.Code == "Attachment.TargetDeleted");
        attachment.DomainEvents
            .OfType<AttachmentDeletedDomainEvent>()
            .Should().BeEmpty("MarkForDeletion must NOT be called when the target entity is soft-deleted");
        repository.DidNotReceiveWithAnyArgs().Remove(default!);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await storage.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
        await cache.DidNotReceiveWithAnyArgs().RemoveByTagAsync((string)default!, default);
    }
}
