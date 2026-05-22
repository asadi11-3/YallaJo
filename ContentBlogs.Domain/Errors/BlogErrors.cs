using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Domain.Errors;

/// <summary>Domain errors for <see cref="Entities.Blog"/> transitions (Wave 7).</summary>
public static class BlogErrors
{
    public static readonly Error NotFound =
        new("Blog.NotFound", "Blog was not found.");

    public static readonly Error InvalidTransitionToReview =
        new("Blog.InvalidTransitionToReview", "Blog must be in Draft status to submit for creator review.");

    public static readonly Error NotCreatorAuthored =
        new("Blog.NotCreatorAuthored", "Only creator-authored articles can be submitted for creator review.");

    public static readonly Error InvalidTransitionToHidden =
        new("Blog.InvalidTransitionToHidden", "Blog must be in Published status to be hidden.");

    public static readonly Error InvalidTransitionToPublished =
        new("Blog.InvalidTransitionToPublished", "Blog must be in Hidden status to be unhidden.");

    public static readonly Error AlreadyDeleted =
        new("Blog.AlreadyDeleted", "This blog has been deleted.");
}
