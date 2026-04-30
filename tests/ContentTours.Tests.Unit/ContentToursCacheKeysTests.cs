using ContentTours.Application.Caching;
using FluentAssertions;

namespace ContentTours.Tests.Unit;

/// <summary>
/// Phase-3 cache-language partition contract.
///
/// The 3 public tour query handlers project translated identity-bearing fields via
/// COALESCE(translation, source). Without language in the cache key the Arabic
/// projection would be served to English callers (cross-language poisoning), so the
/// cache-key helper MUST partition every language-sensitive read by normalized
/// Accept-Language. These tests pin that contract against accidental regressions.
/// </summary>
public sealed class ContentToursCacheKeysTests
{
    [Fact]
    public void Tour_VariesByLanguage()
    {
        var id = Guid.NewGuid();

        var en = ContentToursCacheKeys.Tour(id, "en-US", isElevated: false);
        var ar = ContentToursCacheKeys.Tour(id, "ar-JO", isElevated: false);

        en.Should().NotBe(ar);
    }

    [Fact]
    public void Tour_StableKeyForBlankLanguage()
    {
        var id = Guid.NewGuid();

        var nullKey  = ContentToursCacheKeys.Tour(id, null,  isElevated: false);
        var blankKey = ContentToursCacheKeys.Tour(id, "   ", isElevated: false);
        var emptyKey = ContentToursCacheKeys.Tour(id, "",    isElevated: false);

        nullKey.Should().Be(blankKey);
        nullKey.Should().Be(emptyKey);
        nullKey.Should().Contain(":lang:default:");
    }

    [Fact]
    public void TourBySlug_VariesByLanguage()
    {
        var en = ContentToursCacheKeys.TourBySlug("petra", "en", isElevated: false);
        var ar = ContentToursCacheKeys.TourBySlug("petra", "ar", isElevated: false);

        en.Should().NotBe(ar);
    }

    [Fact]
    public void TourList_VariesByLanguage()
    {
        var en = ContentToursCacheKeys.TourList(1, 20, null, null, null, null, "en-US",      isElevated: false);
        var ar = ContentToursCacheKeys.TourList(1, 20, null, null, null, null, "ar-JO,en;q=0.5", isElevated: false);

        en.Should().NotBe(ar);
    }

    [Fact]
    public void TourList_VariesByElevatedFlag()
    {
        var pub = ContentToursCacheKeys.TourList(1, 20, null, null, null, null, "en", isElevated: false);
        var ele = ContentToursCacheKeys.TourList(1, 20, null, null, null, null, "en", isElevated: true);

        pub.Should().NotBe(ele);
    }

    [Theory]
    [InlineData(null,                "default")]
    [InlineData("",                  "default")]
    [InlineData("   ",               "default")]
    [InlineData("EN",                "en")]
    [InlineData("en-US",             "en-us")]
    [InlineData("ar-JO,en;q=0.5",    "ar-jo")]
    [InlineData("ar;q=0.9,en-US",    "ar")]
    public void NormalizeLanguage_NormalisesHeaderToFirstTokenLowercased(string? input, string expected)
    {
        var actual = ContentToursCacheKeys.NormalizeLanguage(input);

        actual.Should().Be(expected);
    }
}
