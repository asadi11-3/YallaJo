using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using FluentAssertions;
using YallaJo.Tests.Shared;

namespace ContentCore.Tests.Unit;

public sealed class CategoryTranslationProtectionTests : DomainTestBase
{
    [Fact]
    public void TryUpdateAutoTranslationShouldNotOverwriteWhenTranslationIsHumanReviewed()
    {
        var category = Category.Create("Food", RandomSlug(), DefaultSourceLanguageCode);
        var languageId = Guid.NewGuid();

        category.AddTranslation(languageId, "طعام", "taeam", TranslationStatus.HumanReviewed);

        var updated = category.TryUpdateAutoTranslation(languageId, "Food Auto", "food-auto");

        updated.Should().BeFalse();

        var translation = category.Translations.Single(x => x.LanguageId == languageId);
        translation.Name.Should().Be("طعام");
        translation.Slug.Should().Be("taeam");
        translation.Status.Should().Be(TranslationStatus.HumanReviewed);
    }

    [Fact]
    public void TryUpdateAutoTranslationShouldUpdateWhenTranslationIsAutoTranslated()
    {
        var category = Category.Create("Adventure", RandomSlug(), DefaultSourceLanguageCode);
        var languageId = Guid.NewGuid();

        category.AddTranslation(languageId, "مغامرة", "mughamara", TranslationStatus.AutoTranslated);

        var updated = category.TryUpdateAutoTranslation(languageId, "Adventure Updated", "adventure-updated");

        updated.Should().BeTrue();

        var translation = category.Translations.Single(x => x.LanguageId == languageId);
        translation.Name.Should().Be("Adventure Updated");
        translation.Slug.Should().Be("adventure-updated");
        translation.Status.Should().Be(TranslationStatus.AutoTranslated);
    }

    [Fact]
    public void UpsertHumanReviewedTranslationShouldCreateThenUpdateSingleTranslation()
    {
        var category = Category.Create("Nature", RandomSlug(), DefaultSourceLanguageCode);
        var languageId = Guid.NewGuid();

        category.UpsertHumanReviewedTranslation(languageId, "طبيعة", "tabiea");
        category.UpsertHumanReviewedTranslation(languageId, "طبيعة محدثة", "tabiea-updated");

        category.Translations.Should().HaveCount(1);

        var translation = category.Translations.Single(x => x.LanguageId == languageId);
        translation.Name.Should().Be("طبيعة محدثة");
        translation.Slug.Should().Be("tabiea-updated");
        translation.Status.Should().Be(TranslationStatus.HumanReviewed);
    }
}
