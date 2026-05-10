using ContentPlaces.Application.Caching;
using FluentAssertions;

namespace ContentPlaces.Tests.Unit;

public sealed class ContentPlacesCacheKeysTests
{
    [Fact]
    public void ServiceItemList_VariesByElevatedFlag()
    {
        var businessId = Guid.NewGuid();
        var publicKey = ContentPlacesCacheKeys.ServiceItemList(businessId, isElevated: false);
        var elevatedKey = ContentPlacesCacheKeys.ServiceItemList(businessId, isElevated: true);

        publicKey.Should().NotBe(elevatedKey);
    }

    [Fact]
    public void ServiceItemList_VariesByBusinessId()
    {
        var keyA = ContentPlacesCacheKeys.ServiceItemList(Guid.NewGuid(), isElevated: true);
        var keyB = ContentPlacesCacheKeys.ServiceItemList(Guid.NewGuid(), isElevated: true);

        keyA.Should().NotBe(keyB);
    }

    [Fact]
    public void ServiceItemList_StableForSameInputs()
    {
        var businessId = Guid.NewGuid();
        var keyA = ContentPlacesCacheKeys.ServiceItemList(businessId, isElevated: false);
        var keyB = ContentPlacesCacheKeys.ServiceItemList(businessId, isElevated: false);

        keyA.Should().Be(keyB);
    }

    // ── CONTENTPLACES-FOLLOWUP-CACHE-CENTRALIZATION-001 ──────────────────────
    //
    // The centralisation refactor must preserve every previous raw-string tag
    // value byte-for-byte so that cached entries created before the refactor
    // remain reachable for invalidation.  These tests pin each helper's output.

    [Fact]
    public void TagPlaces_ReturnsExpectedValue()
    {
        ContentPlacesCacheKeys.TagPlaces.Should().Be("places",
            "the broad places tag value must remain unchanged from the prior raw-string usage");
    }

    [Fact]
    public void TagBusinesses_ReturnsExpectedValue()
    {
        ContentPlacesCacheKeys.TagBusinesses.Should().Be("businesses",
            "the broad businesses tag value must remain unchanged from the prior raw-string usage");
    }

    [Fact]
    public void TagForPlace_ReturnsExpectedValue()
    {
        var placeId = Guid.NewGuid();
        ContentPlacesCacheKeys.TagForPlace(placeId).Should().Be($"place:{placeId}",
            "per-place tag must match the prior raw-string format $\"place:{id}\"");
    }

    [Fact]
    public void TagForBusiness_ReturnsExpectedValue()
    {
        var businessId = Guid.NewGuid();
        ContentPlacesCacheKeys.TagForBusiness(businessId).Should().Be($"biz:{businessId}",
            "per-business tag must match the prior raw-string format $\"biz:{id}\"");
    }

    [Fact]
    public void TagForBusinessHours_ReturnsExpectedValue()
    {
        var businessId = Guid.NewGuid();
        ContentPlacesCacheKeys.TagForBusinessHours(businessId).Should().Be($"biz:{businessId}:hours");
    }

    [Fact]
    public void TagForBusinessServices_ReturnsExpectedValue()
    {
        var businessId = Guid.NewGuid();
        ContentPlacesCacheKeys.TagForBusinessServices(businessId).Should().Be($"biz:{businessId}:services");
    }

    [Fact]
    public void TagForPlaceBusinesses_ReturnsExpectedValue()
    {
        var placeId = Guid.NewGuid();
        ContentPlacesCacheKeys.TagForPlaceBusinesses(placeId).Should().Be($"place:{placeId}:businesses");
    }

    [Fact]
    public void TagForServiceItem_ReturnsExpectedValue()
    {
        var serviceItemId = Guid.NewGuid();
        ContentPlacesCacheKeys.TagForServiceItem(serviceItemId).Should().Be($"service:{serviceItemId}");
    }

    [Fact]
    public void PlaceTag_ReturnsSameValueAsTagForPlace()
    {
        var placeId = Guid.NewGuid();
        ContentPlacesCacheKeys.PlaceTag(placeId)
            .Should().Be(ContentPlacesCacheKeys.TagForPlace(placeId),
                "PlaceTag is a backwards-compatible alias and must produce the canonical helper's value");
    }

    [Fact]
    public void BusinessTag_ReturnsSameValueAsTagForBusiness()
    {
        var businessId = Guid.NewGuid();
        ContentPlacesCacheKeys.BusinessTag(businessId)
            .Should().Be(ContentPlacesCacheKeys.TagForBusiness(businessId),
                "BusinessTag is a backwards-compatible alias and must produce the canonical helper's value");
    }

    // ── CONTENTPLACES-FOLLOWUP-CACHE-SLUG-001 ────────────────────────────────

    [Fact]
    public void TagForPlaceSlug_ReturnsExpectedValue()
    {
        ContentPlacesCacheKeys.TagForPlaceSlug("petra-day-tour")
            .Should().Be("place:slug:petra-day-tour",
                "per-slug tag must use the canonical 'place:slug:{slug}' format");
    }

    [Fact]
    public void TagForPlaceSlug_NormalizesCasingAndWhitespace()
    {
        // Slug normalization must match PlaceBySlug() exactly so the per-slug
        // cache key and per-slug invalidation tag align even when callers pass
        // mixed-case or whitespace-padded slugs.
        ContentPlacesCacheKeys.TagForPlaceSlug("  Petra-Day-Tour  ")
            .Should().Be("place:slug:petra-day-tour");
    }

    [Fact]
    public void TagForPlaceSlug_HandlesNullSlugDefensively()
    {
        // A null slug must not throw — it normalises to an empty segment so the
        // helper is safe to call from edge paths.
        ContentPlacesCacheKeys.TagForPlaceSlug(null!)
            .Should().Be("place:slug:");
    }
}
