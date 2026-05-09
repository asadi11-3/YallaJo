using ContentTours.Application.Commands.TourPricingTier.DeleteTourPricingTier;
using ContentTours.Application.Commands.TourSchedule.DeleteTourSchedule;
using ContentTours.Application.Commands.Tour.ToggleTourFeatured;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit.Mohammad;

/// <summary>
/// Regression tests for ERR-004 / PDF Critical Rule 7.
///
/// Background: prior to P0 #2, Mohammad's handlers used the <c>Result.NotFound(string)</c>,
/// <c>Result.Forbidden(string)</c>, and <c>Result.Conflict(string)</c> shortcut overloads,
/// which populate <c>Messages</c> rather than <c>Errors</c>. The <c>ToApiResult()</c>
/// extension only includes <c>Errors[0].Code</c> in the Problem title; the string-shortcut
/// path therefore caused consumers to see e.g. a 404 with empty body instead of the expected
/// <c>Tour.NotFound</c> error code.
///
/// These tests pin the contract that the error code now lives in
/// <see cref="Result.Errors"/> (NOT only in <see cref="Result.Messages"/>) for every
/// fixed handler path. Together with <c>ToApiResult()</c>'s use of <c>Errors[0].Code</c>,
/// this guarantees the round-trip code is preserved end-to-end.
/// </summary>
public sealed class Err004ResultPatternRegressionTests
{
    [Fact]
    public async Task DeleteTourSchedule_TourNotFound_ErrorIsInErrors_NotMessages()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        tourRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((ContentTours.Domain.Entities.Tour?)null);

        var handler = new DeleteTourScheduleCommandHandler(
            tourRepo,
            Substitute.For<ITourScheduleRepository>(),
            Substitute.For<IScheduleBookingCountService>(),
            Substitute.For<IContentToursUnitOfWork>(),
            Substitute.For<IContentToursOutboxWriter>(),
            Substitute.For<HybridCache>(),
            Substitute.For<ICurrentUser>(),
            Substitute.For<ILogger<DeleteTourScheduleCommandHandler>>());

        var result = await handler.Handle(
            new DeleteTourScheduleCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotFound");
        // Critical: the code must be in Errors (the shape ToApiResult preserves), not
        // only in Messages (the shape that gets stripped).
        result.Messages.Should().NotContain(m => m == "Tour.NotFound");
    }

    [Fact]
    public async Task DeleteTourPricingTier_TourNotFound_ErrorIsInErrors_NotMessages()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        tourRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((ContentTours.Domain.Entities.Tour?)null);

        var handler = new DeleteTourPricingTierCommandHandler(
            tourRepo,
            Substitute.For<ITourPricingTierRepository>(),
            Substitute.For<IContentToursUnitOfWork>(),
            Substitute.For<IContentToursOutboxWriter>(),
            Substitute.For<HybridCache>(),
            Substitute.For<ICurrentUser>(),
            Substitute.For<ILogger<DeleteTourPricingTierCommandHandler>>());

        var result = await handler.Handle(
            new DeleteTourPricingTierCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotFound");
        result.Messages.Should().NotContain(m => m == "Tour.NotFound");
    }

    [Fact]
    public async Task ToggleTourFeatured_TourNotFound_ErrorIsInErrors_NotMessages()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        tourRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((ContentTours.Domain.Entities.Tour?)null);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new ToggleTourFeaturedCommandHandler(
            tourRepo,
            Substitute.For<IContentToursUnitOfWork>(),
            Substitute.For<HybridCache>(),
            currentUser,
            Substitute.For<ILogger<ToggleTourFeaturedCommandHandler>>());

        var result = await handler.Handle(
            new ToggleTourFeaturedCommand(Guid.NewGuid(), IsFeatured: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotFound");
        result.Messages.Should().NotContain(m => m == "Tour.NotFound");
    }
}
