using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.Place.GetPlaceBySlug;
using FluentAssertions;

namespace ContentPlaces.Tests.Unit;

/// <summary>
/// CONTENTPLACES-FOLLOWUP-CACHE-SLUG-001 regression test.
///
/// <para>
/// <see cref="GetPlaceBySlugQuery.Tags"/> must include both the broad
/// <c>TagPlaces</c> tag (preserving the prior invalidation surface) and the
/// new per-slug tag (so a slug rename can evict the stale slug entry without
/// a broad sweep — mirroring the ContentTours P1-005 standard).
/// </para>
/// </summary>
public sealed class GetPlaceBySlugQueryTagsTests
{
    [Fact]
    public void GetPlaceBySlugQuery_HasPerSlugTag()
    {
        const string slug = "petra-day-tour";
        var query = new GetPlaceBySlugQuery(slug);

        var tags = query.Tags;

        tags.Should().Contain(ContentPlacesCacheKeys.TagPlaces,
            "the broad places tag must remain so list/detail invalidation continues to evict slug entries");

        tags.Should().Contain(ContentPlacesCacheKeys.TagForPlaceSlug(slug),
            "the per-slug tag must be present so a slug rename can target this entry " +
            "without a broad sweep (CONTENTPLACES-FOLLOWUP-CACHE-SLUG-001)");
    }

    [Fact]
    public void GetPlaceBySlugQuery_PerSlugTag_NormalizesSameAsCacheKey()
    {
        // Casing/whitespace normalization in the per-slug tag must match the
        // PlaceBySlug cache-key normalization, so an invalidation by tag from
        // UpdatePlace (which captures the entity's stored slug) reliably matches
        // the cache key produced when the public read used a differently-cased
        // slug in the URL.
        var lowercase = new GetPlaceBySlugQuery("petra-day-tour");
        var mixedCase = new GetPlaceBySlugQuery("  Petra-Day-Tour  ");

        lowercase.Tags.Should().Contain(ContentPlacesCacheKeys.TagForPlaceSlug("petra-day-tour"));
        mixedCase.Tags.Should().Contain(ContentPlacesCacheKeys.TagForPlaceSlug("petra-day-tour"),
            "a mixed-case URL slug must produce the same per-slug invalidation tag as the canonical lowercased form");
    }
}
