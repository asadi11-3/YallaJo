using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourPricingTier.ListTourPricingTiers;
using FluentAssertions;

namespace ContentTours.Tests.Unit.Mohammad;

/// <summary>
/// Pins the P1 #2 visibility partition. Anonymous, admin, owner, and non-owner
/// authenticated callers must NOT collide on the same cache slot — even with the
/// same tour-id, language, and activeOnly value — because the handler returns
/// different content (active-only vs all tiers) for elevated callers.
/// </summary>
public sealed class TourPricingTierCacheKeyTests
{
    [Fact]
    public void Anonymous_AdminOwner_NonOwner_AllUseDifferentCacheSlots()
    {
        var tourId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var nonOwner = Guid.NewGuid();
        var lang = "en";

        var keys = new[]
        {
            // anonymous
            TourPricingTierCacheKeys.List(tourId, activeOnly: false, lang, callerUserId: null,    isAdmin: false),
            // admin (any UserId — admins all share the same slot)
            TourPricingTierCacheKeys.List(tourId, activeOnly: false, lang, callerUserId: owner,   isAdmin: true),
            // owner authenticated, not admin
            TourPricingTierCacheKeys.List(tourId, activeOnly: false, lang, callerUserId: owner,   isAdmin: false),
            // non-owner authenticated, not admin
            TourPricingTierCacheKeys.List(tourId, activeOnly: false, lang, callerUserId: nonOwner, isAdmin: false),
        };

        keys.Distinct().Should().HaveCount(4,
            "anonymous, admin, owner, and non-owner authenticated callers must each occupy " +
            "their own visibility-partitioned cache slot to prevent cross-tier poisoning");
    }

    [Fact]
    public void AnonymousAndAdmin_NeverCollide_AcrossLanguagesAndActiveOnly()
    {
        var tourId = Guid.NewGuid();

        foreach (var lang in new[] { "en", "ar-jo", "default" })
        {
            foreach (var activeOnly in new[] { true, false })
            {
                var anon  = TourPricingTierCacheKeys.List(tourId, activeOnly, lang, callerUserId: null,           isAdmin: false);
                var admin = TourPricingTierCacheKeys.List(tourId, activeOnly, lang, callerUserId: Guid.NewGuid(), isAdmin: true);
                anon.Should().NotBe(admin,
                    $"anon and admin must never collide; offending lang={lang} activeOnly={activeOnly}");
            }
        }
    }

    [Fact]
    public void AdminCallers_ShareTheSameSlotRegardlessOfUserId()
    {
        // Optimisation: every admin sees the same data, so they should share a cache slot.
        var tourId = Guid.NewGuid();
        var lang = "en";

        var admin1 = TourPricingTierCacheKeys.List(tourId, activeOnly: false, lang, callerUserId: Guid.NewGuid(), isAdmin: true);
        var admin2 = TourPricingTierCacheKeys.List(tourId, activeOnly: false, lang, callerUserId: Guid.NewGuid(), isAdmin: true);

        admin1.Should().Be(admin2);
        admin1.Should().Contain(":viz=adm:");
    }

    [Fact]
    public void TwoAuthenticatedNonAdminUsers_HaveDistinctSlots()
    {
        // Owner and non-owner authenticated callers each get their own bucket — small
        // cache-fill cost for a hard correctness guarantee against owner-data leak.
        var tourId = Guid.NewGuid();
        var lang = "en";
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();

        var k1 = TourPricingTierCacheKeys.List(tourId, activeOnly: false, lang, callerUserId: u1, isAdmin: false);
        var k2 = TourPricingTierCacheKeys.List(tourId, activeOnly: false, lang, callerUserId: u2, isAdmin: false);

        k1.Should().NotBe(k2);
        k1.Should().Contain($":viz=usr-{u1}:");
        k2.Should().Contain($":viz=usr-{u2}:");
    }

    [Fact]
    public void Anonymous_TokenIsAnon()
    {
        var key = TourPricingTierCacheKeys.List(
            Guid.NewGuid(), activeOnly: true, "en", callerUserId: null, isAdmin: false);
        key.Should().Contain(":viz=anon:");
    }

    [Fact]
    public void QueryRecord_BuildsExpectedCacheKey()
    {
        // Smoke-test that the query record's CacheKey reflects the new visibility shape.
        var tourId = Guid.NewGuid();
        var owner = Guid.NewGuid();

        var anonQuery = new ListTourPricingTiersQuery(
            TourId: tourId, ActiveOnly: false, LanguageCode: "en",
            CallerUserId: null, IsAdmin: false);
        anonQuery.CacheKey.Should().Contain(":viz=anon:");

        var adminQuery = new ListTourPricingTiersQuery(
            TourId: tourId, ActiveOnly: false, LanguageCode: "en",
            CallerUserId: owner, IsAdmin: true);
        adminQuery.CacheKey.Should().Contain(":viz=adm:");

        anonQuery.CacheKey.Should().NotBe(adminQuery.CacheKey);
    }
}
