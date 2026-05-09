using ContentCore.Application.Authorization;
using ContentCore.Application.Commands.Attachment.SetPrimaryImage;
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

file static class SetPrimaryImageHandlerBuilder
{
    internal static SetPrimaryImageCommandHandler Build(
        IAttachmentRepository attachmentRepository,
        ICurrentUser? currentUser = null,
        IEntityOwnershipResolver? ownershipResolver = null,
        IContentCoreUnitOfWork? unitOfWork = null,
        HybridCache? cache = null)
    {
        return new SetPrimaryImageCommandHandler(
            attachmentRepository,
            unitOfWork ?? OwnershipAuthFixture.NoOpUnitOfWork(),
            currentUser ?? OwnershipAuthFixture.NonAdminUser(Guid.NewGuid()),
            ownershipResolver ?? OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(Guid.NewGuid())),
            cache ?? OwnershipAuthFixture.NoOpCache(),
            Substitute.For<ILogger<SetPrimaryImageCommandHandler>>());
    }

    internal static Attachment BuildAttachment(Guid entityId, Guid uploadedByUserId) =>
        Attachment.Create(EntityType.Tour, entityId, AttachmentType.Image, "https://cdn/app/photo.jpg", uploadedByUserId);
}

public sealed class SetPrimaryImageCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenAttachmentBelongsToDifferentEntity()
    {
        var callerId = Guid.NewGuid();
        var attachment = SetPrimaryImageHandlerBuilder.BuildAttachment(Guid.NewGuid(), callerId);
        var requestEntityId = Guid.NewGuid();
        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), true).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var resolver = Substitute.For<IEntityOwnershipResolver>();
        var handler = SetPrimaryImageHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: resolver,
            unitOfWork: unitOfWork,
            cache: cache);

        var result = await handler.Handle(
            new SetPrimaryImageCommand(EntityType.Tour, requestEntityId, attachment.Id),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().Contain(x => x.Code == "Attachment.WrongEntity");
        await resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default);
        await repository.DidNotReceiveWithAnyArgs().GetEntityImagesAsync(default, default, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await cache.DidNotReceiveWithAnyArgs().RemoveByTagAsync((string)default!, default);
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenUploaderIsNotOwner()
    {
        var callerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachment = SetPrimaryImageHandlerBuilder.BuildAttachment(entityId, callerId);

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), true).Returns(attachment);
        repository.GetEntityImagesAsync(EntityType.Tour, entityId, Arg.Any<CancellationToken>())
            .Returns(new List<EntityImage>());
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var handler = SetPrimaryImageHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(ownerId)),
            unitOfWork: unitOfWork,
            cache: cache);

        var result = await handler.Handle(new SetPrimaryImageCommand(EntityType.Tour, entityId, attachment.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        await repository.DidNotReceiveWithAnyArgs().GetEntityImagesAsync(default, default, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_ShouldAllowOwner_WhenCallerOwnsTargetEntity()
    {
        var callerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachment = SetPrimaryImageHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid());
        var images = new List<EntityImage>();

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), true).Returns(attachment);
        repository.GetEntityImagesAsync(EntityType.Tour, entityId, Arg.Any<CancellationToken>()).Returns(images);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = SetPrimaryImageHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(callerId)),
            unitOfWork: unitOfWork);

        var result = await handler.Handle(new SetPrimaryImageCommand(EntityType.Tour, entityId, attachment.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Received(1).AddEntityImage(Arg.Any<EntityImage>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSkipResolver_WhenCallerIsAdminTier()
    {
        var callerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachment = SetPrimaryImageHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid());

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), true).Returns(attachment);
        repository.GetEntityImagesAsync(EntityType.Tour, entityId, Arg.Any<CancellationToken>())
            .Returns(new List<EntityImage>());
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var resolver = Substitute.For<IEntityOwnershipResolver>();
        var handler = SetPrimaryImageHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.AdminUser(callerId),
            ownershipResolver: resolver,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(new SetPrimaryImageCommand(EntityType.Tour, entityId, attachment.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default);
    }

    [Fact]
    public async Task Handle_ShouldMapUnsupportedEntityType_AndNotMutate()
    {
        var callerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var attachment = SetPrimaryImageHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid());
        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), true).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var handler = SetPrimaryImageHandlerBuilder.Build(
            repository,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.Unsupported()),
            unitOfWork: unitOfWork);

        var result = await handler.Handle(new SetPrimaryImageCommand(EntityType.Tour, entityId, attachment.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().Contain(x => x.Code == "Attachment.UnsupportedEntityType");
        await repository.DidNotReceiveWithAnyArgs().GetEntityImagesAsync(default, default, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
