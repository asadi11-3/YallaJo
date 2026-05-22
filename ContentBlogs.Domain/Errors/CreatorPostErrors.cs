using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Domain.Errors;

/// <summary>Domain errors for <see cref="Entities.Creators.CreatorPost"/>.</summary>
public static class CreatorPostErrors
{
    public static readonly Error NotFound =
        new("CreatorPost.NotFound", "Creator post was not found.");

    public static readonly Error NotDraft =
        new("CreatorPost.NotDraft", "Post must be in Draft status for this operation.");

    public static readonly Error NotPendingReview =
        new("CreatorPost.NotPendingReview", "Post must be in PendingReview status for this operation.");

    public static readonly Error NotPublished =
        new("CreatorPost.NotPublished", "Post must be Published for this operation.");

    public static readonly Error NotHidden =
        new("CreatorPost.NotHidden", "Post must be Hidden for this operation.");

    public static readonly Error AlreadyPublished =
        new("CreatorPost.AlreadyPublished", "Post is already published.");

    public static readonly Error AlreadyFeatured =
        new("CreatorPost.AlreadyFeatured", "Post is already featured.");

    public static readonly Error NotFeatured =
        new("CreatorPost.NotFeatured", "Post is not currently featured.");

    public static readonly Error AlreadyRemoved =
        new("CreatorPost.AlreadyRemoved", "Post has already been removed.");

    public static readonly Error TierInsufficientForDirectPublish =
        new("CreatorPost.TierInsufficientForDirectPublish", "Only Trusted or Expert tier creators can publish directly.");

    public static readonly Error TierInsufficientForFeaturing =
        new("CreatorPost.TierInsufficientForFeaturing", "Only Expert tier creators are eligible for featuring.");

    public static readonly Error DisclosureRequired =
        new("CreatorPost.DisclosureRequired", "Post references entities owned by the creator; IsSponsored must be true or disclosures must be declared.");

    public static readonly Error SlugAlreadyTaken =
        new("CreatorPost.SlugAlreadyTaken", "The requested slug is already in use by another post.");

    public static readonly Error ExcerptTooShort =
        new("CreatorPost.ExcerptTooShort", "Excerpt must be at least 100 characters.");

    public static readonly Error ExcerptTooLong =
        new("CreatorPost.ExcerptTooLong", "Excerpt must not exceed 500 characters.");

    public static readonly Error TooManyNiches =
        new("CreatorPost.TooManyNiches", "A post may reference at most 3 niches.");

    public static readonly Error TooManyFreeTags =
        new("CreatorPost.TooManyFreeTags", "A post may have at most 10 free tags.");
}
