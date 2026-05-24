using Booking.Application.Queries.GetRefundPolicyByTour;
using Booking.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Tests.Unit.Queries;

public sealed class GetRefundPolicyByTourQueryHandlerTests
{
    private static readonly Guid TourId = Guid.Parse("cccccccc-3333-3333-3333-333333333333");

    [Fact]
    public async Task Existing_active_row_returns_DTO_with_IsDefault_false()
    {
        var repo = Substitute.For<IRefundPolicyRepository>();
        var logger = Substitute.For<ILogger<GetRefundPolicyByTourQueryHandler>>();

        var policy = Booking.Domain.Entities.RefundPolicy.Create(TourId, new[]
        {
            new Booking.Domain.ValueObjects.RefundTier(72, 100m),
            new Booking.Domain.ValueObjects.RefundTier(24, 50m),
            new Booking.Domain.ValueObjects.RefundTier(0, 0m),
        });
        repo.GetByTourIdAsync(TourId, Arg.Any<CancellationToken>()).Returns(policy);

        var handler = new GetRefundPolicyByTourQueryHandler(repo, logger);
        var result = await handler.Handle(new GetRefundPolicyByTourQuery(TourId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeFalse();
        result.Value.TourId.Should().Be(TourId);
        result.Value.Tiers.Should().HaveCount(3);
        result.Value.Tiers.Select(t => t.HoursBeforeTour).Should().ContainInOrder(72, 24, 0);
    }

    [Fact]
    public async Task Missing_row_returns_default_DTO_with_IsDefault_true()
    {
        var repo = Substitute.For<IRefundPolicyRepository>();
        var logger = Substitute.For<ILogger<GetRefundPolicyByTourQueryHandler>>();
        repo.GetByTourIdAsync(TourId, Arg.Any<CancellationToken>())
            .Returns((Booking.Domain.Entities.RefundPolicy?)null);

        var handler = new GetRefundPolicyByTourQueryHandler(repo, logger);
        var result = await handler.Handle(new GetRefundPolicyByTourQuery(TourId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeTrue();
        result.Value.TourId.Should().Be(TourId);
        // Default: 100% if ≥ 24h, 0% otherwise.
        result.Value.Tiers.Should().HaveCount(2);
        result.Value.Tiers.Should().Contain(t => t.HoursBeforeTour == 24 && t.RefundPercent == 100m);
        result.Value.Tiers.Should().Contain(t => t.HoursBeforeTour == 0 && t.RefundPercent == 0m);
        result.Value.RowVersion.Should().BeEmpty();
        result.Value.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Inactive_row_falls_back_to_default()
    {
        var repo = Substitute.For<IRefundPolicyRepository>();
        var logger = Substitute.For<ILogger<GetRefundPolicyByTourQueryHandler>>();

        var policy = Booking.Domain.Entities.RefundPolicy.Create(TourId, new[]
        {
            new Booking.Domain.ValueObjects.RefundTier(24, 100m),
        });
        policy.Deactivate();
        repo.GetByTourIdAsync(TourId, Arg.Any<CancellationToken>()).Returns(policy);

        var handler = new GetRefundPolicyByTourQueryHandler(repo, logger);
        var result = await handler.Handle(new GetRefundPolicyByTourQuery(TourId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_TourId_returns_Invalid()
    {
        var repo = Substitute.For<IRefundPolicyRepository>();
        var logger = Substitute.For<ILogger<GetRefundPolicyByTourQueryHandler>>();

        var handler = new GetRefundPolicyByTourQueryHandler(repo, logger);
        var result = await handler.Handle(new GetRefundPolicyByTourQuery(Guid.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
    }
}
