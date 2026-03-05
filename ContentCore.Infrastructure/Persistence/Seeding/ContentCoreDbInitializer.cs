using ContentCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentCore.Infrastructure.Persistence.Seeding;

public sealed class ContentCoreDbInitializer(ContentCoreDbContext dbContext) : IModuleDbInitializer
{
    public int Order => 20;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Languages.AnyAsync(cancellationToken))
        {
            return;
        }

        var languages = SeedLanguages();
        var categories = SeedCategories();
        var categoryTranslations = SeedCategoryTranslations(categories, languages);
        var tags = SeedTags();
        var specializations = SeedSpecializations();

        dbContext.Languages.AddRange(languages);
        dbContext.Categories.AddRange(categories);
        dbContext.CategoryTranslations.AddRange(categoryTranslations);
        dbContext.Tags.AddRange(tags);
        dbContext.Specializations.AddRange(specializations);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<Language> SeedLanguages()
    {
        return
        [
            BuildLanguage("en", "English", "English", isRtl: false),
            BuildLanguage("ar", "Arabic", "العربية", isRtl: true),
            BuildLanguage("es", "Spanish", "Espanol", isRtl: false)
        ];
    }

    private static List<Category> SeedCategories()
    {
        return
        [
            BuildCategory("Adventure", "adventure", 1, "mountain"),
            BuildCategory("Historical", "historical", 2, "landmark"),
            BuildCategory("Culinary", "culinary", 3, "restaurant")
        ];
    }

    private static List<CategoryTranslation> SeedCategoryTranslations(
        IReadOnlyList<Category> categories,
        IReadOnlyList<Language> languages)
    {
        var byName = categories.ToDictionary(GetStringProperty, c => c, StringComparer.OrdinalIgnoreCase);
        var byCode = languages.ToDictionary(GetCodeProperty, l => l, StringComparer.OrdinalIgnoreCase);

        return
        [
            BuildCategoryTranslation(byName["Adventure"], byCode["en"], "Adventure", "adventure"),
            BuildCategoryTranslation(byName["Adventure"], byCode["ar"], "Moghamarat", "moghamarat"),
            BuildCategoryTranslation(byName["Adventure"], byCode["es"], "Aventura", "aventura"),

            BuildCategoryTranslation(byName["Historical"], byCode["en"], "Historical", "historical"),
            BuildCategoryTranslation(byName["Historical"], byCode["ar"], "Tarikhi", "tarikhi"),
            BuildCategoryTranslation(byName["Historical"], byCode["es"], "Historico", "historico"),

            BuildCategoryTranslation(byName["Culinary"], byCode["en"], "Culinary", "culinary"),
            BuildCategoryTranslation(byName["Culinary"], byCode["ar"], "Matbakh", "matbakh"),
            BuildCategoryTranslation(byName["Culinary"], byCode["es"], "Culinario", "culinario")
        ];
    }

    private static List<Tag> SeedTags()
    {
        var names = new[] { "family-friendly", "budget", "luxury", "eco", "photography", "walking" };

        return names.Select(BuildTag).ToList();
    }

    private static List<Specialization> SeedSpecializations()
    {
        return
        [
            BuildSpecialization("City Guide", "Expert in urban tours and local culture.", "compass"),
            BuildSpecialization("Desert Guide", "Experienced with desert routes and safety planning.", "map"),
            BuildSpecialization("Hiking Guide", "Leads mountain and trail adventures.", "binoculars"),
            BuildSpecialization("History Expert", "Focuses on heritage sites and historical storytelling.", "book"),
            BuildSpecialization("Food Specialist", "Curates culinary tours and local tasting sessions.", "utensils")
        ];
    }

    private static Language BuildLanguage(string code, string name, string nativeName, bool isRtl)
    {
        var language = CreateEntity<Language>();
        SetProperty(language, nameof(Language.Code), code);
        SetProperty(language, nameof(Language.Name), name);
        SetProperty(language, nameof(Language.NativeName), nativeName);
        SetProperty(language, nameof(Language.IsRtl), isRtl);
        SetProperty(language, nameof(Language.IsActive), true);
        return language;
    }

    private static Category BuildCategory(string name, string slug, int sortOrder, string icon)
    {
        var category = CreateEntity<Category>();
        SetProperty(category, nameof(Category.Name), name);
        SetProperty(category, nameof(Category.Slug), slug);
        SetProperty(category, nameof(Category.SortOrder), sortOrder);
        SetProperty(category, nameof(Category.Icon), icon);
        SetProperty(category, nameof(Category.IsActive), true);
        return category;
    }

    private static CategoryTranslation BuildCategoryTranslation(
        Category category,
        Language language,
        string translatedName,
        string translatedSlug)
    {
        var translation = CreateEntity<CategoryTranslation>();
        SetProperty(translation, nameof(CategoryTranslation.CategoryId), category.Id);
        SetProperty(translation, nameof(CategoryTranslation.LanguageId), language.Id);
        SetProperty(translation, nameof(CategoryTranslation.Name), translatedName);
        SetProperty(translation, nameof(CategoryTranslation.Slug), translatedSlug);
        return translation;
    }

    private static Tag BuildTag(string seedName)
    {
        var tag = CreateEntity<Tag>();
        var normalized = seedName.Trim().ToLowerInvariant();
        SetProperty(tag, nameof(Tag.Name), normalized.Replace('-', ' '));
        SetProperty(tag, nameof(Tag.Slug), normalized);
        SetProperty(tag, nameof(Tag.IsActive), true);
        return tag;
    }

    private static Specialization BuildSpecialization(string name, string description, string icon)
    {
        var specialization = CreateEntity<Specialization>();
        SetProperty(specialization, nameof(Specialization.Name), name);
        SetProperty(specialization, nameof(Specialization.Description), description);
        SetProperty(specialization, nameof(Specialization.Icon), icon);
        SetProperty(specialization, nameof(Specialization.IsActive), true);
        return specialization;
    }

    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        var entity = Activator.CreateInstance(typeof(TEntity), nonPublic: true) as TEntity;
        if (entity is null)
        {
            throw new InvalidOperationException($"Failed to create entity instance for {typeof(TEntity).FullName}.");
        }

        return entity;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (property is null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");
        }

        property.SetValue(target, value);
    }

    private static string GetStringProperty(Category category)
    {
        return category.GetType().GetProperty(nameof(Category.Name))?.GetValue(category)?.ToString() ?? string.Empty;
    }

    private static string GetCodeProperty(Language language)
    {
        return language.GetType().GetProperty(nameof(Language.Code))?.GetValue(language)?.ToString() ?? string.Empty;
    }
}
