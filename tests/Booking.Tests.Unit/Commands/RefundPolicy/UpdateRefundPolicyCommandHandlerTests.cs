using Booking.Application.Commands.RefundPolicy.UpdateRefundPolicy;
using Booking.Application.Interfaces;
using Booking.Application.Queries.GetRefundPolicyByTour;
using Booking.Domain.Repositories;
using ContentTours.Contracts.Authorization;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Tests.Unit.Commands.RefundPolicy;

public sealed class UpdateRefundPolicyCommandHandlerTests
{
    private static readonly Guid TourId = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222");

    private static IReadOnlyList<RefundTierDto> NewTiers => new[]
    {
        new RefundTierDto(48, 80m),
        new RefundTierDto(0, 0m),
    };

    private static (UpdateRefundPolicyCommandHandler handler,
        IRefundPolicyRepository repo,
        ITourOwnershipService ownership,
        IBookingUnitOfWork uow,
        HybridCache cache,
        ICurrentUser user) BuildSut()
    {
        var repo = Substitute.For<IRefundPolicyRepository>();
        var ownership = Substitute.For<ITourOwnershipService>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var user = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<UpdateRefundPolicyCommandHandler>>();
        return (new UpdateRefundPolicyCommandHandler(repo, ownership, uow, cache, user, logger),
            repo, ownership, uow, cache, user);
    }

    private static Booking.Domain.Entities.RefundPolicy SeedExisting()
        => Booking.Domain.Entities.RefundPolicy.Create(TourId, new[]
        {
            new Booking.Domain.ValueObjects.RefundTier(24, 100m),
            new Booking.Domain.ValueObjects.RefundTier(0, 0m),
        });

    [Fact]
    public async Task Missing_policy_returns_NotFound()
    {
        var (handler, repo, _, _, _, _) = BuildSut();
        var policyId = Guid.NewGuid();
        repo.GetByIdTrackedAsync(policyId, Arg.Any<CancellationToken>())
            .Returns((Booking.Domain.Entities.RefundPolicy?)null);

        var result = await handler.Handle(
            new UpdateRefundPolicyCommand(policyId, NewTiers, []),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task Ownership_check_uses_stored_TourId_not_body_data()
    {
        var (handler, repo, ownership, _, _, user) = BuildSut();
        var existing = SeedExisting();
        repo.GetByIdTrackedAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);

        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(Guid.NewGuid());
        user.HasPermission(Arg.Any<string>()).Returns(false);
        ownership.GetTourOwnershipAsync(TourId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(true, true, false, Guid.NewGuid()));

        var result = await handler.Handle(
            new UpdateRefundPolicyCommand(existing.Id, NewTiers, []),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);

        // Critical: ownership probe was called with the STORED TourId, not anything
        // a client could spoof through the body or the route.
        await ownership.Received(1).GetTourOwnershipAsync(TourId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Owner_can_update_and_cache_is_invalidated()
    {
        var (handler, repo, ownership, uow, cache, user) = BuildSut();
        var existing = SeedExisting();
        var ownerId = Guid.NewGuid();

        repo.GetByIdTrackedAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(ownerId);
        ownership.GetTourOwnershipAsync(TourId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(true, true, false, ownerId));

        var result = await handler.Handle(
            new UpdateRefundPolicyCommand(existing.Id, NewTiers, []),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Ok);
        existing.Tiers.Select(t => t.HoursBeforeTour).Should().ContainInOrder(48, 0);

        repo.Received(1).Update(existing);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            $"refund-policy:tour:{TourId:D}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Domain_validation_failure_returns_Invalid()
    {
        var (handler, repo, ownership, uow, _, user) = BuildSut();
        var existing = SeedExisting();
        var ownerId = Guid.NewGuid();

        repo.GetByIdTrackedAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(ownerId);
        ownership.GetTourOwnershipAsync(TourId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(true, true, false, ownerId));

        var badTiers = new[]
        {
            new RefundTierDto(24, 100m),
            new RefundTierDto(24, 50m),
        };

        var result = await handler.Handle(
            new UpdateRefundPolicyCommand(existing.Id, badTiers, []),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "RefundPolicy.InvalidTiers");
        await uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
