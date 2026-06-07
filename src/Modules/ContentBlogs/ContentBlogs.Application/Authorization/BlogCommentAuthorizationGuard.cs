using ContentBlogs.Domain.Entities;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Authorization;

public sealed class BlogCommentAuthorizationGuard(
    ICurrentUser currentUser,
    IBlogAuthorHierarchyGuard authorHierarchyGuard,
    ILogger<BlogCommentAuthorizationGuard> logger)
    : IBlogCommentAuthorizationGuard
{
    /// <summary>Owner-edit window per business requirements.</summary>
    public static readonly TimeSpan OwnerEditWindow = TimeSpan.FromMinutes(30);

    public async Task<Result> ResolveEditAuthorizationAsync(
        BlogComment comment,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comment);

        if (currentUser.UserId is null)
        {
            return Result.Failure(
                new Error("BlogComment.Unauthorized", "Authentication is required to manage comments."),
                Outcome.Unauthorized);
        }

        // ── Owner path ───────────────────────────────────────────────────────
        if (currentUser.UserId.Value == comment.UserId)
        {
            var age = utcNow - comment.CreatedAt;
            if (age > OwnerEditWindow)
            {
                logger.LogInformation(
                    "BlogCommentAuthorizationGuard: owner edit window expired for comment {CommentId} (age={AgeMinutes} min).",
                    comment.Id, (int)age.TotalMinutes);
                return Result.Failure(
                    new Error(
                        "BlogComment.EditWindowExpired",
                        "Comments can only be edited by their owner within 30 minutes of posting."),
                    Outcome.Forbidden);
            }

            return Result.Success();
        }

        // ── Moderation path — defer to the existing author-hierarchy guard.
        // It enforces "actor's privilege level must strictly outrank the target's",
        // which already covers Admin / SuperAdmin / Owner moderation and prevents
        // privilege-escalation (Standard cannot moderate; Admin cannot moderate
        // peers/seniors; SuperAdmin cannot moderate Owner).
        return await authorHierarchyGuard
            .EnsureCanManageBlogOwnedByAsync(comment.UserId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result> ResolveDeleteAuthorizationAsync(
        BlogComment comment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comment);

        if (currentUser.UserId is null)
        {
            return Result.Failure(
                new Error("BlogComment.Unauthorized", "Authentication is required to manage comments."),
                Outcome.Unauthorized);
        }

        // Owner: allowed at any time (no time-window per business spec for delete).
        if (currentUser.UserId.Value == comment.UserId)
            return Result.Success();

        return await authorHierarchyGuard
            .EnsureCanManageBlogOwnedByAsync(comment.UserId, cancellationToken)
            .ConfigureAwait(false);
    }
}
