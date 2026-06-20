using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Social.Domain.Entities;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Social.Infrastructure.Persistence.Seeding;

/// <summary>
/// DEV-SEED-B1 — Development / QA-only seeder that creates public reviews used to validate
/// review-image rendering rules:
/// <list type="bullet">
///   <item><see cref="DevSeedIds.ReviewWithImagesId"/> — published review that receives images.</item>
///   <item><see cref="DevSeedIds.ReviewWithoutImagesId"/> — published review with no images.</item>
///   <item><see cref="DevSeedIds.HiddenReviewWithImagesId"/> — auto-hidden review that DOES have
///   images, to confirm images never surface publicly for non-published reviews.</item>
/// </list>
/// <para>
/// Each review uses a distinct (UserId, TargetType, TargetId) tuple to satisfy the unique filtered
/// index on the Reviews table. Guarded by <see cref="IHostEnvironment.IsDevelopment"/>; idempotent
/// per-row. The actual image rows are attached by the ContentCore DevImagesSeeder.
/// </para>
/// </summary>
public sealed class DevSocialSeeder(
    SocialDbContext dbContext,
    IHostEnvironment hostEnvironment,
    ILogger<DevSocialSeeder> logger) : IModuleDbInitializer
{
    public int Order => 164;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        var seededAny = false;

        seededAny |= await EnsureReviewAsync(
            DevSeedIds.ReviewWithImagesId,
            ReviewTargetType.Tour,
            DevSeedIds.TourWithImageId,
            4.8m,
            "Fantastic guided experience",
            "Published review WITH images (dev seed) — used to verify review galleries render publicly.",
            ReviewStatus.Published,
            cancellationToken);

        seededAny |= await EnsureReviewAsync(
            DevSeedIds.ReviewWithoutImagesId,
            ReviewTargetType.Tour,
            DevSeedIds.TourWithoutImageId,
            4.2m,
            "Solid tour, no photos",
            "Published review WITHOUT images (dev seed) — used to verify text-only review rendering.",
            ReviewStatus.Published,
            cancellationToken);

        seededAny |= await EnsureReviewAsync(
            DevSeedIds.HiddenReviewWithImagesId,
            ReviewTargetType.Place,
            DevSeedIds.PlaceId,
            3.5m,
            "Hidden review with images",
            "Auto-hidden review WITH images (dev seed) — must NOT surface publicly despite having images.",
            ReviewStatus.AutoHidden,
            cancellationToken);

        if (seededAny)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("DEV-SEED-B1: seeded development reviews.");
        }
    }

    private async Task<bool> EnsureReviewAsync(
        Guid reviewId,
        ReviewTargetType targetType,
        Guid targetId,
        decimal rating,
        string title,
        string content,
        ReviewStatus status,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Reviews
            .IgnoreQueryFilters()
            .AnyAsync(r => r.Id == reviewId, cancellationToken);

        if (exists)
        {
            return false;
        }

        var review = CreateEntity<Review>();
        SetProperty(review, nameof(Review.Id), reviewId);
        SetProperty(review, nameof(Review.UserId), DevSeedIds.CustomerUserId);
        SetProperty(review, nameof(Review.TargetType), targetType);
        SetProperty(review, nameof(Review.TargetId), targetId);
        SetProperty(review, nameof(Review.Rating), rating);
        SetProperty(review, nameof(Review.Title), title);
        SetProperty(review, nameof(Review.Content), content);
        SetProperty(review, nameof(Review.VisitDate), (DateOnly?)DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)));
        SetProperty(review, nameof(Review.Status), status);
        SetProperty(review, nameof(Review.IsVerifiedBooking), false);
        SetProperty(review, nameof(Review.ProfanityFlagged), false);
        SetProperty(review, nameof(Review.CurrentReportCount), 0);
        SetProperty(review, nameof(Review.IsDeleted), false);
        SetProperty(review, nameof(Review.CreatedAt), DateTime.UtcNow.AddDays(-20));

        dbContext.Reviews.Add(review);
        return true;
    }

    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        if (Activator.CreateInstance(typeof(TEntity), nonPublic: true) is not TEntity entity)
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
}
