using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentCore.Infrastructure.Persistence.Seeding;

/// <summary>
/// DEV-SEED-B1 — Development / QA-only seeder that attaches DB-only static-URL images to
/// development content so media workflows can be validated manually:
/// <list type="bullet">
///   <item>A primary image for <see cref="DevSeedIds.TourWithImageId"/> (tour card image).</item>
///   <item>Images for <see cref="DevSeedIds.ReviewWithImagesId"/> (published review gallery).</item>
///   <item>Images for <see cref="DevSeedIds.HiddenReviewWithImagesId"/> (auto-hidden review — to
///   confirm images do NOT appear publicly).</item>
/// </list>
/// <para>
/// Images reference existing static gallery assets (e.g. <c>/assets/images/gallery/01.jpg</c>) — NO
/// physical files are written and NO binaries are committed. Mirrors
/// <see cref="ContentCoreDbInitializer"/>: builds an <see cref="Attachment"/>, clears its domain
/// events (so the auto-EntityImage handler does not fire), then adds explicit
/// <see cref="EntityImage"/> rows. Guarded by <see cref="IHostEnvironment.IsDevelopment"/> and
/// idempotent per (EntityType, EntityId). Runs after the tour/review seeders (Order 165).
/// </para>
/// </summary>
public sealed class DevImagesSeeder(
    ContentCoreDbContext dbContext,
    IContentCoreUnitOfWork unitOfWork,
    IHostEnvironment hostEnvironment,
    ILogger<DevImagesSeeder> logger) : IModuleDbInitializer
{
    public int Order => 165;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        var ownerUserId = DevSeedIds.GuideUserId;
        var seededAny = false;

        // Tour card primary image.
        seededAny |= await EnsureImagesAsync(
            EntityType.Tour,
            DevSeedIds.TourWithImageId,
            ownerUserId,
            new[] { "/assets/images/gallery/01.jpg", "/assets/images/gallery/02.jpg" },
            cancellationToken);

        // Published review gallery.
        seededAny |= await EnsureImagesAsync(
            EntityType.Review,
            DevSeedIds.ReviewWithImagesId,
            ownerUserId,
            new[] { "/assets/images/gallery/03.jpg" },
            cancellationToken);

        // Auto-hidden review — has images but must never surface publicly.
        seededAny |= await EnsureImagesAsync(
            EntityType.Review,
            DevSeedIds.HiddenReviewWithImagesId,
            ownerUserId,
            new[] { "/assets/images/gallery/04.jpg" },
            cancellationToken);

        if (seededAny)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("DEV-SEED-B1: seeded development images (tour + review galleries).");
        }
    }

    private async Task<bool> EnsureImagesAsync(
        EntityType entityType,
        Guid entityId,
        Guid ownerUserId,
        string[] imageUrls,
        CancellationToken cancellationToken)
    {
        // Idempotency: if any attachment already exists for this entity, skip (composite-PK
        // EntityImage has no soft-delete, so guard on the parent Attachment instead).
        var alreadySeeded = await dbContext.Attachments
            .AnyAsync(a => a.EntityType == entityType && a.EntityId == entityId, cancellationToken);

        if (alreadySeeded)
        {
            return false;
        }

        var attachments = new List<Attachment>();
        var entityImages = new List<EntityImage>();

        for (var i = 0; i < imageUrls.Length; i++)
        {
            var attachment = Attachment.Create(
                entityType,
                entityId,
                AttachmentType.Image,
                imageUrls[i],
                ownerUserId,
                sortOrder: i);

            attachment.ClearDomainEvents();
            attachments.Add(attachment);

            entityImages.Add(EntityImage.Create(
                entityType,
                entityId,
                attachment.Id,
                ImageSize.Large,
                sortOrder: i,
                isPrimary: i == 0));
        }

        dbContext.Attachments.AddRange(attachments);
        dbContext.EntityImages.AddRange(entityImages);
        return true;
    }
}
