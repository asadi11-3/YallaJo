using ContentCore.Application.Authorization;
using ContentCore.Application.Commands.Attachment.ReorderAttachments;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Tests.Unit;

file static class ReorderAttachmentsHandlerBuilder
{
    internal static ReorderAttachmentsCommandHandler Build(
        IAttachmentRepository attachmentRepository,
        ICurrentUser? currentUser = null,
        IEntityOwnershipResolver? ownershipResolver = null,
        IContentCoreUnitOfWork? unitOfWork = null,
        HybridCache? cache = null)
    {
        return new ReorderAttachmentsCommandHandler(
            attachmentRepository,
            unitOfWork ?? OwnershipAuthFixture.NoOpUnitOfWork(),
            currentUser ?? OwnershipAuthFixture.NonAdminUser(Guid.NewGuid()),
            ownershipResolver ?? OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(Guid.NewGuid())),
            cache ?? OwnershipAuthFixture.NoOpCache(),
            Substitute.For<ILogger<ReorderAttachmentsCommandHandler>>());
    }

    internal static Attachment BuildAttachment(Guid entityId, Guid uploaderId, int sortOrder) =>
        Attachment.Create(EntityType.Tour, entityId, AttachmentType.Image, $"https://cdn/app/{sortOrder}.jpg", uploaderId, sortOrder: sortOrder);
}

public sealed class ReorderAttachmentsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIdIsNull()
    {
        var repository = Substitute.For<IAttachmentRepository>();
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var resolver = Substitute.For<IEntityOwnershipResolver>();
        var handler = ReorderAttachmentsHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.UnauthenticatedUser(),
            ownershipResolver: resolver,
            unitOfWork: unitOfWork,
            cache: cache);

        var result = await handler.Handle(
            new ReorderAttachmentsCommand(EntityType.Tour, Guid.NewGuid(), Array.Empty<Guid>()),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Unauthorized);
        await repository.DidNotReceiveWithAnyArgs().GetAllAsync(default, default, default, default, default);
        await resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default);
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenUploaderIsNotOwner()
    {
        var callerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachments = new List<Attachment>
        {
            ReorderAttachmentsHandlerBuilder.BuildAttachment(entityId, callerId, 0),
            ReorderAttachmentsHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid(), 1)
        };
        var orderedIds = attachments.Select(x => x.Id).Reverse().ToList();

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetAllAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Attachment, bool>>>(), null, null, false, Arg.Any<CancellationToken>())
            .Returns(attachments);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var handler = ReorderAttachmentsHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(ownerId)),
            unitOfWork: unitOfWork,
            cache: cache);

        var result = await handler.Handle(new ReorderAttachmentsCommand(EntityType.Tour, entityId, orderedIds), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        attachments[0].SortOrder.Should().Be(0);
        attachments[1].SortOrder.Should().Be(1);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await cache.DidNotReceiveWithAnyArgs().RemoveByTagAsync((string)default!, default);
    }

    [Fact]
    public async Task Handle_ShouldAllowOwner_WhenCallerOwnsTargetEntity()
    {
        var callerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachments = new List<Attachment>
        {
            ReorderAttachmentsHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid(), 0),
            ReorderAttachmentsHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid(), 1)
        };
        var orderedIds = attachments.Select(x => x.Id).Reverse().ToList();

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetAllAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Attachment, bool>>>(), null, null, false, Arg.Any<CancellationToken>())
            .Returns(attachments);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = ReorderAttachmentsHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(callerId)),
            unitOfWork: unitOfWork);

        var result = await handler.Handle(new ReorderAttachmentsCommand(EntityType.Tour, entityId, orderedIds), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        attachments[0].SortOrder.Should().Be(1);
        attachments[1].SortOrder.Should().Be(0);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSkipResolver_WhenCallerIsAdminTier()
    {
        var callerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachments = new List<Attachment>
        {
            ReorderAttachmentsHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid(), 0)
        };
        var orderedIds = attachments.Select(x => x.Id).ToList();

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetAllAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Attachment, bool>>>(), null, null, false, Arg.Any<CancellationToken>())
            .Returns(attachments);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var resolver = Substitute.For<IEntityOwnershipResolver>();
        var handler = ReorderAttachmentsHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.AdminUser(callerId),
            ownershipResolver: resolver,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(new ReorderAttachmentsCommand(EntityType.Tour, entityId, orderedIds), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default);
    }

    [Fact]
    public async Task Handle_ShouldMapDeletedTarget_AndNotMutate()
    {
        var callerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachments = new List<Attachment>
        {
            ReorderAttachmentsHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid(), 0)
        };

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetAllAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Attachment, bool>>>(), null, null, false, Arg.Any<CancellationToken>())
            .Returns(attachments);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var handler = ReorderAttachmentsHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.Deleted(callerId)),
            unitOfWork: unitOfWork);

        var result = await handler.Handle(
            new ReorderAttachmentsCommand(EntityType.Tour, entityId, attachments.Select(x => x.Id).ToList()),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().Contain(x => x.Code == "Attachment.TargetDeleted");
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
