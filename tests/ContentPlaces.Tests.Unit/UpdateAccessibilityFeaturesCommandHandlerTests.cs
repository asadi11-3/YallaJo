using System.Linq.Expressions;
using ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using FeatureEntity = ContentPlaces.Domain.Entities.AccessibilityFeature;

namespace ContentPlaces.Tests.Unit;

public sealed class UpdateAccessibilityFeaturesCommandHandlerTests
{
    private static (
        UpdateAccessibilityFeaturesCommandHandler Handler,
        IAccessibilityFeatureRepository FeatureRepo,
        IPlaceRepository PlaceRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var featureRepo = Substitute.For<IAccessibilityFeatureRepository>();
        var placeRepo = Substitute.For<IPlaceRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<UpdateAccessibilityFeaturesCommandHandler>>();

        var handler = new UpdateAccessibilityFeaturesCommandHandler(
            featureRepo, placeRepo, uow, currentUser, cache, logger);

        return (handler, featureRepo, placeRepo, uow, currentUser);
    }

    [Fact]
    public async Task ReturnsUnauthorizedWhenNotAuthenticated()
    {
        var (handler, _, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(false);

        var result = await handler.Handle(
            new UpdateAccessibilityFeaturesCommand(Guid.NewGuid(), Array.Empty<AccessibilityFeatureItemRequest>()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Unauthorized");
    }

    [Fact]
    public async Task ReturnsNotFoundWhenPlaceMissing()
    {
        var (handler, _, placeRepo, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        placeRepo
            .AnyAsync(
                Arg.Any<Expression<Func<ContentPlaces.Domain.Entities.Place, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(
            new UpdateAccessibilityFeaturesCommand(Guid.NewGuid(), Array.Empty<AccessibilityFeatureItemRequest>()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "NotFound.Place.NotFound");
    }

    [Fact]
    public async Task SucceedsForAuthenticatedCallerWithoutRoleCheck()
    {
        // Endpoint enforces MustHavePermissionAttribute(AccessibilityFeature, Update);
        // handler must no longer re-check IsInRole("Admin").
        var (handler, featureRepo, placeRepo, uow, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        // Roles intentionally NOT set — handler should not consult them.
        placeRepo
            .AnyAsync(
                Arg.Any<Expression<Func<ContentPlaces.Domain.Entities.Place, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        featureRepo
            .GetAllAsync(
                Arg.Any<Expression<Func<FeatureEntity, bool>>>(),
                include: Arg.Any<Func<IQueryable<FeatureEntity>, IQueryable<FeatureEntity>>>(),
                orderBy: Arg.Any<Func<IQueryable<FeatureEntity>, IOrderedQueryable<FeatureEntity>>>(),
                asNoTracking: Arg.Any<bool>(),
                ct: Arg.Any<CancellationToken>())
            .Returns(new List<FeatureEntity>());

        var result = await handler.Handle(
            new UpdateAccessibilityFeaturesCommand(Guid.NewGuid(), Array.Empty<AccessibilityFeatureItemRequest>()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        currentUser.DidNotReceive().IsInRole(Arg.Any<string>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
