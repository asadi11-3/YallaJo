using System.Linq.Expressions;
using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using ContentPlaces.Application.Queries.AccessibilityFeature.GetAccessibilityFeatures;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using AccessibilityFeatureEntity = ContentPlaces.Domain.Entities.AccessibilityFeature;
using PlaceEntity = ContentPlaces.Domain.Entities.Place;

namespace ContentPlaces.Tests.Unit;

/// <summary>
/// Regression tests for P1-008: GetAccessibilityFeaturesQueryHandler must validate that
/// the parent Place is active (not missing, not soft-deleted) before returning features.
/// The Place EF query filter <c>HasQueryFilter(x =&gt; !x.IsDeleted)</c> means
/// <c>placeRepository.AnyAsync(...)</c> returns <c>false</c> for both missing and
/// soft-deleted Places, so a single guard covers both cases.
/// </summary>
public sealed class GetAccessibilityFeaturesQueryHandlerTests
{
    private static (
        GetAccessibilityFeaturesQueryHandler Handler,
        IAccessibilityFeatureRepository FeatureRepo,
        IPlaceRepository PlaceRepo) BuildSubject()
    {
        var featureRepo = Substitute.For<IAccessibilityFeatureRepository>();
        var placeRepo = Substitute.For<IPlaceRepository>();
        var logger = Substitute.For<ILogger<GetAccessibilityFeaturesQueryHandler>>();

        var handler = new GetAccessibilityFeaturesQueryHandler(featureRepo, placeRepo, logger);
        return (handler, featureRepo, placeRepo);
    }

    [Fact]
    public async Task ReturnsFeaturesWhenPlaceIsActive()
    {
        var (handler, featureRepo, placeRepo) = BuildSubject();
        var placeId = Guid.NewGuid();
        var feature = AccessibilityFeatureEntity.Create(
            entityType: 1,
            entityId: placeId,
            featureType: AccessibilityFeatureType.Wheelchair,
            name: "Wheelchair Ramp",
            description: "Available at main entrance",
            isAvailable: true);

        placeRepo
            .AnyAsync(Arg.Any<Expression<Func<PlaceEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);
        featureRepo
            .SelectAsync(
                Arg.Any<Expression<Func<AccessibilityFeatureEntity, AccessibilityFeatureDto>>>(),
                Arg.Any<Expression<Func<AccessibilityFeatureEntity, bool>>>(),
                Arg.Any<Func<IQueryable<AccessibilityFeatureEntity>, IOrderedQueryable<AccessibilityFeatureEntity>>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<AccessibilityFeatureDto> { AccessibilityFeatureDto.From(feature) });

        var result = await handler.Handle(
            new GetAccessibilityFeaturesQuery(placeId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCount(1);
        result.Value![0].Name.Should().Be("Wheelchair Ramp");
    }

    [Fact]
    public async Task ReturnsNotFoundWhenPlaceIsMissing()
    {
        var (handler, featureRepo, placeRepo) = BuildSubject();
        placeRepo
            .AnyAsync(Arg.Any<Expression<Func<PlaceEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(
            new GetAccessibilityFeaturesQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(x => x.Code == "Place.NotFound");

        // Feature repo must NOT be queried when Place is missing — leak prevention.
        await featureRepo.DidNotReceive().SelectAsync(
            Arg.Any<Expression<Func<AccessibilityFeatureEntity, AccessibilityFeatureDto>>>(),
            Arg.Any<Expression<Func<AccessibilityFeatureEntity, bool>>>(),
            Arg.Any<Func<IQueryable<AccessibilityFeatureEntity>, IOrderedQueryable<AccessibilityFeatureEntity>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsNotFoundWhenPlaceIsSoftDeleted()
    {
        // The Place EF query filter excludes soft-deleted rows, so AnyAsync returns false
        // for soft-deleted Places exactly as it does for missing ones. This test
        // documents the leak-prevention contract from the perspective of the handler.
        var (handler, featureRepo, placeRepo) = BuildSubject();
        placeRepo
            .AnyAsync(Arg.Any<Expression<Func<PlaceEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(
            new GetAccessibilityFeaturesQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(x => x.Code == "Place.NotFound");

        await featureRepo.DidNotReceive().SelectAsync(
            Arg.Any<Expression<Func<AccessibilityFeatureEntity, AccessibilityFeatureDto>>>(),
            Arg.Any<Expression<Func<AccessibilityFeatureEntity, bool>>>(),
            Arg.Any<Func<IQueryable<AccessibilityFeatureEntity>, IOrderedQueryable<AccessibilityFeatureEntity>>>(),
            Arg.Any<CancellationToken>());
    }
}
