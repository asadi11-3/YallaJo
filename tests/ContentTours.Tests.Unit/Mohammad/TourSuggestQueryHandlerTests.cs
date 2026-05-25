using ContentTours.Application.Caching;
using ContentTours.Application.Queries.Tour.SuggestTours;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Tests.Unit.Mohammad;

/// <summary>
/// Tests for SuggestTours autocomplete — both source-name prefix matching AND
/// TourTranslation.Name prefix matching for the active Accept-Language. Pins
/// duplicate-prevention (one row per Tour even when both source and translation match)
/// and the language-partitioned cache key.
/// </summary>
public sealed class TourSuggestQueryHandlerTests
{
    private static readonly Guid ArabicLanguageId = Guid.NewGuid();
    private static readonly Guid EnglishLanguageId = Guid.NewGuid();

    private static IActiveLanguageProvider StubLanguageProvider()
    {
        var provider = Substitute.For<IActiveLanguageProvider>();
        provider.GetActiveLanguagesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ActiveLanguage>
            {
                new(EnglishLanguageId, "en"),
                new(ArabicLanguageId,  "ar"),
            });
        return provider;
    }

    private static Tour SeedTour(string name, bool isDeleted = false, TourStatus status = TourStatus.Approved)
    {
        var tour = Tour.Create(
            name:                    name,
            slug:                    $"{name.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid():N}",
            difficulty:              Difficulty.Easy,
            durationMinutes:         480, maxGroupSize: 20,
            basePriceAmount:         100m, currency: "JOD",
            location:                new Location(30.32m, 35.45m),
            createdByUserId:         Guid.NewGuid(),
            description:             new string('a', 120),
            shortDescription:        null, minAge: null,
            meetingPoint:            new Location(30.32m, 35.45m),
            placeId:                 Guid.NewGuid());

        if (status != TourStatus.Draft)
        {
            tour.Submit();
            if (status == TourStatus.Approved) tour.Approve(Guid.NewGuid());
        }
        if (isDeleted) tour.SoftDelete();
        return tour;
    }

    [Fact]
    public async Task Suggest_PrefixMatchesTourName_ReturnsApprovedNonDeletedOnly()
    {
        await using var db = TestDbContextFactory.NewInMemory();
        db.Tours.AddRange(
            SeedTour("Petra Day Tour"),
            SeedTour("Petra Sunset Tour"),
            SeedTour("Petra Draft Tour", status: TourStatus.Draft),       // excluded
            SeedTour("Petra Deleted Tour", isDeleted: true),              // excluded
            SeedTour("Wadi Rum Adventure"));                              // doesn't match prefix
        await db.SaveChangesAsync();

        var handler = new SuggestToursQueryHandler(
            new TourRepository(db),
            StubLanguageProvider(),
            Substitute.For<ILogger<SuggestToursQueryHandler>>());

        var result = await handler.Handle(
            new SuggestToursQuery("Petra", AcceptLanguage: "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(s => s.Name).Should().BeEquivalentTo(new[]
        {
            "Petra Day Tour", "Petra Sunset Tour"
        });
    }

    [Fact]
    public async Task Suggest_RespectsTopTenLimit()
    {
        await using var db = TestDbContextFactory.NewInMemory();
        for (int i = 0; i < 15; i++) db.Tours.Add(SeedTour($"Petra-{i:00}"));
        await db.SaveChangesAsync();

        var handler = new SuggestToursQueryHandler(
            new TourRepository(db),
            StubLanguageProvider(),
            Substitute.For<ILogger<SuggestToursQueryHandler>>());

        var result = await handler.Handle(
            new SuggestToursQuery("Petra", AcceptLanguage: "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(10);
    }

    [Fact]
    public async Task Suggest_MatchesTranslationNameForActiveLanguage()
    {
        // Source name "Petra Day Tour" does not start with "بترا" — only the Arabic
        // translation does. Suggest in ar must surface the tour and use the translation
        // as the displayed name.
        await using var db = TestDbContextFactory.NewInMemory();

        var tour = SeedTour("Petra Day Tour");
        db.Tours.Add(tour);
        db.TourTranslations.Add(TourTranslation.Create(
            tourId:           tour.Id,
            languageId:       ArabicLanguageId,
            name:             "بترا — جولة يومية",
            description:      null,
            shortDescription: null));
        await db.SaveChangesAsync();

        var handler = new SuggestToursQueryHandler(
            new TourRepository(db),
            StubLanguageProvider(),
            Substitute.For<ILogger<SuggestToursQueryHandler>>());

        var result = await handler.Handle(
            new SuggestToursQuery("بترا", AcceptLanguage: "ar"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle();
        result.Value![0].Id.Should().Be(tour.Id);
        result.Value![0].Name.Should().StartWith("بترا");
    }

    [Fact]
    public async Task Suggest_DoesNotDuplicateTourWhenSourceAndTranslationBothMatch()
    {
        // Source name "Adventure" starts with "Adv" AND translation also starts with "Adv".
        // Only one row should come back, not two.
        await using var db = TestDbContextFactory.NewInMemory();

        var tour = SeedTour("Adventure Tour");
        db.Tours.Add(tour);
        db.TourTranslations.Add(TourTranslation.Create(
            tourId:           tour.Id,
            languageId:       EnglishLanguageId,
            name:             "Adventure — extended",
            description:      null,
            shortDescription: null));
        await db.SaveChangesAsync();

        var handler = new SuggestToursQueryHandler(
            new TourRepository(db),
            StubLanguageProvider(),
            Substitute.For<ILogger<SuggestToursQueryHandler>>());

        var result = await handler.Handle(
            new SuggestToursQuery("Adv", AcceptLanguage: "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Count(s => s.Id == tour.Id).Should().Be(1);
    }

    [Fact]
    public void Suggest_CacheKey_IsLanguagePartitioned()
    {
        // Direct cache-key contract test — pins the P1 #5 normalisation/partition rule.
        var ar = TourSearchCacheKeys.Suggest("Petra", "ar-JO,en;q=0.5");
        var en = TourSearchCacheKeys.Suggest("Petra", "en-US");
        var def = TourSearchCacheKeys.Suggest("Petra", null);
        var blank = TourSearchCacheKeys.Suggest("Petra", "   ");

        ar.Should().NotBe(en);
        ar.Should().Contain(":lang:ar-jo");      // first-token, lowercased
        en.Should().Contain(":lang:en-us");
        def.Should().Be(blank);
        def.Should().Contain(":lang:default");

        // q is normalised — case-insensitive collision is desired
        TourSearchCacheKeys.Suggest("PETRA", "en")
            .Should().Be(TourSearchCacheKeys.Suggest("petra", "en"));
    }
}
