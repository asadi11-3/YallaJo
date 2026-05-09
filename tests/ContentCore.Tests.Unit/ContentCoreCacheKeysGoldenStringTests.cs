using ContentCore.Application.Caching;
using ContentCore.Domain.Enums;
using FluentAssertions;

namespace ContentCore.Tests.Unit;

/// <summary>
/// Golden-string regression tests for <see cref="ContentCoreCacheKeys"/> tag helpers.
/// CONTENTCORE-STD-P2-002: tag strings are intentionally distinct from cache key strings
/// and MUST remain byte-for-byte identical to the historical literals used by query
/// <c>Tags</c> properties. Any rename / reformat silently breaks HybridCache invalidation.
///
/// Rules for this file:
///  - No mocks.  No DB.  No cache object.
///  - Tests only tag constants / tag helpers — not cache key helpers or TTLs.
///  - The expected string literals here are the source of truth.
///    If a value changes in production, this test MUST fail first.
/// </summary>
public sealed class ContentCoreCacheKeysGoldenStringTests
{
    // ── 1. Global tag constants ───────────────────────────────────────────────
    // Every query that tags its cache entry with one of these constants must
    // produce exactly the same string that command handlers pass to RemoveByTagAsync.

    [Fact]
    public void CategoriesTag_ShouldBeExactExpectedString()
        => ContentCoreCacheKeys.CategoriesTag.Should().Be("categories");

    [Fact]
    public void TagsTag_ShouldBeExactExpectedString()
        => ContentCoreCacheKeys.TagsTag.Should().Be("tags");

    [Fact]
    public void LanguagesTag_ShouldBeExactExpectedString()
        => ContentCoreCacheKeys.LanguagesTag.Should().Be("languages");

    [Fact]
    public void SpecializationsTag_ShouldBeExactExpectedString()
        => ContentCoreCacheKeys.SpecializationsTag.Should().Be("specializations");

    [Fact]
    public void AttachmentsTag_ShouldBeExactExpectedString()
        => ContentCoreCacheKeys.AttachmentsTag.Should().Be("attachments");

    [Fact]
    public void TranslationsTag_ShouldBeExactExpectedString()
        => ContentCoreCacheKeys.TranslationsTag.Should().Be("translations");

    // ── 2. Single-resource tag helpers ────────────────────────────────────────
    // These are used to evict exactly one cached resource (e.g. after an update).
    // The prefix is intentionally SINGULAR ("category", not "categories").

    [Fact]
    public void CategoryTag_ShouldProduceExpectedPrefixAndGuid()
    {
        var id = new Guid("11111111-1111-1111-1111-111111111111");
        ContentCoreCacheKeys.CategoryTag(id)
            .Should().Be("category:11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void TagTag_ShouldProduceExpectedPrefixAndGuid()
    {
        var id = new Guid("22222222-2222-2222-2222-222222222222");
        ContentCoreCacheKeys.TagTag(id)
            .Should().Be("tag:22222222-2222-2222-2222-222222222222");
    }

    [Fact]
    public void LanguageTag_ShouldProduceExpectedPrefixAndGuid()
    {
        var id = new Guid("33333333-3333-3333-3333-333333333333");
        ContentCoreCacheKeys.LanguageTag(id)
            .Should().Be("language:33333333-3333-3333-3333-333333333333");
    }

    [Fact]
    public void SpecializationTag_ShouldProduceExpectedPrefixAndGuid()
    {
        var id = new Guid("44444444-4444-4444-4444-444444444444");
        ContentCoreCacheKeys.SpecializationTag(id)
            .Should().Be("specialization:44444444-4444-4444-4444-444444444444");
    }

    [Fact]
    public void AttachmentTag_ShouldProduceExpectedPrefixAndGuid()
    {
        var id = new Guid("55555555-5555-5555-5555-555555555555");
        ContentCoreCacheKeys.AttachmentTag(id)
            .Should().Be("attachment:55555555-5555-5555-5555-555555555555");
    }

    // ── 3. Entity-scoped tag helpers ──────────────────────────────────────────
    // These narrow invalidation to a single entity's cached list.
    // Note the asymmetric prefix design:
    //   EntityAttachmentsTag  → "attachments:..."   (matches AttachmentsTag prefix)
    //   EntityTranslationsTag → "translations:..."  (matches TranslationsTag prefix)
    //   EntityCategoriesTag   → "entity-categories:..." (distinct from CategoriesTag)
    //   EntityTagsTag         → "entity-tags:..."       (distinct from TagsTag)
    // Any accidental reformat would break RemoveByTagAsync matching silently.

    private static readonly Guid EntityId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string TourType = "Tour";

    [Fact]
    public void EntityAttachmentsTag_ShouldProduceExpectedPattern()
        => ContentCoreCacheKeys.EntityAttachmentsTag(TourType, EntityId)
            .Should().Be("attachments:Tour:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void EntityCategoriesTag_ShouldProduceExpectedPattern()
        => ContentCoreCacheKeys.EntityCategoriesTag(TourType, EntityId)
            .Should().Be("entity-categories:Tour:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void EntityTagsTag_ShouldProduceExpectedPattern()
        => ContentCoreCacheKeys.EntityTagsTag(TourType, EntityId)
            .Should().Be("entity-tags:Tour:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void EntityTranslationsTag_ShouldProduceExpectedPattern()
        => ContentCoreCacheKeys.EntityTranslationsTag(TourType, EntityId)
            .Should().Be("translations:Tour:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    // ── 4. EntityType enum parity ─────────────────────────────────────────────
    // Handlers call EntityType.ToString() before passing to tag helpers.
    // If the enum member is renamed (e.g. Tour → TourEntity), the ToString()
    // output changes silently, producing a different tag string and breaking
    // RemoveByTagAsync matching even if ContentCoreCacheKeys is untouched.

    [Fact]
    public void EntityTypeTour_ToString_ShouldProduceLiteralTour()
        => EntityType.Tour.ToString().Should().Be("Tour");

    [Fact]
    public void EntityAttachmentsTag_ViaTourEnumToString_ShouldMatchDirectStringLiteral()
    {
        var viaEnum = ContentCoreCacheKeys.EntityAttachmentsTag(
            EntityType.Tour.ToString(), EntityId);
        var direct = ContentCoreCacheKeys.EntityAttachmentsTag("Tour", EntityId);
        viaEnum.Should().Be(direct,
            "EntityType.Tour.ToString() must produce 'Tour' so the tag helper is stable");
    }
}
