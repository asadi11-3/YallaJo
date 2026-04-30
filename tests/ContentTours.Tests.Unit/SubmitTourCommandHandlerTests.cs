using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using ContentPlaces.Contracts.Places;
using ContentTours.Application.Commands.Tour.SubmitTour;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit;

public sealed class SubmitTourCommandHandlerTests
{
    private static (
        SubmitTourCommandHandler Handler,
        ITourRepository TourRepo,
        IAttachmentRepository AttachmentRepo,
        IPlaceExistenceService PlaceExists,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        var attachmentRepo = Substitute.For<IAttachmentRepository>();
        var place = Substitute.For<IPlaceExistenceService>();
        var uow = Substitute.For<IContentToursEventUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<SubmitTourCommandHandler>>();

        var handler = new SubmitTourCommandHandler(
            tourRepo, attachmentRepo, place, uow, cache, currentUser, logger);
        return (handler, tourRepo, attachmentRepo, place, currentUser);
    }

    private static Tour DraftWithoutMeetingPoint(Guid owner) =>
        TestTourFactory.CreateDraft(createdByUserId: owner);

    [Fact]
    public async Task MissingPreSubmitRequirementsReturnAggregatedUmbrella422()
    {
        var (handler, tourRepo, attachmentRepo, _, currentUser) = BuildSubject();
        var owner = Guid.NewGuid();

        // A pristine Draft tour fails every pre-submit check (no images, no pricing,
        // no schedule, description too short, no meeting point, no Adult tier).
        var tour = DraftWithoutMeetingPoint(owner);

        currentUser.UserId.Returns(owner);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        attachmentRepo.GetEntityImagesAsync(EntityType.Tour, tour.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EntityImage>());
        tourRepo.HasActivePricingAsync(tour.Id, Arg.Any<CancellationToken>()).Returns(false);
        tourRepo.HasActiveScheduleAsync(tour.Id, Arg.Any<CancellationToken>()).Returns(false);
        tourRepo.HasActiveAdultPricingAsync(tour.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await handler.Handle(new SubmitTourCommand(tour.Id, tour.RowVersion), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
        result.Errors.Should().Contain(e => e.Code == "Tour.SubmitValidationFailed");

        // Aggregated child errors — every check that should have fired must be present.
        var codes = result.Errors.Select(e => e.Code).ToList();
        codes.Should().Contain("Tour.NoImages");
        codes.Should().Contain("Tour.NoPricing");
        codes.Should().Contain("Tour.NoSchedule");
        codes.Should().Contain("Tour.DescriptionTooShort");
        codes.Should().Contain("Tour.MissingMeetingPoint");
        codes.Should().Contain("Tour.NoAdultPricingTier");
    }

    [Fact]
    public async Task ValidDraftTransitionsToPending()
    {
        var (handler, tourRepo, attachmentRepo, _, currentUser) = BuildSubject();
        var owner = Guid.NewGuid();

        // Build a Draft tour that satisfies every pre-submit gate the aggregate does
        // not enforce on its own (description >= 100, meeting point present).
        var tour = TestTourFactory.CreateDraft(
            createdByUserId: owner,
            meetingPoint:    new YallaJo.SharedKernel.Domain.ValueObjects.Location(30.32m, 35.45m),
            description:     new string('a', 120));

        currentUser.UserId.Returns(owner);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        // Repository checks all pass.
        attachmentRepo.GetEntityImagesAsync(EntityType.Tour, tour.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { CreateEntityImage(tour.Id) });
        tourRepo.HasActivePricingAsync(tour.Id, Arg.Any<CancellationToken>()).Returns(true);
        tourRepo.HasActiveScheduleAsync(tour.Id, Arg.Any<CancellationToken>()).Returns(true);
        tourRepo.HasActiveAdultPricingAsync(tour.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await handler.Handle(new SubmitTourCommand(tour.Id, tour.RowVersion), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tour.Status.Should().Be(TourStatus.Pending);
    }

    private static EntityImage CreateEntityImage(Guid tourId) =>
        EntityImage.Create(
            entityType:   EntityType.Tour,
            entityId:     tourId,
            attachmentId: Guid.NewGuid(),
            imageSize:    ImageSize.Medium,
            sortOrder:    0,
            isPrimary:    true);
}
