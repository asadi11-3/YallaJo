using ContentCore.Application.Authorization;
using ContentCore.Application.Commands.Attachment.SetPrimaryImage;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Tests.Unit;

file static class SetPrimaryImageHandlerBuilder
{
    internal static SetPrimaryImageCommandHandler Build(
        IAttachmentRepository attachmentRepository,
        IOwnershipGuard? ownershipGuard = null,
        IContentCoreUnitOfWork? unitOfWork = null,
        HybridCache? cache = null)
    {
        return new SetPrimaryImageCommandHandler(
            attachmentRepository,
            unitOfWork ?? OwnershipAuthFixture.NoOpUnitOfWork(),
            ownershipGuard ?? OwnershipAuthFixture.GuardAllowing(),
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
        var attachment = SetPrimaryImageHandlerBuilder.BuildAttachment(Guid.NewGuid(), Guid.NewGuid());
        var requestEntityId = Guid.NewGuid();
        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), true).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var guard = OwnershipAuthFixture.GuardAllowing();
        var handler = SetPrimaryImageHandlerBuilder.Build(
            repository,
            ownershipGuard: guard,
            unitOfWork: unitOfWork,
            cache: cache);

        var result = await handler.Handle(
            new SetPrimaryImageCommand(EntityType.Tour, requestEntityId, attachment.Id),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().Contain(x => x.Code == "Attachment.WrongEntity");
        await guard.DidNotReceiveWithAnyArgs().AuthorizeAsync(default, default, default!, default, default);
        await repository.DidNotReceiveWithAnyArgs().GetEntityImagesAsync(default, default, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await cache.DidNotReceiveWithAnyArgs().RemoveByTagAsync((string)default!, default);
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenGuardDenies()
    {
        var entityId = Guid.NewGuid();
        var attachment = SetPrimaryImageHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid());

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), true).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var handler = SetPrimaryImageHandlerBuilder.Build(
            repository,
            ownershipGuard: OwnershipAuthFixture.GuardForbidden(),
            unitOfWork: unitOfWork,
            cache: cache);

        var result = await handler.Handle(new SetPrimaryImageCommand(EntityType.Tour, entityId, attachment.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        await repository.DidNotReceiveWithAnyArgs().GetEntityImagesAsync(default, default, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_ShouldAllowOwner_WhenGuardAllows()
    {
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
            ownershipGuard: OwnershipAuthFixture.GuardAllowing(),
            unitOfWork: unitOfWork);

        var result = await handler.Handle(new SetPrimaryImageCommand(EntityType.Tour, entityId, attachment.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Received(1).AddEntityImage(Arg.Any<EntityImage>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenGuardAllowsAsAdmin()
    {
        var entityId = Guid.NewGuid();
        var attachment = SetPrimaryImageHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid());

        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), true).Returns(attachment);
        repository.GetEntityImagesAsync(EntityType.Tour, entityId, Arg.Any<CancellationToken>())
            .Returns(new List<EntityImage>());
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = SetPrimaryImageHandlerBuilder.Build(
            repository,
            ownershipGuard: OwnershipAuthFixture.GuardAllowing(isAdminTier: true),
            unitOfWork: unitOfWork);

        var result = await handler.Handle(new SetPrimaryImageCommand(EntityType.Tour, entityId, attachment.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldMapUnsupportedEntityType_AndNotMutate()
    {
        var entityId = Guid.NewGuid();
        var attachment = SetPrimaryImageHandlerBuilder.BuildAttachment(entityId, Guid.NewGuid());
        var repository = Substitute.For<IAttachmentRepository>();
        repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>(), true).Returns(attachment);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var handler = SetPrimaryImageHandlerBuilder.Build(
            repository,
            ownershipGuard: OwnershipAuthFixture.GuardInvalid("Attachment.UnsupportedEntityType", "Unsupported entity type."),
            unitOfWork: unitOfWork);

        var result = await handler.Handle(new SetPrimaryImageCommand(EntityType.Tour, entityId, attachment.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().Contain(x => x.Code == "Attachment.UnsupportedEntityType");
        await repository.DidNotReceiveWithAnyArgs().GetEntityImagesAsync(default, default, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
