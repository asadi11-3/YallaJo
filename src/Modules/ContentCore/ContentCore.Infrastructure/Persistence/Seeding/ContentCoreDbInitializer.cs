using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentCore.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds reference data on first boot.
/// Uses domain factory methods so invariants are enforced and domain events (e.g.
/// LanguageActivatedDomainEvent) are raised and committed to the outbox atomically
/// via <see cref="IContentCoreUnitOfWork"/>.
/// Categories clear their own domain events after creation so the auto-translation
/// handler is not triggered for seed data whose translations are supplied manually.
/// </summary>
public sealed class ContentCoreDbInitializer(
    ContentCoreDbContext dbContext,
    IContentCoreUnitOfWork unitOfWork) : IModuleDbInitializer
{
    public int Order => 20;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Languages.AnyAsync(cancellationToken))
            return;

        // ── Languages ────────────────────────────────────────────────────────
        // Language.Create() raises LanguageActivatedDomainEvent → outbox message
        // is committed atomically so cross-module subscribers receive notification.
        var en = Language.Create("en", "English", "English", isRtl: false);
        var ar = Language.Create("ar", "Arabic", "العربية", isRtl: true);
        var es = Language.Create("es", "Spanish", "Español", isRtl: false);

        dbContext.Languages.AddRange(en, ar, es);

        // ── Categories ───────────────────────────────────────────────────────
        // Category.Create() raises CategoryCreatedDomainEvent. We clear it before
        // save so the auto-translation handler does not fire — translations are
        // added manually below to avoid external API calls during seeding.
        var adventure = BuildCategory("Adventure", "adventure", sortOrder: 1, icon: "mountain");
        adventure.AddTranslation(en.Id, "Adventure", "adventure");
        adventure.AddTranslation(ar.Id, "Moghamarat", "moghamarat");
        adventure.AddTranslation(es.Id, "Aventura", "aventura");

        var historical = BuildCategory("Historical", "historical", sortOrder: 2, icon: "landmark");
        historical.AddTranslation(en.Id, "Historical", "historical");
        historical.AddTranslation(ar.Id, "Tarikhi", "tarikhi");
        historical.AddTranslation(es.Id, "Histórico", "historico");

        var culinary = BuildCategory("Culinary", "culinary", sortOrder: 3, icon: "restaurant");
        culinary.AddTranslation(en.Id, "Culinary", "culinary");
        culinary.AddTranslation(ar.Id, "Matbakh", "matbakh");
        culinary.AddTranslation(es.Id, "Culinario", "culinario");

        dbContext.Categories.AddRange(adventure, historical, culinary);

        // ── Tags ─────────────────────────────────────────────────────────────
        var tagSeeds = new[]
        {
            ("family friendly", "family-friendly"),
            ("budget", "budget"),
            ("luxury", "luxury"),
            ("eco", "eco"),
            ("photography", "photography"),
            ("walking", "walking"),
        };

        foreach (var (name, slug) in tagSeeds)
            dbContext.Tags.Add(Tag.Create(name, slug));

        // ── Specializations ──────────────────────────────────────────────────
        dbContext.Specializations.AddRange(
            Specialization.Create("City Guide", "Expert in urban tours and local culture.", "compass"),
            Specialization.Create("Desert Guide", "Experienced with desert routes and safety planning.", "map"),
            Specialization.Create("Hiking Guide", "Leads mountain and trail adventures.", "binoculars"),
            Specialization.Create("History Expert", "Focuses on heritage sites and historical storytelling.", "book"),
            Specialization.Create("Food Specialist", "Curates culinary tours and local tasting sessions.", "utensils"));

        // Single atomic commit: Language outbox messages + all seed entities.
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a Category via the factory method (enforces invariants), then clears
    /// its domain events so auto-translation is suppressed during seeding.
    /// </summary>
    private static Category BuildCategory(string name, string slug, int sortOrder, string icon)
    {
        var category = Category.Create(name, slug, sourceLanguageCode: "en", sortOrder: sortOrder);
        category.SetIcon(icon);
        category.ClearDomainEvents(); // translations are seeded manually — suppress CategoryCreatedDomainEvent
        return category;
    }
}
