using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Domain.Errors;

/// <summary>Domain errors for <see cref="Entities.Blog"/> transitions.</summary>
public static class BlogErrors
{
    public static readonly Error NotFound =
        new("Blog.NotFound", "Blog was not found.");

    public static readonly Error AlreadyDeleted =
        new("Blog.AlreadyDeleted", "This blog has been deleted.");

    public static readonly Error NotCreatorAuthored =
        new("Blog.NotCreatorAuthored", "Only creator-authored articles can be submitted for review.");

    // ── Transition guards ───────────────────────────────────────────────

    public static readonly Error InvalidTransitionToReview =
        new("Blog.InvalidTransitionToReview", "Blog must be in Draft or Rejected status to submit for review.");

    public static readonly Error InvalidTransitionToApproved =
        new("Blog.InvalidTransitionToApproved", "Blog must be in PendingReview status to be approved.");

    public static readonly Error InvalidTransitionToRejected =
        new("Blog.InvalidTransitionToRejected", "Blog must be in PendingReview status to be rejected.");

    public static readonly Error InvalidTransitionToRemoved =
        new("Blog.InvalidTransitionToRemoved", "Blog cannot be removed from its current status.");

    public static readonly Error InvalidTransitionToHidden =
        new("Blog.InvalidTransitionToHidden", "Blog must be in Published status to be hidden.");

    public static readonly Error InvalidTransitionToPublished =
        new("Blog.InvalidTransitionToPublished", "Blog must be in Hidden status to be unhidden.");

    public static readonly Error InvalidTransitionToFeatured =
        new("Blog.InvalidTransitionToFeatured", "Blog must be in Published status to be featured.");

    public static readonly Error AlreadyFeatured =
        new("Blog.AlreadyFeatured", "Blog is already featured.");

    public static readonly Error NotFeatured =
        new("Blog.NotFeatured", "Blog is not currently featured.");

    public static readonly Error Unauthorized =
        new("Blog.Unauthorized", "You do not have permission to modify this blog.");
}
