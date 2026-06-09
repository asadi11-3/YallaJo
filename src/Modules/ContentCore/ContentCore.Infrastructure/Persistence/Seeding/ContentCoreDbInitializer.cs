using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
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
        var adventure = BuildCategory("Adventure", "adventure", sortOrder: 1, icon: "bi-compass");
        adventure.AddTranslation(en.Id, "Adventure", "adventure");
        adventure.AddTranslation(ar.Id, "Moghamarat", "moghamarat");
        adventure.AddTranslation(es.Id, "Aventura", "aventura");

        var historical = BuildCategory("Historical", "historical", sortOrder: 2, icon: "bi-bank");
        historical.AddTranslation(en.Id, "Historical", "historical");
        historical.AddTranslation(ar.Id, "Tarikhi", "tarikhi");
        historical.AddTranslation(es.Id, "Histórico", "historico");

        var culinary = BuildCategory("Culinary", "culinary", sortOrder: 3, icon: "bi-egg-fried");
        culinary.AddTranslation(en.Id, "Culinary", "culinary");
        culinary.AddTranslation(ar.Id, "Matbakh", "matbakh");
        culinary.AddTranslation(es.Id, "Culinario", "culinario");

        var nature = BuildCategory("Nature", "nature", sortOrder: 4, icon: "bi-tree");
        nature.AddTranslation(en.Id, "Nature", "nature");
        nature.AddTranslation(ar.Id, "Tabea", "tabea");
        nature.AddTranslation(es.Id, "Naturaleza", "naturaleza");

        var beach = BuildCategory("Beach", "beach", sortOrder: 5, icon: "bi-umbrella");
        beach.AddTranslation(en.Id, "Beach", "beach");
        beach.AddTranslation(ar.Id, "Shatea", "shatea");
        beach.AddTranslation(es.Id, "Playa", "playa");

        var culture = BuildCategory("Culture", "culture", sortOrder: 6, icon: "bi-palette");
        culture.AddTranslation(en.Id, "Culture", "culture");
        culture.AddTranslation(ar.Id, "Thaqafa", "thaqafa");
        culture.AddTranslation(es.Id, "Cultura", "cultura");

        var wellness = BuildCategory("Wellness", "wellness", sortOrder: 7, icon: "bi-heart-pulse");
        wellness.AddTranslation(en.Id, "Wellness", "wellness");
        wellness.AddTranslation(ar.Id, "Afiya", "afiya");
        wellness.AddTranslation(es.Id, "Bienestar", "bienestar");

        var desert = BuildCategory("Desert", "desert", sortOrder: 8, icon: "bi-sun");
        desert.AddTranslation(en.Id, "Desert", "desert");
        desert.AddTranslation(ar.Id, "Sahraa", "sahraa");
        desert.AddTranslation(es.Id, "Desierto", "desierto");

        dbContext.Categories.AddRange(adventure, historical, culinary, nature, beach, culture, wellness, desert);

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

        // ── Place Images ─────────────────────────────────────────────────────
        // Seed gallery images for all seeded places.
        // AttachmentUploadedDomainEvent is cleared so the cross-module
        // event handler is NOT triggered for test/seed assets.
        var ownerUserId = SeedIdentityProfiles.All[0].UserId;

        // Petra (3 images)
        var petraAttachments = new[]
        {
            Attachment.Create(EntityType.Place, SeedContentIds.PlacePetra, AttachmentType.Image, "/assets/images/gallery/01.jpg", ownerUserId, sortOrder: 0),
            Attachment.Create(EntityType.Place, SeedContentIds.PlacePetra, AttachmentType.Image, "/assets/images/gallery/02.jpg", ownerUserId, sortOrder: 1),
            Attachment.Create(EntityType.Place, SeedContentIds.PlacePetra, AttachmentType.Image, "/assets/images/gallery/03.jpg", ownerUserId, sortOrder: 2),
        };

        // Jerash (2 images)
        var jerashAttachments = new[]
        {
            Attachment.Create(EntityType.Place, SeedContentIds.PlaceJerash, AttachmentType.Image, "/assets/images/gallery/04.jpg", ownerUserId, sortOrder: 0),
            Attachment.Create(EntityType.Place, SeedContentIds.PlaceJerash, AttachmentType.Image, "/assets/images/gallery/05.jpg", ownerUserId, sortOrder: 1),
        };

        // Dead Sea (2 images)
        var deadSeaAttachments = new[]
        {
            Attachment.Create(EntityType.Place, SeedContentIds.PlaceDeadSea, AttachmentType.Image, "/assets/images/gallery/06.jpg", ownerUserId, sortOrder: 0),
            Attachment.Create(EntityType.Place, SeedContentIds.PlaceDeadSea, AttachmentType.Image, "/assets/images/gallery/07.jpg", ownerUserId, sortOrder: 1),
        };

        // Wadi Rum (2 images)
        var wadiRumAttachments = new[]
        {
            Attachment.Create(EntityType.Place, SeedContentIds.PlaceWadiRum, AttachmentType.Image, "/assets/images/gallery/08.jpg", ownerUserId, sortOrder: 0),
            Attachment.Create(EntityType.Place, SeedContentIds.PlaceWadiRum, AttachmentType.Image, "/assets/images/gallery/09.jpg", ownerUserId, sortOrder: 1),
        };

        // Aqaba (2 images)
        var aqabaAttachments = new[]
        {
            Attachment.Create(EntityType.Place, SeedContentIds.PlaceAqaba, AttachmentType.Image, "/assets/images/gallery/10.jpg", ownerUserId, sortOrder: 0),
            Attachment.Create(EntityType.Place, SeedContentIds.PlaceAqaba, AttachmentType.Image, "/assets/images/gallery/11.jpg", ownerUserId, sortOrder: 1),
        };

        // Amman (2 images)
        var ammanAttachments = new[]
        {
            Attachment.Create(EntityType.Place, SeedContentIds.PlaceAmman, AttachmentType.Image, "/assets/images/gallery/12.jpg", ownerUserId, sortOrder: 0),
            Attachment.Create(EntityType.Place, SeedContentIds.PlaceAmman, AttachmentType.Image, "/assets/images/gallery/13.jpg", ownerUserId, sortOrder: 1),
        };

        var allAttachments = petraAttachments
            .Concat(jerashAttachments)
            .Concat(deadSeaAttachments)
            .Concat(wadiRumAttachments)
            .Concat(aqabaAttachments)
            .Concat(ammanAttachments)
            .ToArray();

        foreach (var att in allAttachments)
            att.ClearDomainEvents();

        dbContext.Attachments.AddRange(allAttachments);

        dbContext.EntityImages.AddRange(
            // Petra
            EntityImage.Create(EntityType.Place, SeedContentIds.PlacePetra,   petraAttachments[0].Id,    ImageSize.Large, sortOrder: 0, isPrimary: true),
            EntityImage.Create(EntityType.Place, SeedContentIds.PlacePetra,   petraAttachments[1].Id,    ImageSize.Large, sortOrder: 1),
            EntityImage.Create(EntityType.Place, SeedContentIds.PlacePetra,   petraAttachments[2].Id,    ImageSize.Large, sortOrder: 2),
            // Jerash
            EntityImage.Create(EntityType.Place, SeedContentIds.PlaceJerash,  jerashAttachments[0].Id,   ImageSize.Large, sortOrder: 0, isPrimary: true),
            EntityImage.Create(EntityType.Place, SeedContentIds.PlaceJerash,  jerashAttachments[1].Id,   ImageSize.Large, sortOrder: 1),
            // Dead Sea
            EntityImage.Create(EntityType.Place, SeedContentIds.PlaceDeadSea, deadSeaAttachments[0].Id,  ImageSize.Large, sortOrder: 0, isPrimary: true),
            EntityImage.Create(EntityType.Place, SeedContentIds.PlaceDeadSea, deadSeaAttachments[1].Id,  ImageSize.Large, sortOrder: 1),
            // Wadi Rum
            EntityImage.Create(EntityType.Place, SeedContentIds.PlaceWadiRum, wadiRumAttachments[0].Id,  ImageSize.Large, sortOrder: 0, isPrimary: true),
            EntityImage.Create(EntityType.Place, SeedContentIds.PlaceWadiRum, wadiRumAttachments[1].Id,  ImageSize.Large, sortOrder: 1),
            // Aqaba
            EntityImage.Create(EntityType.Place, SeedContentIds.PlaceAqaba,   aqabaAttachments[0].Id,    ImageSize.Large, sortOrder: 0, isPrimary: true),
            EntityImage.Create(EntityType.Place, SeedContentIds.PlaceAqaba,   aqabaAttachments[1].Id,    ImageSize.Large, sortOrder: 1),
            // Amman
            EntityImage.Create(EntityType.Place, SeedContentIds.PlaceAmman,   ammanAttachments[0].Id,    ImageSize.Large, sortOrder: 0, isPrimary: true),
            EntityImage.Create(EntityType.Place, SeedContentIds.PlaceAmman,   ammanAttachments[1].Id,    ImageSize.Large, sortOrder: 1));

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
